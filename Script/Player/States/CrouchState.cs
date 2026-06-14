using UnityEngine;

namespace DreamKnight.Player.States
{
    public class CrouchState : PlayerState
    {
        private enum CrouchPhase { Start, Loop, End }

        private CrouchPhase currentPhase;
        private float phaseTimer;
        private float dropTransitionLockTimer;

        private const float CROUCH_START_DURATION = 0.12f;
        private const float CROUCH_END_DURATION = 0.12f;
        private const float DROP_TRANSITION_LOCK_DURATION = 0.12f;

        public CrouchState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            currentPhase = CrouchPhase.Start;
            phaseTimer = 0f;
            dropTransitionLockTimer = 0f;
            movement.EnterCrouch();
            controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.CROUCH_START);
        }

        public override void Update()
        {
            phaseTimer += Time.deltaTime;
            if (dropTransitionLockTimer > 0f)
                dropTransitionLockTimer -= Time.deltaTime;

            switch (currentPhase)
            {
                case CrouchPhase.Start:
                    if (phaseTimer >= CROUCH_START_DURATION)
                    {
                        currentPhase = CrouchPhase.Loop;
                        phaseTimer = 0f;
                        controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.CROUCH_LOOP, 0.05f);
                    }
                    break;

                case CrouchPhase.Loop:
                    if (input.MoveInput.y >= -0.1f)
                    {
                        if (movement.TryExitCrouch())
                        {
                            currentPhase = CrouchPhase.End;
                            phaseTimer = 0f;
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.CROUCH_END, 0.05f);
                        }
                    }
                    break;

                case CrouchPhase.End:
                    break;
            }
        }

        public override void CheckTransitions()
        {
            if (dropTransitionLockTimer > 0f)
                return;

            if ((input.DashPressed || input.HasDashBuffered()) && movement.CanDash())
            {
                controller.StateMachine.ChangeState(controller.DashState);
                return;
            }

            // Bấm S + Space lúc đang ngồi xổm trên nóc nắp cống -> Rớt qua bộ cống
            if (input.MoveInput.y < -0.1f && (input.JumpPressed || input.HasJumpBuffered()))
            {
                if (movement.TryDropDownFromPlatform())
                {
                    input.ConsumeJumpInput();
                    dropTransitionLockTimer = DROP_TRANSITION_LOCK_DURATION;
                    return;
                }
            }

            if (movement.IsDropping)
                return;

            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            if (currentPhase == CrouchPhase.End && phaseTimer >= CROUCH_END_DURATION)
            {
                if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                {
                    controller.StateMachine.ChangeState(controller.GetFormMoveState(controller.CurrentFormId));
                }
                else
                {
                    controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                }
            }
        }

        public override void Exit()
        {
            movement.TryExitCrouch();
        }
    }
}
