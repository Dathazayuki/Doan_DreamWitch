using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0100AttackState : PlayerState
    {
    private FormStateProxy formStates;
    private const float COMBO_QUEUE_START = 0.25f;
        private const float COMBO_QUEUE_END = 0.9f;
        private const float ATTACK_TIMEOUT = 1f;
        private const float HEAVY_STRIKE_HOLD_DURATION = 0.3f;

        private int comboStep;
        private bool nextAttackQueued;
        private bool isUpAttack;
        private bool isHeavyStrike;
        private float attackTimer;
        private float heavyStrikeHoldTimer;
        private string currentAnimation;

        public Em0100AttackState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0100);
         }

        public override void Enter()
        {
            comboStep = 1;
            nextAttackQueued = false;
            attackTimer = 0f;
            isHeavyStrike = false;
            heavyStrikeHoldTimer = 0f;
            isUpAttack = input.MoveInput.y > 0.1f;

            if (isUpAttack)
            {
                controller.Combat?.SetCurrentComboStep(1, false, true);
                currentAnimation = PlayerAnimationEm0100States.ATK_UP_1;
                controller.AnimationController?.ForcePlayAnimation(currentAnimation);
                return;
            }

            controller.Combat?.SetCurrentComboStep(comboStep, false, false);
            currentAnimation = FormAnimationHelper.GetAttackAnimation(controller.CurrentFormId);
            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        public override void Update()
        {
            attackTimer += Time.deltaTime;

            // Check for heavy strike charging (allow at any combo step)
            if (!isHeavyStrike)
            {
                if (input.AttackHeld)
                {
                    heavyStrikeHoldTimer += Time.deltaTime;
                    if (heavyStrikeHoldTimer >= HEAVY_STRIKE_HOLD_DURATION)
                    {
                        // Transition to heavy strike: clear any queued combo and interrupt
                        isHeavyStrike = true;
                        nextAttackQueued = false;
                        comboStep = 0;
                        controller.Combat?.SetCurrentComboStep(0, true, false);
                        currentAnimation = PlayerAnimationEm0100States.ATK_HEAVY_STRIKE;
                        controller.AnimationController?.ForcePlayAnimation(currentAnimation);
                        attackTimer = 0f;
                        return;
                    }
                }
                else
                {
                    // Reset charge if player releases
                    heavyStrikeHoldTimer = 0f;
                }
            }
            else
            {
                // Heavy strike mode
                if (IsCurrentAttackFinished())
                {
                    ExitToLocomotionState();
                }
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
            // Normal combo queuing (holding does not block queuing)
            if (inQueueWindow && input.AttackPressed)
            {
                nextAttackQueued = true;
            }

            // If current animation hasn't finished, keep updating
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

        private void PlayComboStep(int step)
        {
            attackTimer = 0f;
            controller.Combat?.SetCurrentComboStep(step, false, false);

            switch (step)
            {
                case 2:
                    currentAnimation = PlayerAnimationEm0100States.ATK_2;
                    break;
                case 3:
                    currentAnimation = PlayerAnimationEm0100States.ATK_3;
                    break;
                default:
                    currentAnimation = PlayerAnimationEm0100States.ATK_1;
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
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(formStates.MoveState);
            }
            else
            {
                controller.StateMachine.ChangeState(formStates.IdleState);
            }
        }
    }
}



