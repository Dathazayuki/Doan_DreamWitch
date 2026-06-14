using UnityEngine;

namespace DreamKnight.Player.States
{
    public class Em0070JumpState : PlayerState
    {
        private FormStateProxy formStates;
        private bool isLanding;
        private float landingTimer;
        private const float LAND_DURATION = 0.2f;

        public Em0070JumpState(PlayerController controller) : base(controller)
        {
            formStates = new FormStateProxy(controller, PlayerFormId.Em0070);
        }

        public override void Enter()
        {
            if (input.JumpPressed || input.HasJumpBuffered())
            {
                movement.Jump();
                input.ConsumeJumpInput();
            }

            isLanding = false;
            landingTimer = 0f;

            string jumpAnim = FormAnimationHelper.GetJumpAnimation(controller.CurrentFormId);
            controller.AnimationController?.CrossFadeAnimation(jumpAnim, 0.05f);
        }

        public override void Update()
        {
            if (isLanding)
            {
                landingTimer += Time.deltaTime;
            }
        }

        public override void CheckTransitions()
        {
            if (input.AttackPressed)
            {
                controller.StateMachine.ChangeState(formStates.AttackState);
                return;
            }

            if (movement.IsGrounded && movement.Velocity.y <= 0.1f)
            {
                if (!isLanding)
                {
                    isLanding = true;
                    landingTimer = 0f;

                    string landAnim = FormAnimationHelper.GetLandAnimation(controller.CurrentFormId);
                    if (!string.IsNullOrWhiteSpace(landAnim))
                        controller.AnimationController?.PlayAnimation(landAnim);

                    return;
                }

                if (landingTimer >= LAND_DURATION)
                {
                    if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                    {
                        controller.StateMachine.ChangeState(formStates.MoveState);
                    }
                    else
                    {
                        controller.StateMachine.ChangeState(formStates.IdleState);
                    }

                    return;
                }
            }
        }
    }
}



