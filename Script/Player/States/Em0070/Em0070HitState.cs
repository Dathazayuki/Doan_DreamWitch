using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0070HitState : PlayerState
    {
    private FormStateProxy formStates;
    private const float HIT_START_GRACE_TIME = 0.08f;

        private float hitStateTimer;
        private bool hasHitAnimationStarted;

        public Em0070HitState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0070);
         }

        public override void Enter()
        {
            hitStateTimer = 0f;
            hasHitAnimationStarted = false;

            string hitAnim = FormAnimationHelper.GetHitAnimation(controller.CurrentFormId);
            controller.AnimationController?.ForcePlayAnimation(hitAnim);
            controller.PlayHitCameraShake();
        }

        public override void Update()
        {
            hitStateTimer += Time.deltaTime;
        }

        public override void CheckTransitions()
        {
            if (controller.AnimationController == null)
            {
                ExitToLocomotionState();
                return;
            }

            string hitAnim = FormAnimationHelper.GetHitAnimation(controller.CurrentFormId);
            bool isStillPlayingHit = controller.AnimationController.IsPlaying(hitAnim);
            bool hitFinished = controller.AnimationController.HasAnimationFinished();

            if (!hasHitAnimationStarted)
            {
                if (isStillPlayingHit)
                {
                    hasHitAnimationStarted = true;
                    return;
                }

                if (hitStateTimer < HIT_START_GRACE_TIME)
                    return;
            }

            if (isStillPlayingHit && !hitFinished)
                return;

            ExitToLocomotionState();
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



