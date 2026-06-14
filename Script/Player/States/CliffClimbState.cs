using UnityEngine;

namespace DreamKnight.Player.States
{
    /// <summary>
    /// State bám mép (Ledge / Cliff Climb):
    ///   Phase Idle   : Nhân vật treo ở mép → Play CliffClimb_Loop
    ///                  - W/Up hoặc Jump → bắt đầu leo lên
    ///                  - S/Down          → thả tay, rơi
    ///                  - Mất tiếp xúc   → rơi
    ///   Phase ClimbUp: Nội suy vị trí 2 pha (kéo lên mép → bước qua)
    ///                  → Play CliffClimb_End → về Idle/Move
    /// </summary>
    public class CliffClimbState : PlayerState
    {
        // ── Phases ────────────────────────────────────────────────
        private enum ClimbPhase { Idle, ClimbLoopIntro, ClimbingUp, CrouchStart, CrouchLoop, CrouchEnd }
        private ClimbPhase currentPhase;

        // ── Climb-up position interpolation ───────────────────────
        private float phaseTimer;
        private Vector2 enterPos;
        private Vector2 targetPos0;   // đích cuối phase 0 (kéo lên mép)
        private Vector2 targetPos1;   // đích cuối phase 1 (bước qua mép)

        private const float PHASE0_DURATION   = 0.1f;   // giây kéo lên
        private const float PHASE1_DURATION   = 0.2f;  // giây bước qua
        private const float PHASE0_UP_OFFSET  = 1.0f;   // lên bao nhiêu đơn vị
        private const float PHASE1_FWD_OFFSET = 1.2f;   // tiến về phía trước
        private const float PHASE1_UP_OFFSET  = 0.35f;  // lên thêm khi bước qua
        private const float CROUCH_LOOP_DURATION = 0.15f; // giữ Crouch_Loop ngắn trước khi đứng dậy
        private float crouchLoopTimer;

        private const float UNSTUCK_STEP = 0.02f;
        private const float UNSTUCK_MAX_HEIGHT = 0.7f;

        // ── Input lock khi mới vào ────────────────────────────────
        private float inputLockTimer;
        private const float INPUT_LOCK_DURATION = 0.12f;

        /// <summary>Cờ để DashState biết có cần cancel khi vừa leo xong không.</summary>
        public bool IsCancelDodge { get; private set; }

        public CliffClimbState(PlayerController controller) : base(controller) { }

        // ─────────────────────────────────────────────────────────

        public override void Enter()
        {
            phaseTimer     = 0f;
            inputLockTimer = INPUT_LOCK_DURATION;
            IsCancelDodge  = false;

            movement.FreezeVertical();
            StartClimbUp();
        }

        public override void Update()
        {
            if (inputLockTimer > 0f)
                inputLockTimer -= Time.deltaTime;

            if (currentPhase == ClimbPhase.ClimbingUp)
            {
                phaseTimer += Time.deltaTime;
                UpdateClimbPosition();
            }
            else if (currentPhase == ClimbPhase.CrouchLoop)
            {
                crouchLoopTimer += Time.deltaTime;
            }
        }

        public override void CheckTransitions()
        {
            if (currentPhase == ClimbPhase.Idle)
            {
                // Không dùng nữa, Enter() tự gọi StartClimbUp()
                return;
            }
            else if (currentPhase == ClimbPhase.ClimbLoopIntro)
            {
                // Chờ CliffClimb_Loop chạy xong (normalizedTime ≥ 0.2) rồi mới leo
                float norm = controller.AnimationController?.GetNormalizedTime() ?? 0f;
                if (norm >= 0.2f)
                    BeginActualClimb();
            }
            else if (currentPhase == ClimbPhase.ClimbingUp)
            {
                float totalDuration = PHASE0_DURATION + PHASE1_DURATION;
                if (phaseTimer >= totalDuration)
                {
                    FinishClimbUp();
                }
            }
            else if (currentPhase == ClimbPhase.CrouchStart)
            {
                float norm = controller.AnimationController?.GetNormalizedTime() ?? 0f;
                if (norm >= 0.95f)
                {
                    currentPhase = ClimbPhase.CrouchLoop;
                    crouchLoopTimer = 0f;
                    string crouchLoop = FormAnimationHelper.GetCrouchLoopAnimation(controller.CurrentFormId);
                    if (string.IsNullOrWhiteSpace(crouchLoop)) crouchLoop = PlayerAnimationController.CROUCH_LOOP;
                    controller.AnimationController?.PlayAnimation(crouchLoop);
                }
            }
            else if (currentPhase == ClimbPhase.CrouchLoop)
            {
                if (crouchLoopTimer >= CROUCH_LOOP_DURATION)
                {
                    currentPhase = ClimbPhase.CrouchEnd;
                    string crouchEnd = FormAnimationHelper.GetCrouchEndAnimation(controller.CurrentFormId);
                    if (string.IsNullOrWhiteSpace(crouchEnd)) crouchEnd = PlayerAnimationController.CROUCH_END;
                    controller.AnimationController?.PlayAnimation(crouchEnd);
                }
            }
            else if (currentPhase == ClimbPhase.CrouchEnd)
            {
                float norm = controller.AnimationController?.GetNormalizedTime() ?? 0f;
                if (norm >= 0.95f)
                {
                    Debug.Log("[CliffClimbState] Crouch sequence done → Idle/Move");
                    if (Mathf.Abs(input.MoveInput.x) > 0.1f)
                        controller.StateMachine.ChangeState(controller.GetFormMoveState(controller.CurrentFormId));
                    else
                        controller.StateMachine.ChangeState(controller.GetIdleStateForCurrentForm());
                }
            }
        }

