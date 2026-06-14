using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0060IdleState : PlayerState
    {
    private FormStateProxy formStates;
    public Em0060IdleState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0060);
         }

        public override void Enter()
        {
            string idleAnim = FormAnimationHelper.GetIdleAnimation(controller.CurrentFormId);
            controller.AnimationController?.CrossFadeAnimation(idleAnim, 0.1f);
        }

        public override void CheckTransitions()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (input.DashHeld)
            {
                controller.StateMachine.ChangeState(formStates.GuardState);
                return;
            }

            if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(formStates.CrouchState);
                return;
            }

            if (input.AttackPressed)
            {
                controller.StateMachine.ChangeState(formStates.AttackState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(formStates.MoveState);
                return;
            }

            if (input.JumpPressed || input.HasJumpBuffered())
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }
        }
    }
}



