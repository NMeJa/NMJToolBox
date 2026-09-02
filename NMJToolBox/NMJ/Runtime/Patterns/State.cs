using UnityEngine;

namespace NMJTools.Patterns
{
	/// <summary>
	/// One state in a <see cref="StateMachine"/>.
	///
	/// <see cref="Update"/> and <see cref="FixedUpdate"/> are virtual rather than abstract: most
	/// states need one of the two at most, and forcing every subclass to write two empty overrides
	/// was pure noise. <see cref="Enter"/> and <see cref="Exit"/> stay abstract because a state that
	/// does nothing on entry or exit is almost always a mistake.
	/// </summary>
	[System.Serializable]
	public abstract class State
	{
		[Tooltip("The component this state acts on. Assign it in the inspector, or pass it in.")]
		[SerializeField] protected MonoBehaviour user;

		protected State() { }

		protected State(MonoBehaviour user) => this.user = user;

		/// <summary>The component this state drives.</summary>
		public MonoBehaviour User
		{
			get => user;
			set => user = value;
		}

		public abstract void Enter();

		public abstract void Exit();

		/// <summary>Per-frame logic. Called by <see cref="StateMachine.Tick"/>.</summary>
		public virtual void Update() { }

		/// <summary>Fixed-step logic. Called by <see cref="StateMachine.FixedTick"/>.</summary>
		public virtual void FixedUpdate() { }
	}
}
