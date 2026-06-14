using UnityEngine;

namespace DreamKnight.Player
{
    /// <summary>
    /// Quản lý animation của Player - Play trực tiếp không cần Animator transitions
    /// 
    /// QUAN TRỌNG: Tên animation phải khớp với TÊN STATE trong Animator Controller,
    /// KHÔNG PHẢI tên Animation Clip!
    /// 
    /// Ví dụ:
    /// - State Name: "Idle" → Dùng "Idle"
    /// - Animation Clip: "player_idle_v2.anim" → KHÔNG dùng tên này!
    /// </summary>
    public class PlayerAnimationController
    {
        private Animator animator;
        private string currentAnimation;
        
        // Animation STATE Names - Phải khớp với tên STATE trong Animator Controller
        // (Click vào state trong Animator window để xem tên)
        public const string IDLE = "Idle";
        public const string ENTER_DOOR = "EnterDoor";
        public const string IDLE_A = "IdleA";
        public const string IDLE_B = "IdleB";
        public const string IDLE_C = "IdleC";
        public const string RUN = "Run";
        public const string JUMP = "Jump";
        public const string FALL = "Fall"; // Legacy single fall
        public const string FALL_START = "Fall_Start"; // Fall phase 1
        public const string FALL_LOOP = "Fall_Loop";   // Fall phase 2
        public const string LAND = "Land"; // Landing animation when touching ground
        public const string CROUCH_START = "Crouch_Start";
        public const string CROUCH_LOOP = "Crouch_Loop";
        public const string CROUCH_END = "Crouch_End";
        
        // Ground Dash animations (4-phase sequence)
        public const string DASH = "Dash"; // Legacy/simple dash
        public const string DODGE_START = "Dodge_Start";
        public const string DODGE_LOOP = "Dodge_Loop";
        public const string DODGE_END = "Dodge_End";
        public const string DODGE_TO_FALL = "Dodge_To_Fall";
        
        // Air Dash animations (3-phase sequence)
        public const string DODGE_AIR_START = "DodgeAir_Start";
        public const string DODGE_AIR_LOOP = "DodgeAir_Loop";
        public const string DODGE_AIR_END = "DodgeAir_End";
        
        public const string WALL_GRAB = "WallGrab"; // Legacy
        public const string WALL_SLIDE = "WallSlide";
        public const string WALL_CLIMB_LOOP = "WallClimb_Loop"; // Wall grab loop
        public const string WALL_CLIMB_END = "WallClimb_Start";  // Exit wall grab
        public const string CLIFF_CLIMB_LOOP = "CliffClimb_Loop"; // Ledge hang idle
        public const string CLIFF_CLIMB_END = "CliffClimb_End";   // Climb up over ledge
        public const string DEATH = "DeathHit";
        public const string DEATH_MELT = "DeathMelt";
        public const string PRAY_START = "Pray_Start";
        public const string PRAY_LOOP = "Pray_Loop";
        public const string PRAY_END = "Pray_End";
        public const string TRAP_RESPAWN_START = "Pl_HauntStart";
        public const string RESPAWN_GUSH = "RespawnGush";
        public const string RESPAWN_APPEAL = "RespawnAppeal";
        public const string ATTACK_1 = "Atk1";
        public const string ATTACK_2 = "Atk2";
        public const string ATTACK_3 = "Atk3";
        public const string ATTACK_AIR = "AtkUp1";
        public const string HIT = "Hit";
        public const string TAKE = "Take";
        public const string DRINK = "Drink";
        public const string SKILL_FASTSHOT = "Skill_FastShot";
        public const string SKILL_ONESHOT = "Skill_OneShot";

        // Ladder animations
        public const string LADDER_IDLE = "Ladder_Idle";
        public const string LADDER_UP = "Ladder_Up";
        public const string LADDER_DOWN = "Ladder_Down";

        public PlayerAnimationController(Animator animator)
        {
            this.animator = animator;
            currentAnimation = "";

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Debug.Log("[PlayerAnimationController] Initialized successfully.");
            }
            else
            {
                Debug.LogWarning("[PlayerAnimationController] Animator or AnimatorController is null!");
                Debug.LogWarning("→ Assign an Animator Controller to Player's Animator component.");
            }
        }

        /// <summary>
        /// Play animation ngay lập tức (không blend)
        /// </summary>
        /// <param name="animationName">Tên STATE trong Animator (VD: "Idle")</param>
        /// <param name="layer">Layer index trong Animator</param>
        public void PlayAnimation(string animationName, int layer = 0)
        {
            if (animator == null) return;
            if (currentAnimation == animationName) return;
            
            try
            {
                animator.Play(animationName, layer);
                currentAnimation = animationName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerAnimationController] Failed to play '{animationName}': {e.Message}");
            }
        }

        /// <summary>
        /// CrossFade animation (transition mượt)
        /// </summary>
        /// <param name="animationName">Tên STATE trong Animator (VD: "Run")</param>
        /// <param name="fadeDuration">Thời gian blend (s), default 0.1s</param>
        /// <param name="layer">Layer index trong Animator</param>
        public void CrossFadeAnimation(string animationName, float fadeDuration = 0.1f, int layer = 0)
        {
            if (animator == null) return;
            if (currentAnimation == animationName) return;
            
            try
            {
                animator.CrossFade(animationName, fadeDuration, layer);
                currentAnimation = animationName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerAnimationController] Failed to crossfade to '{animationName}': {e.Message}");
            }
        }

        /// <summary>
        /// Force play animation (kể cả đang play animation đó)
        /// </summary>
        public void ForcePlayAnimation(string animationName, int layer = 0)
        {
            if (animator == null) return;
            
            try
            {
                animator.Play(animationName, layer, 0f); // Reset về frame 0
                currentAnimation = animationName;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerAnimationController] Failed to force play '{animationName}': {e.Message}");
            }
        }

        /// <summary>
        /// Set animation speed
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator != null)
                animator.speed = speed;
        }

        /// <summary>
        /// Pause/Resume animation
        /// </summary>
        public void PauseAnimation(bool pause)
        {
            if (animator != null)
                animator.speed = pause ? 0f : 1f;
        }

        /// <summary>
        /// Check xem animation có đang play không
        /// </summary>
        public bool IsPlaying(string animationName, int layer = 0)
        {
            if (animator == null) return false;
            return animator.GetCurrentAnimatorStateInfo(layer).IsName(animationName);
        }

        /// <summary>
        /// Get normalized time của animation hiện tại (0-1)
        /// </summary>
        public float GetNormalizedTime(int layer = 0)
        {
            if (animator == null) return 0f;
            return animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
        }

        /// <summary>
        /// Check xem animation đã chạy xong chưa
        /// </summary>
        public bool HasAnimationFinished(int layer = 0)
        {
            return GetNormalizedTime(layer) >= 1f;
        }

        /// <summary>
        /// Get current animation name
        /// </summary>
        public string GetCurrentAnimation()
        {
            return currentAnimation;
        }

        /// <summary>
        /// Debug: Get current state info
        /// </summary>
        public string GetCurrentStateInfo(int layer = 0)
        {
            if (animator == null) return "Animator is null";
            
            var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
            return $"Current State Hash: {stateInfo.shortNameHash}, Normalized Time: {stateInfo.normalizedTime:F2}";
        }
    }
}
