namespace DreamKnight.Player
{
    /// <summary>
    /// Helper to map PlayerFormId to animation state names from respective animation constant classes.
    /// Each form (Em0010, Em0020, Em0060, Em0070, Em0100) has different animation constants.
    /// </summary>
    public static class FormAnimationHelper
    {
        /// <summary>
        /// Get the Idle animation for a given form.
        /// </summary>
        public static string GetIdleAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.IDLE,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.IDLE,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.IDLE,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.IDLE,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.IDLE,
                _ => PlayerAnimationController.IDLE, // fallback to human
            };
        }

        /// <summary>
        /// Get the Move/Run animation for a given form.
        /// </summary>
        public static string GetMoveAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.RUN,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.RUN,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.RUN,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.RUN,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.RUN,
                _ => PlayerAnimationController.RUN,
            };
        }

        /// <summary>
        /// Get the Jump animation for a given form.
        /// </summary>
        public static string GetJumpAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.JUMP,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.JUMP,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.JUMP,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.JUMP,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.JUMP,
                _ => PlayerAnimationController.JUMP,
            };
        }

        /// <summary>
        /// Get the Land animation for a given form (only Em0060 has this).
        /// </summary>
        public static string GetLandAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.LAND,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.LAND,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.LAND,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.LAND,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.LAND,
                _ => "",
            };
        }

        // /// <summary>
        // /// Check if the form has a multi-phase crouch (start/loop/end).
        // /// </summary>
        // public static bool HasCrouchLoop(PlayerFormId formId)
        // {
        //     return formId == PlayerFormId.Em0060;
        // }

        /// <summary>
        /// Get the Crouch Start animation (used only for multi-phase crouch like Em0060).
        /// For other forms, returns the simple crouch animation.
        /// </summary>
        public static string GetCrouchStartAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.CROUCH_START,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.CROUCH_START,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.CROUCH_START,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.CROUCH_START,
                _ => PlayerAnimationController.CROUCH_START,
            };
        }

        /// <summary>
        /// Get the Crouch Loop animation (used only for Em0060).
        /// </summary>
        public static string GetCrouchLoopAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.CROUCH_LOOP,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.CROUCH_LOOP,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.CROUCH_LOOP,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.CROUCH_LOOP,
                _ => "", // other forms don't have loop phase
            };
        }

        /// <summary>
        /// Get the Crouch End animation for a given form.
        /// </summary>
        public static string GetCrouchEndAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.CROUCH_END,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.CROUCH_END,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.CROUCH_END,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.CROUCH_END,
                _ => PlayerAnimationController.CROUCH_END,
            };
        }

        /// <summary>
        /// Get the Attack animation for a given form.
        /// </summary>
        public static string GetAttackAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.ATK,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.ATK_LOOP,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.ATK_1,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.ATK,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.ATK_1,
                _ => PlayerAnimationController.ATTACK_1,
            };
        }

        /// <summary>
        /// Get the Hit animation for a given form.
        /// </summary>
        public static string GetHitAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.HIT,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.HIT,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.HIT,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.HIT,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.HIT,
                _ => PlayerAnimationController.HIT,
            };
        }

        /// <summary>
        /// Get the Respawn Gush animation for a given form.
        /// </summary>
        public static string GetRespawnGushAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.RESPAWN_GUSH,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.RESPAWN_GUSH,
                PlayerFormId.Em0060 => PlayerAnimationEm0060States.RESPAWN_GUSH,
                PlayerFormId.Em0070 => PlayerAnimationEm0070States.RESPAWN_GUSH,
                PlayerFormId.Em0100 => PlayerAnimationEm0100States.RESPAWN_GUSH,
                _ => "", // human doesn't have respawn gush
            };
        }

        public static string GetDashAnimation(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => PlayerAnimationEm0010States.DASH_LOOP,
                PlayerFormId.Em0020 => PlayerAnimationEm0020States.DASH_LOOP,
                _ => "", // human doesn't have respawn gush
            };
        }
    }
}
