using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0060CrouchState : PlayerState
    {
    private FormStateProxy formStates;
    private enum CrouchPhase { Start, Loop, End }

        private CrouchPhase currentPhase;
        private bool isExiting;
        private float exitTimer;
        private const float CROUCH_START_DURATION = 0.12f;
        private const float CROUCH_END_DURATION = 0.12f;
        private float phaseTimer;

        public Em0060CrouchState(PlayerController controller) : base(controller) {
        formStates = new FormStateProxy(controller, PlayerFormId.Em0060);
         }

        public override void Enter()
        {
            isExiting = false;
            exitTimer = 0f;
            phaseTimer = 0f;
            currentPhase = CrouchPhase.Start;
            movement.EnterCrouch();
            string crouchStartAnim = FormAnimationHelper.GetCrouchStartAnimation(controller.CurrentFormId);
            controller.AnimationController?.CrossFadeAnimation(crouchStartAnim, 0.05f);
        }

        public override void Update()
        {
            phaseTimer += Time.deltaTime;

            if (currentPhase == CrouchPhase.Start && phaseTimer >= CROUCH_START_DURATION)
            {
                currentPhase = CrouchPhase.Loop;
                phaseTimer = 0f;
                string crouchLoopAnim = FormAnimationHelper.GetCrouchLoopAnimation(controller.CurrentFormId);
                controller.AnimationController?.CrossFadeAnimation(crouchLoopAnim, 0.05f);
            }

            if (!isExiting && input.MoveInput.y >= -0.1f)
            {
                if (movement.TryExitCrouch())
                {
                    isExiting = true;
                    exitTimer = 0f;
                    currentPhase = CrouchPhase.End;
                    string crouchEndAnim = FormAnimationHelper.GetCrouchEndAnimation(controller.CurrentFormId);
                    controller.AnimationController?.CrossFadeAnimation(crouchEndAnim, 0.05f);
                }
            }
            if (isExiting)
            {
                exitTimer += Time.deltaTime;
            }
        }

        public override void CheckTransitions()
        {
            if (input.MoveInput.y < -0.1f && (input.JumpPressed || input.HasJumpBuffered()))
            {
                if (movement.TryDropDownFromPlatform())
                {
                    input.ConsumeJumpInput();
                    return;
                }
            }

            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(formStates.JumpState);
                return;
            }

            if (isExiting && exitTimer >= CROUCH_END_DURATION)
            {
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

        public override void Exit()
        {
            movement.TryExitCrouch();
        }
    }
}



