using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Fallback animation controller - Dùng khi chưa có animation clips
    /// Sử dụng Animator parameters thay vì direct play
    /// </summary>
    public class PlayerAnimationFallback
    {
        private Animator animator;

        public PlayerAnimationFallback(Animator animator)
        {
            this.animator = animator;
        }

        /// <summary>
        /// Update animations dựa trên PlayerMovement state
        /// Không cần animation clips - chỉ cần empty Animator Controller với parameters
        /// </summary>
        public void UpdateAnimation(PlayerMovement movement, PlayerInput input)
        {
            if (animator == null) return;

            // Set parameters cho Animator (OLD WAY - vẫn hoạt động nếu có parameters)
            animator.SetBool("IsGrounded", movement.IsGrounded);
            animator.SetFloat("Speed", Mathf.Abs(movement.Velocity.x));
            animator.SetFloat("YVelocity", movement.Velocity.y);

            // Wall grab
            bool isWallGrabbing = movement.IsTouchingWall && !movement.IsGrounded && movement.IsHoldingIntoWall();
            animator.SetBool("IsWallGrabbing", isWallGrabbing);

            if (isWallGrabbing)
            {
                animator.SetFloat("WallClimbSpeed", movement.Velocity.y);
            }

            // Dash trigger
            if (movement.IsDashing)
            {
                animator.SetTrigger("Dash");
            }

            // Jump trigger
            if (input.JumpPressed)
            {
                animator.SetTrigger("Jump");
            }
        }
    }
}
