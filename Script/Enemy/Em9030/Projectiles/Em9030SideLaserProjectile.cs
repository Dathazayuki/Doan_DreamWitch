using UnityEngine;

namespace Mv
{
    public class Em9030SideLaserProjectile : Em9030ProjectileBase
    {
        [Header("Laser")]
        [SerializeField] private GameObject laserVisual;
        [SerializeField] private bool flipByDirection = true;

        private Vector3 startScale;
        private bool hasStartScale;

        protected override void Awake()
        {
            base.Awake();
            startScale = transform.localScale;
            hasStartScale = true;
        }

        protected override void OnInitialized()
        {
            releaseOnFirstHit = false;

            if (!hasStartScale)
            {
                startScale = transform.localScale;
                hasStartScale = true;
            }

            float x = Mathf.Abs(direction.x) > 0.001f ? Mathf.Sign(direction.x) : 1f;
            direction = new Vector2(x, 0f);

            if (flipByDirection)
                transform.localScale = new Vector3(Mathf.Abs(startScale.x) * x, startScale.y, startScale.z);

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (laserVisual != null)
                laserVisual.SetActive(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (laserVisual != null)
                laserVisual.SetActive(false);

            if (hasStartScale)
                transform.localScale = startScale;
        }
    }
}
