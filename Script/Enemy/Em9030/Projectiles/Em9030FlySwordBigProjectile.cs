using UnityEngine;

namespace Mv
{
    public class Em9030FlySwordBigProjectile : Em9030ProjectileBase
    {
        [Header("Movement")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private bool rotateAlongDirection = true;

        protected override void OnInitialized()
        {
            releaseOnFirstHit = false;

            if (rotateAlongDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

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
