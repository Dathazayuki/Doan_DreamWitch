using UnityEngine;

namespace Mv
{
    public class Em9030AirSpritShotProjectile : Em9030ProjectileBase
    {
        [Header("Fall Area")]
        [SerializeField] private float randomRangeX = 3f;
        [SerializeField] private float spawnHeight = 6f;

        [Header("Movement")]
        [SerializeField] private float fallSpeed = 9f;
        [SerializeField] private bool rotateAlongFall = true;

        protected override void OnInitialized()
        {
            releaseOnFirstHit = true;

            float x = targetPosition.x + Random.Range(-Mathf.Abs(randomRangeX), Mathf.Abs(randomRangeX));
            transform.position = new Vector3(x, targetPosition.y + Mathf.Max(0.1f, spawnHeight), transform.position.z);

            direction = Vector2.down;

            if (rotateAlongFall)
                transform.rotation = Quaternion.Euler(0f, 0f, -90f);

            if (rb != null)
                rb.linearVelocity = direction * Mathf.Max(0.1f, fallSpeed);
        }

        protected override void Update()
        {
            base.Update();

            if (!active || rb != null)
                return;

            transform.position += (Vector3)(direction * Mathf.Max(0.1f, fallSpeed) * Time.deltaTime);
        }
    }
}
