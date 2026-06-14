using UnityEngine;

namespace Mv
{
    public class Em9030SpritShotProjectile : Em9030ProjectileBase
    {
        [Header("Movement")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private bool faceDirection = true;

        protected override void OnInitialized()
        {
            releaseOnFirstHit = true;

            float x = Mathf.Abs(direction.x) > 0.001f ? Mathf.Sign(direction.x) : 1f;
            direction = new Vector2(x, 0f);

            if (faceDirection)
                transform.rotation = Quaternion.Euler(0f, 0f, x >= 0f ? 0f : 180f);

            if (rb != null)
                rb.linearVelocity = direction * Mathf.Max(0.1f, speed);
        }

        protected override void Update()
        {
            base.Update();

            if (!active || rb != null)
                return;

            transform.position += (Vector3)(direction * Mathf.Max(0.1f, speed) * Time.deltaTime);
        }
    }
}
