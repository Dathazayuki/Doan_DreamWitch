using UnityEngine;

namespace DreamKnight.Player.States
{
    /// <summary>
    /// State bám tường:
    /// - Điều kiện vào: !IsGrounded + IsTouchingWall + nhấn Jump
    /// - Nhấn S/DownArrow → rơi (về JumpState)
    /// - Nhấn Jump → wall jump (về JumpState)
    /// - Chạm đất → về Idle/Move
    /// </summary>
    public class WallClimbState : PlayerState
    {
        private bool isPlayingClimbEnd;
        private float climbEndTimer;
        private const float CLIMB_END_DURATION = 0.2f;
        private float jumpInputLockTimer;
        private const float JUMP_INPUT_LOCK_DURATION = 0.08f;

        public WallClimbState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            isPlayingClimbEnd = false;
            climbEndTimer = 0f;
            jumpInputLockTimer = JUMP_INPUT_LOCK_DURATION;

            // Freeze vertical velocity khi bám tường
            movement.FreezeVertical();
            movement.StopMovement();

            Debug.Log("[WallClimbState] Enter → WallClimb_Loop");
            controller.AnimationController?.PlayAnimation(PlayerAnimationController.WALL_CLIMB_LOOP);
        }

        public override void Update()
        {
            // Đang play ClimbEnd, chờ xong rồi mới xử lý tiếp
            if (isPlayingClimbEnd)
            {
                climbEndTimer += Time.deltaTime;
            }

            if (jumpInputLockTimer > 0f)
            {
                jumpInputLockTimer -= Time.deltaTime;
            }
        }

        public override void CheckTransitions()
        {
            // Jump từ tường → wall jump
            if (jumpInputLockTimer <= 0f && (input.JumpPressed || input.HasJumpBuffered()))
            {
                Debug.Log("[WallClimbState] Jump pressed → WallClimb_End + wall jump");
                PlayClimbEndAndExit();
                movement.WallClimbJump();
                input.ConsumeJumpInput();
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            // Dash từ tường
            if ((input.DashPressed || input.HasDashBuffered()) && movement.CanDash())
            {
                PlayClimbEndAndExit();
                controller.StateMachine.ChangeState(controller.GetFormDashState(controller.CurrentFormId));
                return;
            }

            // Chạm đất
            if (movement.IsGrounded)
            {
                PlayClimbEndAndExit();
                if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                    controller.StateMachine.ChangeState(controller.GetFormMoveState(controller.CurrentFormId));
                else
                    controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                return;
            }

            // Chuyển sang CliffClimb khi phát hiện mép phía trên (đã leo đến đỉnh tường)
            if (movement.IsTouchingLedge)
            {
                Debug.Log("[WallClimbState] Ledge detected → CliffClimbState");
                PlayClimbEndAndExit();
                controller.StateMachine.ChangeState(controller.CliffClimbState);
                return;
            }

            // Nhấn S/DownArrow để thoát bám tường
            if (input.MoveInput.y < -0.1f)
            {
                Debug.Log("[WallClimbState] Pressed S → WallClimb_End + fall");
                PlayClimbEndAndExit();
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            // Mất contact tường → rơi
            if (!movement.IsHoldingIntoWall())
            {
                Debug.Log("[WallClimbState] Released wall direction → WallClimb_End + fall");
                PlayClimbEndAndExit();
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            if (!movement.IsTouchingWall)
            {
                Debug.Log("[WallClimbState] Lost wall contact → WallClimb_End + fall");
                PlayClimbEndAndExit();
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }
        }

        public override void Exit()
        {
            movement.UnfreezeVertical();
        }

        public override void FixedUpdate()
        {
            movement.SetVelocity(Vector2.zero);
        }

        private void PlayClimbEndAndExit()
        {
            Debug.Log("[WallClimbState] → WallClimb_End");
            controller.AnimationController?.PlayAnimation(PlayerAnimationController.WALL_CLIMB_END);
        }
    }
}
