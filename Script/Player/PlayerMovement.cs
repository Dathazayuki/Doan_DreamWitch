using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DreamKnight.Systems.Interaction;

namespace DreamKnight.Player
{
    /// <summary>
    /// Xử lý di chuyển, nhảy, dash, WALL CLIMB của Player
    /// WALL CLIMB LOGIC: BẮT BUỘC GIỮ A/D để bám tường, nhả = rơi
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        private Rigidbody2D rb;
        private PlayerInput playerInput;
        private PlayerStats playerStats;
        private PlayerFormManager playerFormManager;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Layer dành riêng cho các One-Way Platform để phân biệt với Ground thường")]
        [SerializeField] private LayerMask platformLayer;
        private bool isGrounded;

        [Header("Wall Detection")]
        [SerializeField] private Transform wallCheck;
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 0.5f);
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float wallContactGraceTime = 0.08f;
        [SerializeField] private float wallRegrabLockDuration = 0.18f;
        private bool isTouchingWall;
        private float wallContactGraceTimer;
        private float wallRegrabLockTimer;

        [Header("Ledge Detection")]
        [Tooltip("Khoảng cách raycast ngang để phát hiện mép")]
        [SerializeField] private float ledgeRayDist = 0.4f;
        [Tooltip("Chiều cao ray thấp (tương đối với gốc object), phải chạm tường")]
        [SerializeField] private float ledgeRayLowY = 0.5f;
        [Tooltip("Chiều cao ray cao (trên đầu mép), phải KHÔNG chạm")]
        [SerializeField] private float ledgeRayHighY = 1.1f;
        private bool isTouchingLedge;

        [Header("Ladder Detection")]
        [SerializeField] private LayerMask ladderLayer;
        private bool isTouchingLadder;
        private float currentLadderX;
        private float currentLadderTopY;
        private float currentLadderBottomY;

        [Header("Crouch Collider")]
        [SerializeField] private float crouchHeightMultiplier = 0.6f;
        [SerializeField] private LayerMask standBlockLayer;

        [Header("Drop Through Platform")]
        [SerializeField] private float dropThroughDuration = 0.5f;
        [SerializeField] private float dropInitialDownwardVelocity = 2f;

        private BoxCollider2D boxCollider2D;
        private CapsuleCollider2D capsuleCollider2D;
        private Vector2 standingSize;
        private Vector2 standingOffset;
        private Vector2 defaultStandingSize;
        private Vector2 defaultStandingOffset;
        private bool isCrouching;
        private PhysicsMaterial2D zeroFrictionMaterial;

        [Header("Jump Settings")]
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;
        [SerializeField] private float coyoteTime = 0.15f;
        private float coyoteTimeCounter;
        [SerializeField] private int maxAirJumps = 1;
        private int airJumpsRemaining;

        [Header("Dash Settings")]
        private bool isDashing;
        private float dashTimeRemaining;
        private float dashCooldownTimer;
        private Vector2 dashDirection;

        [Header("Movement State")]
        private Vector2 moveVelocity;
        private bool facingRight = true;
        private float defaultGravityScale;


        // Properties
        public bool IsGrounded => isGrounded;
        public bool IsTouchingWall => isTouchingWall || wallContactGraceTimer > 0f;
        public bool IsTouchingLedge => isTouchingLedge;
        public bool IsTouchingLadder => isTouchingLadder;
        public float CurrentLadderX => currentLadderX;
        public float CurrentLadderTopY => currentLadderTopY;
        public float CurrentLadderBottomY => currentLadderBottomY;
        public bool IsDashing => isDashing;
        public bool FacingRight => facingRight;
        public bool CanWallGrab => wallRegrabLockTimer <= 0f;
        public Vector2 Velocity => rb.linearVelocity;
        public Rigidbody2D Rb => rb;
        public int AirJumpsRemaining => airJumpsRemaining;
        public int MaxAirJumps => maxAirJumps;
        public bool IsCrouching => isCrouching;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            playerStats = GetComponent<PlayerStats>();
            playerFormManager = GetComponent<PlayerFormManager>();
            defaultGravityScale = rb.gravityScale;

            ResolveActiveBodyCollider();

            defaultStandingSize = standingSize;
            defaultStandingOffset = standingOffset;
        }

        private void Start()
        {
            if (wallCheck == null)
                Debug.LogError("[PlayerMovement] wallCheck chưa được assign trong Inspector!", this);
            if (wallLayer.value == 0)
                Debug.LogWarning("[PlayerMovement] wallLayer = Nothing → player không thể bám tường. Hãy set đúng Layer trong Inspector.", this);
            if (standBlockLayer.value == 0)
                standBlockLayer = groundLayer | wallLayer;

            // Khởi tạo material 0 friction để player không dính tường
            if (zeroFrictionMaterial == null)
            {
                zeroFrictionMaterial = new PhysicsMaterial2D("PlayerZeroFriction");
                zeroFrictionMaterial.friction = 0f;
                zeroFrictionMaterial.bounciness = 0f;
            }

            // Fallback (nếu chưa gọi qua ApplyActiveFormCollider)
            Collider2D col = GetActiveBodyCollider();
            if (col != null)
                col.sharedMaterial = zeroFrictionMaterial;
        }

        private void Update()
        {
            CheckGrounded();
            CheckWall();
            CheckLedge();
            CheckLadder();
            UpdateCoyoteTime();
            UpdateDashCooldown();
            UpdateWallRegrabLock();
            TryAutoRestoreCrouchCollider();
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                HandleDash();
            }
            else
            {
                HandleMovement();
                ApplyBetterJump();
            }
        }

        #region Ground and Wall Detection

        private void CheckGrounded()
        {
            // Bất chấp đang rớt, ta vẫn bật CheckGrounded nhưng LỘT BỎ Platform khỏi tầm soát
            LayerMask maskToCheck = groundLayer;
            if (isDropping && platformLayer.value != 0)
            {
                maskToCheck &= ~platformLayer; // Loại trừ dứt khoát Layer thẻ Platform
            }

            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, maskToCheck);

            if (!wasGrounded && isGrounded)
            {
                airJumpsRemaining = maxAirJumps;
            }
        }

        private bool isDropping;
        public bool IsDropping => isDropping;

        public void DropDownFromPlatform()
        {
            TryDropDownFromPlatform();
        }

        public bool TryDropDownFromPlatform()
        {
            if (isDropping)
                return true;

            List<PlatformEffector2D> effectors = CollectPlatformEffectorsUnderfoot();
            if (effectors.Count == 0)
                return false;

            StartCoroutine(DropDownCoroutine(effectors));
            return true;
        }

        private IEnumerator DropDownCoroutine(List<PlatformEffector2D> effectors)
        {
            isDropping = true;

            if (rb != null)
            {
                float downSpeed = Mathf.Max(0.1f, dropInitialDownwardVelocity);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -downSpeed);
            }

            for (int i = 0; i < effectors.Count; i++)
            {
                PlatformEffector2D effector = effectors[i];
                if (effector != null)
                    effector.rotationalOffset = 180f;
            }

            // Đợi rơi qua an toàn
            yield return new WaitForSeconds(Mathf.Max(0.05f, dropThroughDuration));

            foreach (PlatformEffector2D effector in effectors)
            {
                if (effector != null)
                {
                    effector.rotationalOffset = 0f;
                }
            }

            isDropping = false;
        }

        private List<PlatformEffector2D> CollectPlatformEffectorsUnderfoot()
        {
            List<PlatformEffector2D> effectors = new List<PlatformEffector2D>();
            Collider2D[] colliders = Physics2D.OverlapBoxAll(groundCheck.position, groundCheckSize, 0f, groundLayer);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null)
                    continue;

                PlatformEffector2D effector = col.GetComponent<PlatformEffector2D>();
                if (effector == null)
                    continue;

                if (!effectors.Contains(effector))
                    effectors.Add(effector);
            }

            return effectors;
        }

        private void CheckWall()
        {
            if (wallCheck == null) { isTouchingWall = false; return; }
            isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);
            if (isTouchingWall)
                wallContactGraceTimer = wallContactGraceTime;
            else if (wallContactGraceTimer > 0f)
                wallContactGraceTimer -= Time.deltaTime;
        }

        private void CheckLedge()
        {
            if (isDropping)
            {
                isTouchingLedge = false;
                return;
            }

            // Ray thấp chạm groundLayer (có bề mặt bên cạnh tầm tay)
            // Ray cao KHÔNG chạm (phía trên mép trống)
            // → Không cần wall riêng, chỉ cần object Ground hình chữ nhật
            LayerMask cliffMask = groundLayer | wallLayer;
            float dir = facingRight ? 1f : -1f;

            Vector2 originLow = (Vector2)transform.position + new Vector2(0f, ledgeRayLowY);
            Vector2 originHigh = (Vector2)transform.position + new Vector2(0f, ledgeRayHighY);

            RaycastHit2D hitLow = Physics2D.Raycast(originLow, Vector2.right * dir, ledgeRayDist, cliffMask);
            RaycastHit2D hitHigh = Physics2D.Raycast(originHigh, Vector2.right * dir, ledgeRayDist, cliffMask);

            isTouchingLedge = (hitLow.collider != null) && (hitHigh.collider == null);
        }

        private void CheckLadder()
        {
            Collider2D myCol = GetActiveBodyCollider();
            Vector2 colCenter = myCol != null ? (Vector2)myCol.bounds.center : (Vector2)transform.position;
            Vector2 colSize = myCol != null ? (Vector2)myCol.bounds.size : new Vector2(0.5f, 1f);

            Collider2D ladderCol = Physics2D.OverlapBox(colCenter, colSize, 0f, ladderLayer);
            if (ladderCol != null)
            {
                isTouchingLadder = true;
                currentLadderX = ladderCol.bounds.center.x;
                currentLadderTopY = ladderCol.bounds.max.y;
                currentLadderBottomY = ladderCol.bounds.min.y;
            }
            else
            {
                isTouchingLadder = false;
            }
        }

        private void UpdateCoyoteTime()
        {
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            float moveInput = playerInput.MoveInput.x;
            float targetSpeed = moveInput * playerStats.MoveSpeed;

            moveVelocity.x = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 0.2f);
            moveVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = moveVelocity;

            if (moveInput > 0 && !facingRight)
            {
                Flip();
            }
            else if (moveInput < 0 && facingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            facingRight = !facingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            // IMPORTANT: CHỈ flip X axis, giữ nguyên Y và Z để tránh deformation
            // Đảm bảo Y=1, Z=1 (hoặc giá trị mặc định)
            scale.y = Mathf.Abs(scale.y);
            scale.z = Mathf.Abs(scale.z);
            transform.localScale = scale;
        }

        #endregion

        #region Jump

        public void Jump()
        {
            bool canJump = coyoteTimeCounter > 0f || airJumpsRemaining > 0;
            if (!canJump) return;

            if (coyoteTimeCounter <= 0f)
                airJumpsRemaining--;

            coyoteTimeCounter = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, playerStats.JumpForce);
        }

        public void WallClimbJump()
        {
            coyoteTimeCounter = 0f;
            wallRegrabLockTimer = wallRegrabLockDuration;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, playerStats.JumpForce);
        }

        private void ApplyBetterJump()
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0 && !playerInput.JumpHeld)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }

        #endregion

        #region Dash

        public bool CanDash()
        {
            return !isDashing && 
                   dashCooldownTimer <= 0f && 
                   playerStats.HasEnoughStamina(playerStats.DashStaminaCost);
        }

        public void StartDash()
        {
            if (!CanDash()) return;

            isDashing = true;
            dashTimeRemaining = playerStats.DashDuration;

            Vector2 input = playerInput.MoveInput;
            float horizontalInput = input.x;
            if (Mathf.Abs(horizontalInput) < 0.1f)
            {
                dashDirection = facingRight ? Vector2.right : Vector2.left;
            }
            else
            {
                dashDirection = horizontalInput > 0f ? Vector2.right : Vector2.left;
            }

            playerStats.UseStamina(playerStats.DashStaminaCost);
        }

        private void HandleDash()
        {
            dashTimeRemaining -= Time.fixedDeltaTime;

            if (dashTimeRemaining <= 0f)
            {
                isDashing = false;
                dashCooldownTimer = playerStats.DashCooldown;
                rb.linearVelocity = Vector2.zero;
            }
            else
            {
                rb.linearVelocity = dashDirection * playerStats.DashSpeed;
            }
        }

        private void UpdateDashCooldown()
        {
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }
        }

        private void UpdateWallRegrabLock()
        {
            if (wallRegrabLockTimer > 0f)
                wallRegrabLockTimer -= Time.deltaTime;
        }

        #endregion

        #region Wall Jump - LOGIC ĐƠN GIẢN

        /// <summary>
        /// Kiểm tra player có đang giữ phím VÀO TƯỜNG không
        /// Logic: Giữ A/D vào tường → Reset air jumps → Có thể jump liên tục
        /// </summary>
        public bool IsHoldingIntoWall()
        {
            if (!CanWallGrab) return false;
            if (!IsTouchingWall) return false;

            float horizontalInput = playerInput.MoveInput.x;

            // Nếu facing right → cần giữ D (input > 0)
            // Nếu facing left → cần giữ A (input < 0)
            if (facingRight)
            {
                return horizontalInput > 0.1f; // Giữ D
            }
            else
            {
                return horizontalInput < -0.1f; // Giữ A
            }
        }

        #endregion

        #region Utility

        public void SetVelocity(Vector2 velocity)
        {
            rb.linearVelocity = velocity;
        }

        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
        {
            rb.AddForce(force, mode);
        }

        public void StopMovement()
        {
            rb.linearVelocity = Vector2.zero;
        }

        public void FreezeVertical()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.gravityScale = 0f;
        }

        public void UnfreezeVertical()
        {
            rb.gravityScale = defaultGravityScale;
        }

        public void EnterCrouch()
        {
            if (isCrouching) return;

            ResolveActiveBodyCollider();

            if (boxCollider2D == null && capsuleCollider2D == null)
            {
                Debug.LogWarning("[PlayerMovement] Crouch requires active BodyCollider to be BoxCollider2D or CapsuleCollider2D.", this);
                return;
            }

            Vector2 crouchSize = GetCrouchSize();
            Vector2 crouchOffset = GetOffsetKeepingFeet(standingSize, crouchSize, standingOffset);

            if (boxCollider2D != null)
            {
                boxCollider2D.size = crouchSize;
                boxCollider2D.offset = crouchOffset;
                isCrouching = true;
                return;
            }

            if (capsuleCollider2D != null)
            {
                capsuleCollider2D.size = crouchSize;
                capsuleCollider2D.offset = crouchOffset;
                isCrouching = true;
            }
        }

        public bool TryExitCrouch()
        {
            if (!isCrouching) return true;
            if (!CanStandUp()) return false;

            if (boxCollider2D != null)
            {
                boxCollider2D.size = standingSize;
                boxCollider2D.offset = standingOffset;
            }
            else if (capsuleCollider2D != null)
            {
                capsuleCollider2D.size = standingSize;
                capsuleCollider2D.offset = standingOffset;
            }

            isCrouching = false;
            return true;
        }

        private void TryAutoRestoreCrouchCollider()
        {
            if (!isCrouching)
                return;

            if (playerInput != null && playerInput.MoveInput.y < -0.1f)
                return;

            TryExitCrouch();
        }

        public bool ResolveBodyOverlap(float maxDistance = 0.7f, float step = 0.02f)
        {
            ResolveActiveBodyCollider();

            Collider2D activeCollider = GetActiveBodyCollider();
            if (activeCollider == null)
                return false;

            if (!IsBodyOverlapBlocked(activeCollider, Vector2.zero))
                return true;

            float safeStep = Mathf.Max(0.005f, step);
            float safeMaxDistance = Mathf.Max(safeStep, maxDistance);
            Vector2[] directions =
            {
                Vector2.up,
                Vector2.left,
                Vector2.right,
                (Vector2.up + Vector2.left).normalized,
                (Vector2.up + Vector2.right).normalized
            };

            for (float distance = safeStep; distance <= safeMaxDistance; distance += safeStep)
            {
                for (int i = 0; i < directions.Length; i++)
                {
                    Vector2 offset = directions[i] * distance;
                    if (IsBodyOverlapBlocked(activeCollider, offset))
                        continue;

                    transform.position += (Vector3)offset;
                    return true;
                }
            }

            return false;
        }

        private bool IsBodyOverlapBlocked(Collider2D activeCollider, Vector2 offset)
        {
            Bounds bounds = activeCollider.bounds;
            Vector2 checkCenter = (Vector2)bounds.center + offset;
            Vector2 checkSize = bounds.size;
            return IsAreaBlockedByStandBlockers(checkCenter, checkSize, activeCollider);
        }

        private bool IsAreaBlockedByStandBlockers(Vector2 checkCenter, Vector2 checkSize, Collider2D activeCollider)
        {
            int layerMask = standBlockLayer.value != 0
                ? standBlockLayer.value
                : groundLayer.value | wallLayer.value;

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, layerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.isTrigger)
                    continue;
                if (hit == activeCollider)
                    continue;
                if (hit.transform.root == transform.root)
                    continue;

                return true;
            }

            return false;
        }

        public bool CanStandUp()
        {
            if (!isCrouching) return true;

            Collider2D activeCollider = GetActiveBodyCollider();
            if (activeCollider == null)
                return true;

            Bounds currentBounds = activeCollider.bounds;
            float worldStandingHeight = standingSize.y * AbsLossyScale2D().y;
            float extraHeadroomHeight = Mathf.Max(0f, worldStandingHeight - currentBounds.size.y);

            if (extraHeadroomHeight <= 0.001f)
                return true;

            Vector2 checkSize = new Vector2(
                Mathf.Max(currentBounds.size.x * 0.9f, 0.05f),
                Mathf.Max(extraHeadroomHeight - 0.01f, 0.01f));

            Vector2 checkCenter = new Vector2(
                currentBounds.center.x,
                currentBounds.max.y + checkSize.y * 0.5f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, standBlockLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.isTrigger)
                    continue;
                if (hit == activeCollider)
                    continue;
                if (hit.transform.root == transform.root)
                    continue;
                return false;
            }

            return true;
        }

        public bool ShouldAutoCrouchForHeadBlock()
        {
            if (isCrouching || !isGrounded)
                return false;

            ResolveActiveBodyCollider();

            Collider2D activeCollider = GetActiveBodyCollider();
            if (activeCollider == null)
                return false;

            Vector2 crouchSize = GetCrouchSize();
            Bounds bounds = activeCollider.bounds;
            float crouchWorldHeight = crouchSize.y * AbsLossyScale2D().y;
            float headBlockHeight = Mathf.Max(0f, bounds.size.y - crouchWorldHeight);
            if (headBlockHeight <= 0.01f)
                return false;

            Vector2 checkSize = new Vector2(
                Mathf.Max(bounds.size.x * 0.9f, 0.05f),
                Mathf.Max(headBlockHeight - 0.01f, 0.01f));

            Vector2 checkCenter = new Vector2(
                bounds.center.x,
                bounds.max.y - checkSize.y * 0.5f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, standBlockLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit.isTrigger)
                    continue;
                if (hit == activeCollider)
                    continue;
                if (hit.transform.root == transform.root)
                    continue;

                return CanCrouchFitAtCurrentPosition(activeCollider, crouchSize);
            }

            return false;
        }

        private bool CanCrouchFitAtCurrentPosition(Collider2D activeCollider, Vector2 crouchSize)
        {
            if (activeCollider == null)
                return false;

            Bounds bounds = activeCollider.bounds;
            Vector3 scale = activeCollider.transform.lossyScale;
            float skin = 0.02f;
            float crouchWorldWidth = Mathf.Max(0.05f, standingSize.x * Mathf.Abs(scale.x) - skin);
            float crouchWorldHeight = Mathf.Max(0.05f, crouchSize.y * Mathf.Abs(scale.y) - skin);

            Vector2 checkSize = new Vector2(crouchWorldWidth, crouchWorldHeight);
            Vector2 checkCenter = new Vector2(
                bounds.center.x,
                bounds.min.y + skin + checkSize.y * 0.5f);

            return !IsAreaBlockedByStandBlockers(checkCenter, checkSize, activeCollider);
        }

        private Vector2 GetCrouchSize()
        {
            float crouchHeight = Mathf.Max(standingSize.y * crouchHeightMultiplier, 0.1f);
            return new Vector2(standingSize.x, crouchHeight);
        }

        private Vector2 GetOffsetKeepingFeet(Vector2 originalSize, Vector2 newSize, Vector2 originalOffset)
        {
            float deltaHeight = originalSize.y - newSize.y;
            return new Vector2(originalOffset.x, originalOffset.y - deltaHeight * 0.5f);
        }

        private Vector2 AbsLossyScale2D()
        {
            Vector3 scale = transform.lossyScale;
            return new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        /// <summary>
        /// Đổi body collider đang active sang collider của form mới.
        /// Gọi bởi PlayerController sau khi PlayerFormManager instantiate form prefab.
        /// </summary>
        public void ApplyActiveFormCollider(PlayerFormBodyRef formRef)
        {
            if (formRef == null) return;
            
            Collider2D formBodyCollider = formRef.BodyCollider;
            if (formBodyCollider == null) return;

            LinkActiveBodyCollider(formBodyCollider);

            // Gán zero friction material để tránh dính tường
            if (zeroFrictionMaterial == null)
            {
                zeroFrictionMaterial = new PhysicsMaterial2D("PlayerZeroFriction");
                zeroFrictionMaterial.friction = 0f;
                zeroFrictionMaterial.bounciness = 0f;
            }
            formBodyCollider.sharedMaterial = zeroFrictionMaterial;

            // Đổi GroundCheck / WallCheck nếu form có set (nếu không set thì giữ nguyên cái ở gốc)
            if (formRef.GroundCheck != null) groundCheck = formRef.GroundCheck;
            if (formRef.WallCheck != null) wallCheck = formRef.WallCheck;
        }

        private Collider2D GetActiveBodyCollider()
        {
            if (boxCollider2D != null)
                return boxCollider2D;

            if (capsuleCollider2D != null)
                return capsuleCollider2D;

            return playerFormManager != null && playerFormManager.ActiveBodyCollider != null
                ? playerFormManager.ActiveBodyCollider
                : GetComponent<Collider2D>();
        }

        private void ResolveActiveBodyCollider()
        {
            Collider2D activeCollider = playerFormManager != null && playerFormManager.ActiveBodyCollider != null
                ? playerFormManager.ActiveBodyCollider
                : GetComponent<Collider2D>();

            LinkActiveBodyCollider(activeCollider);
        }

        private void LinkActiveBodyCollider(Collider2D activeCollider)
        {
            bool sameColliderWhileCrouching = isCrouching
                && activeCollider != null
                && (activeCollider == boxCollider2D || activeCollider == capsuleCollider2D);

            boxCollider2D = activeCollider as BoxCollider2D;
            capsuleCollider2D = activeCollider as CapsuleCollider2D;

            if (sameColliderWhileCrouching)
                return;

            if (boxCollider2D != null)
            {
                standingSize = boxCollider2D.size;
                standingOffset = boxCollider2D.offset;
                return;
            }

            if (capsuleCollider2D != null)
            {
                standingSize = capsuleCollider2D.size;
                standingOffset = capsuleCollider2D.offset;
            }
        }



        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            }

            if (wallCheck != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
            }
        }

        #endregion
    }
}
