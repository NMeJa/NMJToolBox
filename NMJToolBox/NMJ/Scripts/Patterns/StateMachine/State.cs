namespace NMJToolBox
{
    [System.Serializable]
    public abstract class State
    {
        [UnityEngine.SerializeField] protected UnityEngine.MonoBehaviour user;
        public abstract void Enter();
        public abstract void Exit();
        public abstract void Update();
        public abstract void FixedUpdate();
    }
}