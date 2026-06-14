using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0020DashState : PlayerState
    {
        private FormStateProxy formStates;
        private enum DashPhase { Start, Loop, End }
        private DashPhase currentPhase;
        private float phaseTimer;

        private const float START_DURATION = 0.1f;
        private const float END_DURATION = 0.15f;

        public Em0020DashState(PlayerController controller) : base(controller)
        {
            formStates = new FormStateProxy(controller, PlayerFormId.Em0020);
        }

        public override void Enter()
        {
            movement.StartDash();
            input.ConsumeDashInput();

            currentPhase = DashPhase.Start;
            phaseTimer = 0f;

            controller.AnimationController?.PlayAnimation(PlayerAnimationEm0020States.DASH_START);
        }

        public override void Update()
        {
            phaseTimer += Time.deltaTime;

            switch (currentPhase)
            {
                case DashPhase.Start:
                    if (phaseTimer >= START_DURATION)
                    {
                        currentPhase = DashPhase.Loop;
                        phaseTimer = 0f;
                        controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0020States.DASH_LOOP, 0.05f);
                    }
                    break;

                case DashPhase.Loop:
                    if (!movement.IsDashing)
                    {
                        currentPhase = DashPhase.End;
                        phaseTimer = 0f;
                        controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0020States.DASH_END, 0.1f);
                    }
                    break;

                case DashPhase.End:
                    // wait for end duration before allowing transition
                    break;
            }
        }

        public override void CheckTransitions()
        {
            if (!movement.IsDashing && currentPhase >= DashPhase.End)
            {
                if (currentPhase == DashPhase.End && phaseTimer >= END_DURATION)
                {
                    if (movement.IsGrounded)
                    {
                        if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                            controller.StateMachine.ChangeState(formStates.MoveState);
                        else
                            controller.StateMachine.ChangeState(formStates.IdleState);
                    }
                    else
                    {
                        controller.StateMachine.ChangeState(formStates.JumpState);
                    }
                }
            }
        }
    }
}
