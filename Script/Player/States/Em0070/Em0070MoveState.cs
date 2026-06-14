using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0070MoveState : PlayerState
    {
    private FormStateProxy formStates;
    public Em0070MoveState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0070);
         }

        public override void Enter()
        {
            string moveAnim = FormAnimationHelper.GetMoveAnimation(controller.CurrentFormId);
            controller.AnimationController?.CrossFadeAnimation(moveAnim, 0.1f);
        }

        public override void CheckTransitions()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
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

            if (Mathf.Abs(input.MoveInput.x) < 0.1f)
            {
                controller.StateMachine.ChangeState(formStates.IdleState);
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



