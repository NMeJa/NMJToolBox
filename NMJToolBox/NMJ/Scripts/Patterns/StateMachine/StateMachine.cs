namespace NMJToolBox
{
    public class StateMachine
    {
        private State current;

        public StateMachine(State initialState)
        {
            current = initialState;
            current.Enter();
        }

        public void Change(State nextState)
        {
            current.Exit();
            current = nextState;
            current.Enter();
        }
    }
}