using UnityEngine;

namespace DreamKnight.Player.States
{
    public class MoveState : PlayerState
    {
        public MoveState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            // Play Run animation
            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.RUN, 0.1f);
        }

        public override void Update()
        {
            // Có thể adjust animation speed theo tốc độ di chuyển nếu muốn
            // controller.AnimationController?.SetAnimationSpeed(Mathf.Abs(movement.Velocity.x) / movement.MaxSpeed);
        }

        public override void CheckTransitions()
        {
            if (movement.IsTouchingLadder && Mathf.Abs(input.MoveInput.y) > 0.1f)
            {
                if (!(input.MoveInput.y > 0.1f && movement.transform.position.y > movement.CurrentLadderTopY - 0.2f))
                {
                    controller.StateMachine.ChangeState(controller.LadderClimbState);
                    return;
                }
            }

            if (!movement.IsGrounded && movement.IsTouchingWall && (input.JumpPressed || input.HasJumpBuffered()))
            {
                input.ConsumeJumpInput();
                controller.StateMachine.ChangeState(controller.WallClimbState);
                return;
            }

            if (!movement.IsGrounded && movement.Velocity.y < -0.1f)
            {
                controller.StateMachine.ChangeState(controller.JumpState);
                return;
            }

            if (movement.ShouldAutoCrouchForHeadBlock())
            {
                controller.StateMachine.ChangeState(controller.CrouchState);
                return;
            }

            if (movement.IsGrounded && input.MoveInput.y < -0.1f)
            {
                if (input.JumpPressed || input.HasJumpBuffered())
                {
                    if (movement.TryDropDownFromPlatform())
                    {
                        input.ConsumeJumpInput();
                        return;
                    }
                }

                if (movement.IsTouchingLadder)
                {
                    if (movement.transform.position.y > movement.CurrentLadderBottomY + 0.1f)
                    {
                        movement.DropDownFromPlatform();
                        controller.StateMachine.ChangeState(controller.LadderClimbState);
                        return;
                    }
                }

                controller.StateMachine.ChangeState(controller.CrouchState);
                return;
            }

            if (input.AttackPressed)
            {
                controller.StateMachine.ChangeState(controller.AttackState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) < 0.1f)
            {
                controller.StateMachine.ChangeState(controller.IdleState);
                return;
            }

            if (input.JumpPressed || input.HasJumpBuffered())
            {
                controller.StateMachine.ChangeState(controller.JumpState);
                return;
            }

            if ((input.DashPressed || input.HasDashBuffered()) && movement.CanDash())
            {
                controller.StateMachine.ChangeState(controller.DashState);
                return;
            }
        }
    }
}
