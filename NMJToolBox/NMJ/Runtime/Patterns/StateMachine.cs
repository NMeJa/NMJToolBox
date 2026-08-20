namespace NMJTools.Patterns
{
	// ReSharper disable once UnusedType.Global
	public class StateMachine
	{
		private State current;

		public StateMachine(State initialState)
		{
			current = initialState;
			current.Enter();
		}

		// ReSharper disable once UnusedMember.Global
		public void Change(State nextState)
		{
			current.Exit();
			current = nextState;
			current.Enter();
		}
	}
}