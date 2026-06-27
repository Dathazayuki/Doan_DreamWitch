using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0020AttackState : PlayerState
    {
        private FormStateProxy formStates;
        private bool isEnding;
        private string currentAnimation;

        public Em0020AttackState(PlayerController controller) : base(controller)
        {
            formStates = new FormStateProxy(controller, PlayerFormId.Em0020);
        }

        public override void Enter()
        {
            isEnding = false;
            controller.Combat?.SetCurrentComboStep(1, false, false);

            // Play attack loop animation
            currentAnimation = FormAnimationHelper.GetAttackAnimation(controller.CurrentFormId); // ATK_LOOP for Em0020
            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        public override void Update()
        {
            // If player is still holding the attack button, keep looping
            if (!isEnding)
            {
                bool holding = false;
                try { holding = UnityEngine.Input.GetButton("Attack"); } catch { holding = false; }

                if (holding)
                {
                    // keep ATK_LOOP playing
                    return;
                }

                // player released attack -> play end animation
                currentAnimation = PlayerAnimationEm0020States.ATK_END;
                controller.AnimationController?.ForcePlayAnimation(currentAnimation);
                isEnding = true;
                return;
            }

            // if in ending phase and finished, exit
            if (isEnding && IsCurrentAttackFinished())
            {
                ExitToLocomotionState();
            }
        }

        private void PlayComboStep(int step)
        {
            // Combo disabled for Em0020; method retained for compatibility.
        }

        private bool IsCurrentAttackFinished()
        {
            if (controller.AnimationController == null)
                return true;

            // Check if still playing this animation
            if (!controller.AnimationController.IsPlaying(currentAnimation))
                return false;

            // Check if animation has completed (normalized time >= 1.0)
            return controller.AnimationController.HasAnimationFinished();
        }

        private void ExitToLocomotionState()
        {
            if (input.JumpPressed || input.HasJumpBuffered())
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(formStates.DashState);
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




