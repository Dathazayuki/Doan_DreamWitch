using UnityEngine;

namespace DreamKnight.Player.States
{
    /// <summary>
    /// State nhảy/rơi - chỉ xử lý jump và fall animations.
    /// Wall climb được xử lý riêng bởi WallClimbState.
    /// </summary>
    public class JumpState : PlayerState
    {
        private enum FallPhase { None, Start, Loop }
        private FallPhase currentFallPhase;
        private float fallPhaseTimer;
        private const float FALL_START_DURATION = 0.15f;

        private bool isLanding;
        private float landingTimer;
        private const float LAND_DURATION = 0.2f;

        public JumpState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            if (input.JumpPressed || input.HasJumpBuffered())
            {
                movement.Jump();
                input.ConsumeJumpInput();
            }

            currentFallPhase = FallPhase.None;
            fallPhaseTimer = 0f;
            isLanding = false;
            landingTimer = 0f;

            controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.JUMP);
        }

        public override void Update()
        {
            if (isLanding)
            {
                landingTimer += Time.deltaTime;
                return;
            }

            if (movement.Velocity.y > 0.5f)
            {
                currentFallPhase = FallPhase.None;
                fallPhaseTimer = 0f;
                controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.JUMP, 0.05f);
            }
            else if (movement.Velocity.y < -0.5f)
            {
                switch (currentFallPhase)
                {
                    case FallPhase.None:
                        currentFallPhase = FallPhase.Start;
                        fallPhaseTimer = 0f;
                        controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.FALL_START, 0.1f);
                        break;

                    case FallPhase.Start:
                        fallPhaseTimer += Time.deltaTime;
                        if (fallPhaseTimer >= FALL_START_DURATION)
                        {
                            currentFallPhase = FallPhase.Loop;
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.FALL_LOOP, 0.1f);
                        }
                        break;

                    case FallPhase.Loop:
                        break;
                }
            }
        }

        public override void CheckTransitions()
        {
            if (!movement.IsGrounded && movement.CanWallGrab && movement.IsTouchingWall && movement.IsHoldingIntoWall())
            {
                controller.StateMachine.ChangeState(controller.WallClimbState);
                return;
            }

            if ((input.DashPressed || input.HasDashBuffered()) && movement.CanDash())
            {
                controller.StateMachine.ChangeState(controller.DashState);
                return;
            }

            if (input.AttackPressed)
            {
                controller.StateMachine.ChangeState(controller.AttackState);
                return;
            }

            // Cho phep bat thang khi dang o tren khong neu co input doc.
            if (movement.IsTouchingLadder && Mathf.Abs(input.MoveInput.y) > 0.1f)
            {
                bool isAboveLadderTop = input.MoveInput.y > 0.1f && movement.transform.position.y > movement.CurrentLadderTopY - 0.2f;
                if (!isAboveLadderTop)
                {
                    controller.StateMachine.ChangeState(controller.LadderClimbState);
                    return;
                }
            }

            // Chuyển sang CliffClimb khi đang rơi và chạm mép (tự động bám)
            if (!movement.IsGrounded && movement.IsTouchingLedge && movement.Velocity.y <= 0f
                && controller.StateMachine.CurrentState != controller.CliffClimbState)
            {
                controller.StateMachine.ChangeState(controller.CliffClimbState);
                return;
            }

            // Chuyển sang WallClimb khi đang chạm tường và người chơi nhấn Jump
            if (!movement.IsGrounded && movement.CanWallGrab && movement.IsTouchingWall && (input.JumpPressed || input.HasJumpBuffered()))
            {
                input.ConsumeJumpInput();
                controller.StateMachine.ChangeState(controller.WallClimbState);
                return;
            }

            // Jump tiếp (air jump nếu còn)
            if (input.JumpPressed || input.HasJumpBuffered())
            {
                movement.Jump();
                input.ConsumeJumpInput();
                currentFallPhase = FallPhase.None;
                fallPhaseTimer = 0f;
                isLanding = false;
                landingTimer = 0f;
                controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.JUMP);
            }

            // Landing
            if (movement.IsGrounded && movement.Velocity.y <= 0.1f)
            {
                if (!isLanding)
                {
                    isLanding = true;
                    landingTimer = 0f;
                    controller.AnimationController?.PlayAnimation(PlayerAnimationController.LAND);
                    return;
                }

                if (landingTimer >= LAND_DURATION)
                {
                    if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                        controller.StateMachine.ChangeState(controller.MoveState);
                    else
                        controller.StateMachine.ChangeState(controller.IdleState);
                    return;
                }
            }
        }

        public override void Exit() { }
    }
}

