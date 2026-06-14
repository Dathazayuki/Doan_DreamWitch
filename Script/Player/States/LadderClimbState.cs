using UnityEngine;

namespace DreamKnight.Player.States
{
    public class LadderClimbState : PlayerState
    {
        private const float CLIMB_SPEED = 8f;
        private float stateTimer;

        public LadderClimbState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            stateTimer = 0f;
            // Reset velocity and stop gravity
            movement.SetVelocity(Vector2.zero);
            movement.FreezeVertical();

            // Căn chỉnh Player vào chính giữa tâm nhảy của thang tuyệt đối lập tức
            if (movement.IsTouchingLadder)
            {
                Vector3 pos = controller.transform.position;
                pos.x = movement.CurrentLadderX; // Gán đúng vị trí tâm

                // Đứng trên nền nắp cống bấm S thì kéo người lọt qua nắp
                if (movement.IsGrounded && input.MoveInput.y < -0.1f)
                {
                    // Chắc chắn là đang đứng ở ĐỈNH thang chứ không phải ĐÁY thang
                    if (controller.transform.position.y >= movement.CurrentLadderTopY - 0.5f)
                    {
                        pos.y -= 1f; 
                    }
                }

                controller.transform.position = pos;
            }

            // Mặc định dừng tại chỗ
            controller.AnimationController?.PlayAnimation(PlayerAnimationController.LADDER_IDLE);
        }

        public override void Update()
        {
            stateTimer += Time.deltaTime;

            float verticalInput = input.MoveInput.y;

            // Khóa chặt vị trí X ở chính giữa thang liên tục để tránh rớt/lệch
            if (movement.IsTouchingLadder)
            {
                Vector3 pos = controller.transform.position;
                pos.x = movement.CurrentLadderX;
                controller.transform.position = pos;
            }

            // Di chuyển dọc theo thang
            movement.Rb.linearVelocity = new Vector2(0f, verticalInput * CLIMB_SPEED);

            // Xử lý Animation dựa vào đầu vào
            if (verticalInput > 0.1f)
            {
                controller.AnimationController?.PlayAnimation(PlayerAnimationController.LADDER_UP);
            }
            else if (verticalInput < -0.1f)
            {
                controller.AnimationController?.PlayAnimation(PlayerAnimationController.LADDER_DOWN);
            }
            else
            {
                controller.AnimationController?.PlayAnimation(PlayerAnimationController.LADDER_IDLE);
            }
        }

        public override void CheckTransitions()
        {
            // Khoá an toàn: Trong 0.15s tụt cống, bất khả xâm phạm, cấm tự động out khỏi Ladder
            if (stateTimer < 0.15f) return;

            // Nếu bấm Jump thì nhảy ra khỏi thang
            if (input.JumpPressed || input.HasJumpBuffered())
            {
                input.ConsumeJumpInput();
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }
            // Nếu bấm trái/phải thì rời khỏi thang
            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(movement.IsGrounded ? controller.GetIdleStateForCurrentForm() : controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            // Nếu leo lên hẳn phía trên cùng (đứng trên mặt đất và body đã thoát khỏi thang hoàn toàn)
            if (movement.IsGrounded && input.MoveInput.y > 0.1f && !movement.IsTouchingLadder)
            {
                controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                return;
            }

            // 2. Không còn chạm trục thân chính VÀ không có thang bên dưới -> Rớt ngang/Rớt đáy
            if (!movement.IsTouchingLadder)
            {
                // Đá ra Khỏi Thang khi Box Collider Thân hình hoàn toàn rời khỏi vùng Box Collider thang
                if (movement.IsGrounded)
                {
                    controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                }
                else
                {
                    controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                }
                return;
            }

            // 3. Nếu đang trèo xuống và chạm ĐÁY
            if (input.MoveInput.y < -0.1f)
            {
                // Đo tọa độ gót chân (transform.position.y hoặc center box minus extent.y)
                Collider2D col = controller.GetComponent<Collider2D>();
                float playerBottomY = col != null ? col.bounds.min.y : controller.transform.position.y - 0.5f;

                // Nếu bạn bấm S mà mũi gót chân của Player dưới ngưỡng Đáy Thang -> Đá văng ra để tớt xuống tiếp
                if (playerBottomY <= movement.CurrentLadderBottomY + 0.05f)
                {
                    controller.StateMachine.ChangeState(movement.IsGrounded ? controller.GetIdleStateForCurrentForm() : controller.GetFormJumpState(controller.CurrentFormId));
                    return;
                }
            }

            // 4. Nếu đang leo xuống mà lở đâm sầm đầu vào mặt đất bự (Không phải lúc mới lọt hố)
            if (movement.IsGrounded && input.MoveInput.y < -0.1f && !movement.IsDropping)
            {
                if (movement.transform.position.y < movement.CurrentLadderTopY - 0.5f)
                {
                    controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                    return;
                }
            }
        }

        public override void Exit()
        {
            // Trả lại trọng lực như cũ khi rời khỏi thang
            movement.UnfreezeVertical();
        }
    }
}
