using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Runs a whole setup unattended: one registry write, one package transaction, then every cached
	/// Asset Store package imported back to back without a dialog.
	///
	/// The point of this class is that nothing in it asks the user for anything. The old window
	/// installed one package per <c>Client.Add</c> call, which means one resolve and one domain reload
	/// each — a dozen packages was a dozen reloads and a dozen "Import Unity Package" windows to click
	/// through. <see cref="Client.AddAndRemove"/> takes the entire add and remove list in a single
	/// transaction, and <see cref="AssetDatabase.ImportPackage"/> has a non-interactive mode that
	/// nobody uses because the queueing around it is fiddly. Both are handled here.
	///
	/// Everything survives the domain reload the package transaction causes: the pending Asset Store
	/// queue lives in <see cref="SessionState"/>, and the static constructor picks it back up.
	/// </summary>
	[InitializeOnLoad]
	public static class PackageInstaller
	{
		private const string QueueKey = "NMJ.Setup.PendingImports";
		private const string StatusKey = "NMJ.Setup.Status";
		private const string RunningKey = "NMJ.Setup.Running";
		private const string ImportedAnythingKey = "NMJ.Setup.ImportedAnything";
		private const char QueueSeparator = '|';

		private static AddAndRemoveRequest activeRequest;

		/// <summary>Raised whenever the status line changes, so an open window can repaint.</summary>
		public static event Action Changed;

		static PackageInstaller()
		{
			// A reload lands here mid-run when the package transaction finished and the Asset Store
			// queue has not been drained yet. Deferred past the constructor because importing touches
			// the asset database, which is not ready during the InitializeOnLoad pass.
			if (PendingImports.Count > 0) EditorApplication.delayCall += DrainImportQueue;
		}

		/// <summary>Human-readable progress, persisted so it survives the reload mid-run.</summary>
		public static string Status
		{
			get => SessionState.GetString(StatusKey, string.Empty);
			private set
			{
				SessionState.SetString(StatusKey, value ?? string.Empty);
				Changed?.Invoke();
			}
		}

		/// <summary>True while a transaction or an import queue is in flight.</summary>
		public static bool IsBusy =>
			activeRequest is { IsCompleted: false } ||
			SessionState.GetBool(RunningKey, false) ||
			importInFlight ||
			PendingImports.Count > 0;

		private static List<string> PendingImports
		{
			get
			{
				var raw = SessionState.GetString(QueueKey, string.Empty);
				return string.IsNullOrEmpty(raw)
						   ? new List<string>()
						   : raw.Split(QueueSeparator).Where(s => !string.IsNullOrEmpty(s)).ToList();
			}
			set => SessionState.SetString(QueueKey, value == null ? string.Empty : string.Join(QueueSeparator.ToString(), value));
		}

		/// <summary>What a run intends to do, so the window can show it before anything happens.</summary>
		public readonly struct Plan
		{
			public Plan(IReadOnlyList<PackageEntry> toInstall, IReadOnlyList<PackageRemoval> toRemove,
						IReadOnlyList<PackageEntry> toImport, IReadOnlyList<PackageEntry> unavailable)
			{
				ToInstall = toInstall;
				ToRemove = toRemove;
				ToImport = toImport;
				Unavailable = unavailable;
			}

			/// <summary>UPM packages that are selected and not present.</summary>
			public IReadOnlyList<PackageEntry> ToInstall { get; }

			/// <summary>Packages to strip, that are actually in the manifest.</summary>
			public IReadOnlyList<PackageRemoval> ToRemove { get; }

			/// <summary>Paid packages found in the Asset Store cache and importable.</summary>
			public IReadOnlyList<PackageEntry> ToImport { get; }

			/// <summary>Paid packages selected but neither installed nor cached — nothing can be done.</summary>
			public IReadOnlyList<PackageEntry> Unavailable { get; }

			public bool IsEmpty => ToInstall.Count == 0 && ToRemove.Count == 0 && ToImport.Count == 0;

			public int TotalActions => ToInstall.Count + ToRemove.Count + ToImport.Count;
		}

		/// <summary>
		/// Works out what a run would do without doing any of it. Pure — safe to call from OnGUI.
		/// </summary>
		public static Plan BuildPlan(SetupProfile profile)
		{
			var install = new List<PackageEntry>();
			var import = new List<PackageEntry>();
			var unavailable = new List<PackageEntry>();
			var remove = new List<PackageRemoval>();

			if (profile == null)
				return new Plan(install, remove, import, unavailable);

			var installed = PackageManifest.ReadDependencies();

			foreach (var entry in profile.EnabledEntries)
			{
				if (PackageDetector.IsPresent(entry)) continue;

				if (entry.IsUpm)
				{
					// A git package's manifest key is its package id, not the URL it was added from,
					// so this catches one that is installed but has not compiled yet.
					if (!string.IsNullOrEmpty(entry.packageId) && installed.Contains(entry.packageId)) continue;
					install.Add(entry);
					continue;
				}

				var cached = AssetStoreLocator.FindCachedPackage(entry);

				if (cached != null && profile.ImportCachedAssetStorePackages) import.Add(entry);
				else unavailable.Add(entry);
			}

			foreach (var removal in profile.EnabledRemovals)
			{
				if (installed.Contains(removal.packageId)) remove.Add(removal);
			}

			return new Plan(install, remove, import, unavailable);
		}

		/// <summary>
		/// Executes a plan. Returns immediately; progress arrives through <see cref="Changed"/>.
		/// </summary>
		public static void Run(SetupProfile profile, Plan plan)
		{
			if (IsBusy)
			{
				Debug.LogWarning("[NMJ] A setup run is already in progress.");
				return;
			}

			if (plan.IsEmpty)
			{
				Status = "Nothing to do — everything selected is already installed.";
				return;
			}

			// The registry has to exist before Client can resolve a com.tayx.* or com.yasirkula.*
			// name, so it is written first and synchronously.
			var openUpmScopes = plan.ToInstall
									.Where(e => e.source == InstallSource.OpenUpm)
									.Select(e => PackageManifest.ScopeOf(e.identifier))
									.ToList();

			if (openUpmScopes.Count > 0)
			{
				var registry = PackageManifest.EnsureOpenUpmScopes(openUpmScopes);

				if (registry.Failed)
				{
					Status = registry.Message;
					Debug.LogError("[NMJ] " + registry.Message);
					return;
				}

				if (registry.Changed) Debug.Log("[NMJ] " + registry.Message);
			}

			// Paid imports are queued now but run after the transaction, so a package that expects an
			// installed dependency finds it.
			PendingImports = plan.ToImport
								 .Select(AssetStoreLocator.FindCachedPackage)
								 .Where(path => !string.IsNullOrEmpty(path))
								 .ToList();

			if (profile != null && profile.OpenStorePagesForMissing)
			{
				foreach (var entry in plan.Unavailable.Where(e => !string.IsNullOrEmpty(e.url)))
					Application.OpenURL(entry.url);
			}

			var toAdd = plan.ToInstall.Select(e => e.identifier).Where(id => !string.IsNullOrEmpty(id)).ToArray();
			var toRemove = plan.ToRemove.Select(r => r.packageId).Where(id => !string.IsNullOrEmpty(id)).ToArray();

			if (toAdd.Length == 0 && toRemove.Length == 0)
			{
				DrainImportQueue();
				return;
			}

			SessionState.SetBool(RunningKey, true);
			Status = $"Resolving {toAdd.Length} package(s) to add and {toRemove.Length} to remove…";

			// One transaction rather than one call per package: a single resolve, a single domain
			// reload, and a dependency graph solved once with the whole set visible to it.
			activeRequest = Client.AddAndRemove(toAdd, toRemove);
			EditorApplication.update += PollTransaction;
		}

		private static void PollTransaction()
		{
			if (activeRequest is not { IsCompleted: true }) return;

			EditorApplication.update -= PollTransaction;

			var request = activeRequest;
			activeRequest = null;
			SessionState.SetBool(RunningKey, false);

			if (request.Status == StatusCode.Failure)
			{
				var message = request.Error?.message ?? "unknown error";
				Status = $"Package transaction failed: {message}";
				Debug.LogError($"[NMJ] Package transaction failed: {message}\n" +
							   "Nothing was half-installed — AddAndRemove is atomic, so the manifest is " +
							   "unchanged. A git package usually fails here because the URL is private " +
							   "or the machine has no git credentials.");

				// Do not import paid packages on top of a failed transaction; the dependencies they
				// were queued behind are not there.
				PendingImports = new List<string>();
				return;
			}

			Status = "Packages resolved. Importing Asset Store packages…";
			PackageDetector.Sync(true);

			// Deferred: the transaction's own asset-database work has to settle before an import
			// can start, and a reload may be about to run anyway.
			EditorApplication.delayCall += DrainImportQueue;
		}

		// ===================================================================================
		// Asset Store import queue
		// ===================================================================================

		/// <summary>
		/// Imports the queued .unitypackage files one after another, without the import dialog.
		///
		/// <see cref="AssetDatabase.ImportPackage"/> is asynchronous and Unity refuses to start a
		/// second import while one is running, so they cannot simply be looped. Chaining on the
		/// completion callbacks is what turns "press Import eight times" into one click — and the
		/// <c>interactive: false</c> argument is what stops the tree-view dialog appearing at all.
		/// </summary>
		/// <summary>
		/// Guards against the queue being drained twice.
		///
		/// Two things can start a drain: the completed transaction's own <c>delayCall</c>, and the
		/// static constructor after the domain reload that transaction causes. Whether both fire
		/// depends on the reload landing before or after the delayed call, which is not something this
		/// code gets to decide. Without this flag the race starts two imports, and Unity refuses the
		/// second with an error that says nothing about why.
		/// </summary>
		private static bool importInFlight;

		private static void DrainImportQueue()
		{
			if (importInFlight) return;

			var queue = PendingImports;

			if (queue.Count == 0)
			{
				if (SessionState.GetBool(ImportedAnythingKey, false))
				{
					SessionState.SetBool(ImportedAnythingKey, false);
					Status = "Setup complete.";
				}

				Unsubscribe();
				return;
			}

			var next = queue[0];
			queue.RemoveAt(0);
			PendingImports = queue;

			if (!System.IO.File.Exists(next))
			{
				Debug.LogWarning($"[NMJ] Cached package vanished before it could be imported: {next}");
				DrainImportQueue();
				return;
			}

			Status = $"Importing {System.IO.Path.GetFileNameWithoutExtension(next)} ({queue.Count} left)…";

			SessionState.SetBool(ImportedAnythingKey, true);
			importInFlight = true;

			Subscribe();
			AssetDatabase.ImportPackage(next, false);
		}

		private static bool subscribed;

		private static void Subscribe()
		{
			if (subscribed) return;
			subscribed = true;

			AssetDatabase.importPackageCompleted += OnImportCompleted;
			AssetDatabase.importPackageFailed += OnImportFailed;
			AssetDatabase.importPackageCancelled += OnImportCancelled;
		}

		private static void Unsubscribe()
		{
			if (!subscribed) return;
			subscribed = false;

			AssetDatabase.importPackageCompleted -= OnImportCompleted;
			AssetDatabase.importPackageFailed -= OnImportFailed;
			AssetDatabase.importPackageCancelled -= OnImportCancelled;
		}

		private static void OnImportCompleted(string packageName)
		{
			importInFlight = false;
			Debug.Log($"[NMJ] Imported '{packageName}'.");
			EditorApplication.delayCall += DrainImportQueue;
		}

		private static void OnImportFailed(string packageName, string errorMessage)
		{
			importInFlight = false;
			Debug.LogError($"[NMJ] Import of '{packageName}' failed: {errorMessage}");
			EditorApplication.delayCall += DrainImportQueue;
		}

		private static void OnImportCancelled(string packageName)
		{
			importInFlight = false;
			// A cancel is a deliberate act, so it stops the whole queue rather than moving to the next
			// one — otherwise cancelling means being asked seven more times.
			Debug.Log($"[NMJ] Import of '{packageName}' was cancelled; the rest of the queue was dropped.");
			PendingImports = new List<string>();
			Status = "Import cancelled.";
			Unsubscribe();
		}

		/// <summary>Clears a stuck run. Exposed because a cancelled reload can strand the flags.</summary>
		public static void Reset()
		{
			EditorApplication.update -= PollTransaction;
			Unsubscribe();
			importInFlight = false;
			activeRequest = null;
			SessionState.SetBool(ImportedAnythingKey, false);
			PendingImports = new List<string>();
			SessionState.SetBool(RunningKey, false);
			Status = string.Empty;
		}
	}
}
