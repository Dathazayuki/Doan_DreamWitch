using UnityEngine;

namespace DreamKnight.Player.States
{
    public class HitState : PlayerState
    {
        private const float HIT_START_GRACE_TIME = 0.08f;

        private float hitStateTimer;
        private bool hasHitAnimationStarted;
        private string hitAnimationName;

        public HitState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            hitStateTimer = 0f;
            hasHitAnimationStarted = false;

            hitAnimationName = FormAnimationHelper.GetHitAnimation(controller.CurrentFormId);
            controller.AnimationController?.ForcePlayAnimation(hitAnimationName);
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

            bool isStillPlayingHit = controller.AnimationController.IsPlaying(hitAnimationName);
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
                controller.StateMachine.ChangeState(controller.JumpState);
                return;
            }

            if (input.MoveInput.y < -0.1f)
            {
                controller.StateMachine.ChangeState(controller.CrouchState);
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(controller.MoveState);
                return;
            }

            Debug.Log("Hit animation finished, returning to Idle");
            controller.StateMachine.ChangeState(controller.IdleState);
        }
    }
}