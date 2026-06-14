using System;
using UnityEngine;

namespace DreamKnight.Player.States
{
    public class PrayState : PlayerState
    {
        private enum PrayPhase
        {
            Start,
            Loop,
            End
        }

        private PrayPhase phase;
        private bool startAnimStarted;
        private bool endAnimStarted;
        private bool loopActionInvoked;
        private float loopTimer;
        private Action onPrayLoop;
        private Func<bool> isLoopComplete;

        public PrayState(PlayerController controller) : base(controller) { }

        public void Configure(Action onPrayLoop, Func<bool> isLoopComplete)
        {
            this.onPrayLoop = onPrayLoop;
            this.isLoopComplete = isLoopComplete;
        }

        public override void Enter()
        {
            phase = PrayPhase.Start;
            startAnimStarted = false;
            endAnimStarted = false;
            loopActionInvoked = false;
            loopTimer = 0f;

            controller.Input?.DisableInput();
            movement.StopMovement();
            movement.SetVelocity(Vector2.zero);

            controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.PRAY_START);
        }

        public override void Update()
        {
            switch (phase)
            {
                case PrayPhase.Start:
                    if (HasAnimationEnded(PlayerAnimationController.PRAY_START, ref startAnimStarted))
                    {
                        phase = PrayPhase.Loop;
                        controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.PRAY_LOOP);
                    }
                    break;

                case PrayPhase.Loop:
                    if (!loopActionInvoked)
                    {
                        loopActionInvoked = true;
                        onPrayLoop?.Invoke();
                    }

                    loopTimer += Time.unscaledDeltaTime;

                    bool loopDone = isLoopComplete == null ? loopActionInvoked : isLoopComplete();
                    if (loopDone && loopTimer >= Mathf.Max(0f, controller.PrayLoopHoldDuration))
                    {
                        phase = PrayPhase.End;
                        controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.PRAY_END);
                    }
                    break;

                case PrayPhase.End:
                    if (HasAnimationEnded(PlayerAnimationController.PRAY_END, ref endAnimStarted))
                    {
                        controller.Input?.EnableInput();
                        ExitToLocomotionState();
                    }
                    break;
            }
        }

        public override void Exit()
        {
            onPrayLoop = null;
            isLoopComplete = null;
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
