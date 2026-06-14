using UnityEngine;

namespace DreamKnight.Player.States
{
    public class DashState : PlayerState
    {
        private enum DashPhase { Start, Loop, End, ToFall }
        private DashPhase currentPhase;
        private float phaseTimer;
        private bool isAirDash; // TRUE = Air Dash, FALSE = Ground Dash
        
        // Timing cho từng phase (có thể adjust)
        private const float START_DURATION = 0.1f;  // Dodge_Start / DodgeAir_Start duration
        private const float END_DURATION = 0.15f;   // Dodge_End / DodgeAir_End duration
        
        public DashState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            movement.StartDash();
            input.ConsumeDashInput();
            
            // Xác định dash trên Ground hay Air
            isAirDash = !movement.IsGrounded;
            
            // Start với animation phù hợp
            currentPhase = DashPhase.Start;
            phaseTimer = 0f;
            
            if (isAirDash)
            {
                // Air Dash: DodgeAir_Start
                controller.AnimationController?.PlayAnimation(PlayerAnimationController.DODGE_AIR_START);
            }
            else
            {
                // Ground Dash: Dodge_Start
                controller.AnimationController?.PlayAnimation(PlayerAnimationController.DODGE_START);
            }
        }

        public override void Update()
        {
            phaseTimer += Time.deltaTime;
            
            // Update dash animation phases
            switch (currentPhase)
            {
                case DashPhase.Start:
                    // Sau START_DURATION → Chuyển sang Loop
                    if (phaseTimer >= START_DURATION)
                    {
                        currentPhase = DashPhase.Loop;
                        phaseTimer = 0f;
                        
                        if (isAirDash)
                        {
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.DODGE_AIR_LOOP, 0.05f);
                        }
                        else
                        {
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.DODGE_LOOP, 0.05f);
                        }
                    }
                    break;
                    
                case DashPhase.Loop:
                    // Khi dash sắp kết thúc → Chuyển sang End
                    if (!movement.IsDashing)
                    {
                        currentPhase = DashPhase.End;
                        phaseTimer = 0f;
                        
                        if (isAirDash)
                        {
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.DODGE_AIR_END, 0.1f);
                        }
                        else
                        {
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.DODGE_END, 0.1f);
                        }
                    }
                    break;
                    
                case DashPhase.End:
                    // Sau END_DURATION → Check grounded
                    if (phaseTimer >= END_DURATION)
                    {
                        // Ground Dash: Có thể chuyển sang Dodge_To_Fall nếu rơi
                        // Air Dash: Không có ToFall phase, chuyển thẳng sang JumpState
                        if (!isAirDash && !movement.IsGrounded)
                        {
                            currentPhase = DashPhase.ToFall;
                            phaseTimer = 0f;
                            controller.AnimationController?.CrossFadeAnimation(PlayerAnimationController.DODGE_TO_FALL, 0.1f);
                        }
                        // Nếu grounded hoặc Air Dash xong → Exit state trong CheckTransitions
                    }
                    break;
                    
                case DashPhase.ToFall:
                    // Dodge_To_Fall animation đang play → Cho phép transition trong CheckTransitions
                    break;
            }
        }

        public override void CheckTransitions()
        {
            if (movement.IsDashing)
                return;

            if (!movement.IsGrounded)
            {
                controller.StateMachine.ChangeState(controller.GetFormJumpState(controller.CurrentFormId));
                return;
            }

            if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                controller.StateMachine.ChangeState(controller.GetFormMoveState(controller.CurrentFormId));
            else
                controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
        }
    }
}
