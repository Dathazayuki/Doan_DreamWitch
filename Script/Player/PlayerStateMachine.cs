using UnityEngine;

namespace DreamKnight.Player.States
{
    /// <summary>
    /// State Machine đơn giản cho Player
    /// </summary>
    public class PlayerStateMachine
    {
        private PlayerState currentState;

        public PlayerState CurrentState => currentState;

        public void Initialize(PlayerState startingState)
        {
            currentState = startingState;
            currentState.Enter();
        }

        public void ChangeState(PlayerState newState)
        {
            if (currentState == newState) return;

            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }

        public void ForceChangeState(PlayerState newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState.Enter();
        }

        public void Update()
        {
            currentState?.Update();
            currentState?.CheckTransitions();
        }

        public void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }
    }
}
