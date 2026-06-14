using UnityEngine;

namespace Mv
{
    public class Em9030ScytheProjectile : Em9030ProjectileBase
    {
        [Header("Orbit")]
        [SerializeField] private float orbitRadius = 2.2f;
        [SerializeField] private float orbitSpeedDegrees = 240f;
        [SerializeField] private Vector2 orbitOffset = new Vector2(0f, 1f);
        [SerializeField] private bool rotateVisual = true;

        private float angle;

        protected override void OnInitialized()
        {
            releaseOnFirstHit = false;
            angle = direction.x >= 0f ? 0f : 180f;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            UpdateOrbitPosition();
        }

        protected override void Update()
        {
            base.Update();

            if (!active)
                return;

            angle += orbitSpeedDegrees * Time.deltaTime;
            UpdateOrbitPosition();
        }

        private void UpdateOrbitPosition()
        {
            if (owner == null)
                return;

            Vector2 center = (Vector2)owner.transform.position + orbitOffset;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Mathf.Max(0f, orbitRadius);
            transform.position = center + offset;

            if (rotateVisual)
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void Finish()
        {
            Release();
        }
    }
}
