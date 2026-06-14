using System;
using UnityEngine;

namespace DreamKnight.Player.States
{
    public class PickUpState : PlayerState
    {
        private Action onPickUpComplete;
        private Action onPickUpCancelled;
        private bool animStarted;
        private bool completed;
        private float timeoutTimer;
        private const float TIMEOUT_DURATION = 1.5f;

        public PickUpState(PlayerController controller) : base(controller) { }

        public void Configure(Action onPickUpComplete, Action onPickUpCancelled = null)
        {
            this.onPickUpComplete = onPickUpComplete;
            this.onPickUpCancelled = onPickUpCancelled;
        }

        public override void Enter()
        {
            animStarted = false;
            completed = false;
            timeoutTimer = 0f;

            controller.Input?.DisableInput();
            movement.StopMovement();
            movement.SetVelocity(Vector2.zero);

            controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.TAKE);
        }

        public override void Update()
        {
            timeoutTimer += Time.deltaTime;

            if (HasAnimationEnded(PlayerAnimationController.TAKE, ref animStarted) || timeoutTimer >= TIMEOUT_DURATION)
            {
                completed = true;
                onPickUpComplete?.Invoke();
                controller.Input?.EnableInput();
                ExitToLocomotionState();
            }
        }

        public override void Exit()
        {
            controller.Input?.EnableInput();
            if (!completed)
                onPickUpCancelled?.Invoke();

            onPickUpComplete = null;
            onPickUpCancelled = null;
        }

        private bool HasAnimationEnded(string animationName, ref bool hasStarted)
        {
            if (controller.AnimationController == null)
                return true;

            bool isPlaying = controller.AnimationController.IsPlaying(animationName);
            if (!hasStarted && isPlaying)
            {
                hasStarted = true;
                return false;
            }

            if (hasStarted)
            {
                if (!isPlaying)
                    return true;

                if (controller.AnimationController.HasAnimationFinished())
                    return true;
            }

            return false;
        }

        private void ExitToLocomotionState()
        {
            if (movement.IsTouchingLadder && Mathf.Abs(input.MoveInput.y) > 0.1f)
            {
                controller.StateMachine.ChangeState(controller.LadderClimbState);
                return;
            }

            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
            {
                controller.StateMachine.ChangeState(controller.GetFormMoveState(controller.CurrentFormId));
            }
            else
            {
                controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
            }
        }
    }
}
