using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0010AttackState : PlayerState
    {
    private FormStateProxy formStates;
    private float attackTimer;
        private const float ATTACK_TIMEOUT = 1f;
        private string currentAnimation;

        public Em0010AttackState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0010);
         }

        public override void Enter()
        {
            attackTimer = 0f;
            controller.Combat?.SetCurrentComboStep(1, false, false);
            currentAnimation = FormAnimationHelper.GetAttackAnimation(controller.CurrentFormId);
            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        public override void Update()
        {
            attackTimer += Time.deltaTime;

            if (IsCurrentAttackFinished())
            {
                ExitToLocomotionState();
            }
        }

        private bool IsCurrentAttackFinished()
        {
            // Timeout safety net
            if (attackTimer >= ATTACK_TIMEOUT)
                return true;

            if (controller.AnimationController == null)
                return false;

            // Check if animation is still playing
            if (!controller.AnimationController.IsPlaying(currentAnimation))
                return true;

            // Check if animation has finished (handles looped animations)
            return controller.AnimationController.HasAnimationFinished();
        }

        private void ExitToLocomotionState()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }
             if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(formStates.DashState);
                return;
            }

            if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(formStates.CrouchState);
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



