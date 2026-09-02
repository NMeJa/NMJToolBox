using System;

namespace NMJTools.Patterns
{
	/// <summary>
	/// A minimal state machine: one current <see cref="State"/>, and the calls that drive it.
	/// </summary>
	/// <remarks>
	/// this changed: the previous version only ever called <c>Enter</c> and <c>Exit</c>. <see cref="State"/>
	/// declares <c>Update</c> and <c>FixedUpdate</c> as abstract - so every state had to implement
	/// them - but nothing in the machine ever invoked either. Every user was obliged to write two
	/// methods that could not run, and had to pump their own state by hand from a MonoBehaviour while
	/// the machine held the reference. <see cref="Tick"/> and <see cref="FixedTick"/> close that gap.
	///
	/// It stays a plain class rather than a MonoBehaviour on purpose: the owner decides whether the
	/// machine ticks in Update, in a coroutine, from a server loop, or not at all.
	/// </remarks>
	public class StateMachine
	{
		private State current;

		/// <summary>Raised after every completed transition, with the state just entered.</summary>
		public event Action<State> StateChanged;

		/// <summary>The state currently running. Null only if constructed with null.</summary>
		public State Current => current;

		public StateMachine(State initialState)
		{
			current = initialState ?? throw new ArgumentNullException(nameof(initialState));
			current.Enter();
		}

		/// <summary>
		/// Exits the current state and enters <paramref name="nextState"/>.
		///
		/// A transition to the state already running is ignored rather than re-entered: an accidental
		/// self-transition otherwise fires Exit and Enter for a state that never actually changed,
		/// which resets timers and replays entry effects. Call <see cref="Restart"/> when a genuine
		/// re-entry is what you want.
		/// </summary>
		public void Change(State nextState)
		{
			if (nextState == null) throw new ArgumentNullException(nameof(nextState));
			if (ReferenceEquals(nextState, current)) return;

			current?.Exit();
			current = nextState;
			current.Enter();

			StateChanged?.Invoke(current);
		}

		/// <summary>Exits and re-enters the current state.</summary>
		public void Restart()
		{
			if (current == null) return;

			current.Exit();
			current.Enter();

			StateChanged?.Invoke(current);
		}

		/// <summary>Drives the current state. Call from <c>Update</c>.</summary>
		public void Tick() => current?.Update();

		/// <summary>Drives the current state's physics step. Call from <c>FixedUpdate</c>.</summary>
		public void FixedTick() => current?.FixedUpdate();

		/// <summary>True when the current state is of the given type.</summary>
		public bool IsIn<T>() where T : State => current is T;
	}
}
