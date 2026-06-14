using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0070AttackState : PlayerState, IAttackTriggerHandler
    {
    private FormStateProxy formStates;
    private float attackTimer;
        private const float ATTACK_TIMEOUT = 1f;
        private string currentAnimation;

        public Em0070AttackState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0070);
         }

        public override void Enter()
        {
            attackTimer = 0f;
            controller.Combat?.SetCurrentComboStep(1, false, false);
            currentAnimation = FormAnimationHelper.GetAttackAnimation(controller.CurrentFormId);
            controller.AnimationController?.ForcePlayAnimation(currentAnimation);
        }

        public bool OnAttackHitTriggered()
        {
            SummonFlyPile();
            return true;
        }

        public void SummonFlyPile()
        {
            var bodyRef = controller.FormManager?.ActiveBodyRef;
            if (bodyRef != null && bodyRef.FlyPilePrefab != null)
            {
                Vector2 initialDir = controller.Movement.FacingRight ? Vector2.right : Vector2.left;
                Vector3 spawnPos = bodyRef.FlyPileSpawnPoint != null ? bodyRef.FlyPileSpawnPoint.position : controller.transform.position;
                GameObject go = Object.Instantiate(bodyRef.FlyPilePrefab, spawnPos, Quaternion.identity);
                var flyPile = go.GetComponent<DreamKnight.Systems.Skill.FlyPile>();
                if (flyPile != null)
                {
                    float damage = 20f;
                    if (controller.Combat != null)
                    {
                        damage = controller.Combat.GetCurrentDamage();
                    }
                    flyPile.Initialize(controller.gameObject, damage, initialDir);
                }
            }
        }

        public override void Update()
        {
            attackTimer += Time.deltaTime;

            if (IsCurrentAttackFinished())
            {
                ExitToLocomotionState();
            }
        }

        private bool IsCurrentAttackFinished()
        {
            // Timeout safety net
            if (attackTimer >= ATTACK_TIMEOUT)
                return true;

            if (controller.AnimationController == null)
                return false;

            // Check if animation is still playing
            if (!controller.AnimationController.IsPlaying(currentAnimation))
                return true;

            // Check if animation has finished (handles looped animations)
            return controller.AnimationController.HasAnimationFinished();
        }

        private void ExitToLocomotionState()
        {
            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(formStates.CrouchState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(formStates.MoveState);
                return;
            }

            controller.StateMachine.ChangeState(formStates.IdleState);
        }
    }
}



