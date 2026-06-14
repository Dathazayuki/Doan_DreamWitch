using UnityEngine;

namespace DreamKnight.Player.States
{
    public class AttackState : PlayerState
    {
        private const float COMBO_QUEUE_START = 0.25f;
        private const float COMBO_QUEUE_END = 0.9f;
        private const float ATTACK_TIMEOUT = 1f;

        public int ComboStep => comboStep;
        public bool IsUpAttack => isUpAttack;

        private int comboStep;
        private bool nextAttackQueued;
        private bool isUpAttack;
        private float attackTimer;
        private string currentAnimation;

        public AttackState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            comboStep = 1;
            nextAttackQueued = false;
            attackTimer = 0f;
            isUpAttack = input.MoveInput.y > 0.1f;
            controller.SetSwordVisible(false);

            if (isUpAttack)
            {
                controller.Combat?.SetCurrentComboStep(1, false, true);
                currentAnimation = PlayerAnimationController.ATTACK_AIR;
                controller.AnimationController?.ForcePlayAnimation(currentAnimation);
                return;
            }

            controller.Combat?.SetCurrentComboStep(comboStep, false, false);
            currentAnimation = PlayerAnimationController.ATTACK_1;
            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        public override void Exit()
        {
            controller.SetSwordVisible(true);
        }

        public override void Update()
        {
            attackTimer += Time.deltaTime;

            if ((input.DashPressed || input.HasDashBuffered()) && movement.CanDash())
            {
                controller.StateMachine.ChangeState(controller.DashState);
                return;
            }

            if (isUpAttack)
            {
                if (IsCurrentAttackFinished())
                {
                    ExitToLocomotionState();
                }
                return;
            }

            float progress = controller.AnimationController?.GetNormalizedTime() ?? 0f;
            bool inQueueWindow = progress >= COMBO_QUEUE_START && progress <= COMBO_QUEUE_END;
            if (inQueueWindow && input.AttackPressed)
            {
                nextAttackQueued = true;
            }

            if (!IsCurrentAttackFinished()) return;

            if (nextAttackQueued && comboStep < 3)
            {
                comboStep++;
                nextAttackQueued = false;
                PlayComboStep(comboStep);
                return;
            }

            ExitToLocomotionState();
        }

        public override void CheckTransitions() { }

        private void PlayComboStep(int step)
        {
            attackTimer = 0f;
            controller.Combat?.SetCurrentComboStep(step, false, false);

            switch (step)
            {
                case 2:
                    currentAnimation = PlayerAnimationController.ATTACK_2;
                    break;
                case 3:
                    currentAnimation = PlayerAnimationController.ATTACK_3;
                    break;
                default:
                    currentAnimation = PlayerAnimationController.ATTACK_1;
                    break;
            }

            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        private bool IsCurrentAttackFinished()
        {
            if (attackTimer >= ATTACK_TIMEOUT)
                return true;

            if (controller.AnimationController == null)
                return false;

            if (!controller.AnimationController.IsPlaying(currentAnimation))
                return false;

            return controller.AnimationController.HasAnimationFinished();
        }

        private void ExitToLocomotionState()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(controller.JumpState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(controller.MoveState);
            }
            else
            {
                controller.StateMachine.ChangeState(controller.IdleState);
            }
        }
    }
}
