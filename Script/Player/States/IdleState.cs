using UnityEngine;

namespace DreamKnight.Player.States
{
    public class IdleState : PlayerState
    {
        private const float IDLE_RANDOM_DELAY = 15f;
        private float idleTimer;
        private bool isPlayingIdleVariant;
        private string currentIdleVariant;
        private static readonly string[] idleVariants =
        {
            PlayerAnimationController.IDLE_A,
            PlayerAnimationController.IDLE_B,
            PlayerAnimationController.IDLE_C
        };

        public IdleState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            idleTimer = 0f;
            isPlayingIdleVariant = false;
            currentIdleVariant = string.Empty;
            // Play Idle animation với CrossFade để transition mượt
            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.IDLE, 0.1f);
        }

        public override void Update()
        {
            if (!movement.IsGrounded || Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                idleTimer = 0f;
                isPlayingIdleVariant = false;
                currentIdleVariant = string.Empty;
                return;
            }

            if (isPlayingIdleVariant)
            {
                bool variantFinished = controller.AnimationController != null &&
                                       controller.AnimationController.IsPlaying(currentIdleVariant) &&
                                       controller.AnimationController.HasAnimationFinished();

                if (variantFinished)
                {
                    controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.IDLE, 0.1f);
                    isPlayingIdleVariant = false;
                    currentIdleVariant = string.Empty;
                    idleTimer = 0f;
                }

                return;
            }

            idleTimer += Time.deltaTime;
            if (idleTimer < IDLE_RANDOM_DELAY) return;

            int index = Random.Range(0, idleVariants.Length);
            currentIdleVariant = idleVariants[index];
            isPlayingIdleVariant = true;
            controller.AnimationController?.ForcePlayAnimation(currentIdleVariant);
            idleTimer = 0f;
        }

        public override void CheckTransitions()
        {
            if (movement.IsTouchingLadder && Mathf.Abs(input.MoveInput.y) > 0.1f)
            {
                // Nếu bấm W mà đang ở cao bằng đỉnh thang thì không leo lên nữa (ngăn lỗi giật nháy vòng lặp)
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

                // Tiền trảm hậu tấu: Nếu dưới chân là 1 cái thang, thì tụt qua khỏi cái nắp platform và bám thang thay vì lùm xùm vào Crouch
                if (movement.IsTouchingLadder)
                {
                    // Đang ấn S, nhưng nếu đang ở tuốt dưới Bê tông bệt/Mặt đất đáy cống thì cấm chui lọt qua mặt đất
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

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(controller.MoveState);
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
