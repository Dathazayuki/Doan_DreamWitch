using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0060GuardState : PlayerState
    {
        private FormStateProxy formStates;
        private bool isEnding;
        private bool isStepping;
        private float phaseTimer;

        private const float GUARD_END_DURATION = 0.12f;
        private const float GUARD_STEP_DURATION = 0.3f;

        public Em0060GuardState(PlayerController controller) : base(controller)
        {
            formStates = new FormStateProxy(controller, PlayerFormId.Em0060);
        }

        public override void Enter()
        {
            isEnding = false;
            isStepping = false;
            phaseTimer = 0f;

            // Bật Guard Collider
            if (controller.FormManager != null && controller.FormManager.ActiveGuardCollider != null)
            {
                controller.FormManager.ActiveGuardCollider.gameObject.SetActive(true);
            }

            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0060States.GUARD_LOOP, 0.05f);
        }

        public override void Update()
        {
            phaseTimer += Time.deltaTime;

            if (isEnding)
                return;

            if (!input.DashHeld)
            {
                isEnding = true;
                phaseTimer = 0f;
                controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0060States.GUARD_END, 0.05f);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                if (!isStepping)
                {
                    isStepping = true;
                    phaseTimer = 0f;
                    controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0060States.GUARD_STEP, 0.05f);
                }
                else if (phaseTimer >= GUARD_STEP_DURATION)
                {
                    isStepping = false;
                    phaseTimer = 0f;
                    controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0060States.GUARD_LOOP, 0.05f);
                }
            }
            else if (isStepping && phaseTimer >= GUARD_STEP_DURATION)
            {
                isStepping = false;
                phaseTimer = 0f;
                controller.AnimationController?.CrossFadeAnimation(PlayerAnimationEm0060States.GUARD_LOOP, 0.05f);
            }
        }

        public override void CheckTransitions()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (input.JumpPressed || input.HasJumpBuffered())
            {
                ExitToNormalState();
                return;
            }

            if (input.AttackPressed)
            {
                ExitToNormalState();
                return;
            }

            if (isEnding && phaseTimer >= GUARD_END_DURATION)
            {
                ExitToNormalState();
            }
        }

        private void ExitToNormalState()
        {
            // Tắt Guard Collider
            if (controller.FormManager != null && controller.FormManager.ActiveGuardCollider != null)
            {
                controller.FormManager.ActiveGuardCollider.gameObject.SetActive(false);
            }

            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(formStates.MoveState);
                return;
            }

            controller.StateMachine.ChangeState(formStates.IdleState);
        }
    }
}
