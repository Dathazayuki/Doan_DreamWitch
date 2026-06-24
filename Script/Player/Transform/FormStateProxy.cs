using DreamKnight.Player.States;

namespace DreamKnight.Player
{
    /// <summary>
    /// Helper class for form states to access other form states dynamically.
    /// Usage in form states: var idle = new FormStateProxy(controller, PlayerFormId.Em0020).IdleState;
    /// </summary>
    public class FormStateProxy
    {
        private PlayerController controller;
        private PlayerFormId formId;

        public FormStateProxy(PlayerController controller, PlayerFormId formId)
        {
            this.controller = controller;
            this.formId = formId;
        }

        public PlayerState IdleState => controller.GetFormIdleState(formId);
        public PlayerState MoveState => controller.GetFormMoveState(formId);
        public PlayerState JumpState => controller.GetFormJumpState(formId);
        public PlayerState CrouchState => controller.GetFormCrouchState(formId);
        public PlayerState GuardState => controller.GetFormGuardState(formId);
        public PlayerState AttackState => controller.GetFormAttackState(formId);
        public PlayerState HitState => controller.GetFormHitState(formId);
        public PlayerState DashState => controller.GetFormDashState(formId);
    }
}
