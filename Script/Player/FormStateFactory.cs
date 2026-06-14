using DreamKnight.Player.States;

namespace DreamKnight.Player
{
    /// <summary>
    /// Factory để tạo và quản lý states cho từng form cụ thể.
    /// Mục đích: Giải phóng PlayerController khỏi việc phải hardcode tất cả form states.
    /// Mỗi form cung cấp states của nó thông qua factory này.
    /// </summary>
    public class FormStateFactory
    {
        public class FormStateSet
        {
            public PlayerState IdleState { get; set; }
            public PlayerState MoveState { get; set; }
            public PlayerState JumpState { get; set; }
            public PlayerState CrouchState { get; set; }
            public PlayerState GuardState { get; set; }
            public PlayerState AttackState { get; set; }
            public PlayerState HitState { get; set; }
            public PlayerState DashState { get; set; }
        }

        private PlayerController controller;

        // Form-specific state sets (tạo on-demand, không hardcode)
        private FormStateSet em0010States;
        private FormStateSet em0020States;
        private FormStateSet em0060States;
        private FormStateSet em0070States;
        private FormStateSet em0100States;

        public FormStateFactory(PlayerController controller)
        {
            this.controller = controller;
        }

        /// <summary>
        /// Lấy state set cho form cụ thể. Tạo on-demand nếu chưa tồn tại.
        /// </summary>
        public FormStateSet GetFormStates(PlayerFormId formId)
        {
            return formId switch
            {
                PlayerFormId.Em0010 => em0010States ??= CreateEm0010States(),
                PlayerFormId.Em0020 => em0020States ??= CreateEm0020States(),
                PlayerFormId.Em0060 => em0060States ??= CreateEm0060States(),
                PlayerFormId.Em0070 => em0070States ??= CreateEm0070States(),
                PlayerFormId.Em0100 => em0100States ??= CreateEm0100States(),
                _ => null,
            };
        }

        /// <summary>
        /// Lấy Idle state cho form cụ thể
        /// </summary>
        public PlayerState GetIdleState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.IdleState;
        }

        /// <summary>
        /// Lấy Hit state cho form cụ thể
        /// </summary>
        public PlayerState GetHitState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.HitState;
        }

        /// <summary>
        /// Lấy Move state cho form cụ thể
        /// </summary>
        public PlayerState GetMoveState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.MoveState;
        }

        /// <summary>
        /// Lấy Jump state cho form cụ thể
        /// </summary>
        public PlayerState GetJumpState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.JumpState;
        }

        /// <summary>
        /// Lấy Crouch state cho form cụ thể
        /// </summary>
        public PlayerState GetCrouchState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.CrouchState;
        }

        /// <summary>
        /// Lấy Guard state cho form cụ thể
        /// </summary>
        public PlayerState GetGuardState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.GuardState;
        }

        /// <summary>
        /// Lấy Attack state cho form cụ thể
        /// </summary>
        public PlayerState GetAttackState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.AttackState;
        }

        public PlayerState GetDashState(PlayerFormId formId)
        {
            var states = GetFormStates(formId);
            return states?.DashState;
        }
        
        private FormStateSet CreateEm0010States()
        {
            return new FormStateSet
            {
                IdleState = new Em0010IdleState(controller),
                MoveState = new Em0010MoveState(controller),
                JumpState = new Em0010JumpState(controller),
                CrouchState = new Em0010CrouchState(controller),
                AttackState = new Em0010AttackState(controller),
                HitState = new Em0010HitState(controller),
                DashState = new Em0010DashState(controller),
            };
        }

        private FormStateSet CreateEm0020States()
        {
            return new FormStateSet
            {
                IdleState = new Em0020IdleState(controller),
                MoveState = new Em0020MoveState(controller),
                JumpState = new Em0020JumpState(controller),
                AttackState = new Em0020AttackState(controller),
                HitState = new Em0020HitState(controller),
                DashState = new Em0020DashState(controller),
            };
        }

        private FormStateSet CreateEm0060States()
        {
            return new FormStateSet
            {
                IdleState = new Em0060IdleState(controller),
                MoveState = new Em0060MoveState(controller),
                JumpState = new Em0060JumpState(controller),
                CrouchState = new Em0060CrouchState(controller),
                GuardState = new Em0060GuardState(controller),
                AttackState = new Em0060AttackState(controller),
                HitState = new Em0060HitState(controller),
            };
        }

        private FormStateSet CreateEm0070States()
        {
            return new FormStateSet
            {
                IdleState = new Em0070IdleState(controller),
                MoveState = new Em0070MoveState(controller),
                JumpState = new Em0070JumpState(controller),
                CrouchState = new Em0070CrouchState(controller),
                AttackState = new Em0070AttackState(controller),
                HitState = new Em0070HitState(controller),
            };
        }

        private FormStateSet CreateEm0100States()
        {
            return new FormStateSet
            {
                IdleState = new Em0100IdleState(controller),
                MoveState = new Em0100MoveState(controller),
                JumpState = new Em0100JumpState(controller),
                CrouchState = new Em0100CrouchState(controller),
                AttackState = new Em0100AttackState(controller),
                HitState = new Em0100HitState(controller),
            };
        }
    }
}
