using UnityEngine;

namespace Mv
{
    [DisallowMultipleComponent]
    public class MvEm0110 : MvEnemyBase
    {
        [Header("Em0110 Patrol")]
        [SerializeField] private float patrolSpeed = 2.2f;
        [SerializeField] private bool startMoveRight = true;
        [SerializeField] private LayerMask wallBlockMask = ~0;
        [SerializeField] private float wallCheckDistance = 0.12f;
        [SerializeField] private Transform wallCheckPoint;

        private Rigidbody2D cachedRb;
        private float moveDirection = 1f;

        protected override EnemyState CreateIdleState(EnemyContext context)
        {
            return new Em0110IdleState(context);
        }

        protected override EnemyState CreateRunState(EnemyContext context)
        {
            return new Em0110RunState(context);
        }

        protected override void Awake()
        {
            base.Awake();
            cachedRb = GetComponent<Rigidbody2D>();
            moveDirection = startMoveRight ? 1f : -1f;
            ApplyFacing(moveDirection);
        }

        public void TickWallPatrolMotion()
        {
            if (!IsAlive || cachedRb == null)
                return;

            SetRunAnimation(true, false);

            float direction = moveDirection;
            if (IsWallAhead(direction))
            {
                direction *= -1f;
                moveDirection = direction;
            }

            cachedRb.linearVelocity = new Vector2(direction * Mathf.Max(0f, patrolSpeed), cachedRb.linearVelocity.y);
            ApplyFacing(direction);
        }

        private bool IsWallAhead(float direction)
        {
            Vector2 origin = ResolveWallCheckOrigin(direction);
            Vector2 dir = new Vector2(Mathf.Sign(direction), 0f);
            float distance = Mathf.Max(0.01f, wallCheckDistance);

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, distance, wallBlockMask);
            if (hit.collider == null)
                return false;

            if (hit.collider.transform.root == transform.root)
                return false;

            return true;
        }

        private Vector2 ResolveWallCheckOrigin(float direction)
        {
            if (wallCheckPoint != null)
                return wallCheckPoint.position;

            Collider2D ownerCollider = GetComponent<Collider2D>();
            if (ownerCollider != null)
            {
                Bounds bounds = ownerCollider.bounds;
                float dir = Mathf.Sign(direction);
                float x = bounds.center.x + dir * (bounds.extents.x + 0.01f);
                return new Vector2(x, bounds.center.y);
            }

            return transform.position;
        }

        private void ApplyFacing(float direction)
        {
            if (Mathf.Abs(direction) < 0.001f)
                return;

            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = direction >= 0f ? absX : -absX;
            transform.localScale = scale;
        }
    }
}