        public override void Exit()
        {
            movement.UnfreezeVertical();
        }

        // ─────────────────────────────────────────────────────────

        private void StartClimbUp()
        {
            currentPhase = ClimbPhase.ClimbLoopIntro;
            phaseTimer   = 0f;

            enterPos = controller.transform.position;
            float dir = movement.FacingRight ? 1f : -1f;

            targetPos0 = enterPos  + new Vector2(0f,                   PHASE0_UP_OFFSET);
            targetPos1 = targetPos0 + new Vector2(dir * PHASE1_FWD_OFFSET, PHASE1_UP_OFFSET);

            controller.AnimationController?.ForcePlayAnimation(PlayerAnimationController.CLIFF_CLIMB_LOOP);
            Debug.Log("[CliffClimbState] Start → CliffClimb_Loop (intro)");
        }

        private void BeginActualClimb()
        {
            currentPhase = ClimbPhase.ClimbingUp;
            phaseTimer   = 0f;
            controller.AnimationController?.PlayAnimation(PlayerAnimationController.CLIFF_CLIMB_END);
            Debug.Log("[CliffClimbState] Intro done → CliffClimb_End");
        }

        private void UpdateClimbPosition()
        {
            if (phaseTimer < PHASE0_DURATION)
            {
                // Phase 0: kéo thẳng lên
                float t = phaseTimer / PHASE0_DURATION;
                controller.transform.position = Vector2.Lerp(enterPos, targetPos0, t);
            }
            else
            {
                // Phase 1: bước qua mép
                float t = (phaseTimer - PHASE0_DURATION) / PHASE1_DURATION;
                controller.transform.position = Vector2.Lerp(targetPos0, targetPos1, Mathf.Clamp01(t));
            }
        }

        private void FinishClimbUp()
        {
            controller.transform.position = targetPos1;
            movement.UnfreezeVertical();
            ResolveOverlapAfterClimb();

            currentPhase = ClimbPhase.CrouchStart;
            string crouchStart = FormAnimationHelper.GetCrouchStartAnimation(controller.CurrentFormId);
            if (string.IsNullOrWhiteSpace(crouchStart)) crouchStart = PlayerAnimationController.CROUCH_START;
            controller.AnimationController?.PlayAnimation(crouchStart);
            Debug.Log("[CliffClimbState] Position done → Crouch_Start");
        }

        private void ResolveOverlapAfterClimb()
        {
            Collider2D selfCollider = controller.GetComponent<Collider2D>();
            if (selfCollider == null)
                return;

            Bounds bounds = selfCollider.bounds;
            Vector2 baseCenter = bounds.center;
            Vector2 size = bounds.size;

            for (float offset = 0f; offset <= UNSTUCK_MAX_HEIGHT; offset += UNSTUCK_STEP)
            {
                Vector2 checkCenter = baseCenter + Vector2.up * offset;
                Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, size, 0f);
                bool blocked = false;

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D hit = hits[i];
                    if (hit == null || hit.isTrigger)
                        continue;
                    if (hit == selfCollider)
                        continue;
                    if (hit.transform.root == controller.transform.root)
                        continue;

                    blocked = true;
                    break;
                }

                if (!blocked)
                {
                    controller.transform.position += new Vector3(0f, offset, 0f);
                    return;
                }
            }
        }
    }
}
