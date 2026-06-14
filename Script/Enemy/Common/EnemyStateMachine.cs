using System.Collections.Generic;

namespace Mv
{
    public class EnemyStateMachine
    {
        private readonly Dictionary<byte, EnemyState> states = new Dictionary<byte, EnemyState>();

        public EnemyState CurrentState { get; private set; }
        public byte CurrentStateId { get; private set; }

        public void Register(EnemyState state)
        {
            if (state == null) return;
            states[state.StateId] = state;
        }

        public void ChangeState(byte nextStateId)
        {
            if (CurrentState != null && CurrentStateId == nextStateId) return;
            if (!states.TryGetValue(nextStateId, out EnemyState nextState)) return;

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentStateId = nextStateId;
            CurrentState.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}
