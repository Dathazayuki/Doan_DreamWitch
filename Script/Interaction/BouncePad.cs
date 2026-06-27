using System.Collections.Generic;
using DreamKnight.Player;
using DreamKnight.Player.States;
using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class BouncePad : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private GameObject topVisual;
        [SerializeField] private GameObject springVisual;
        [SerializeField] private float bounceDuration = 0.5f;
        [SerializeField] private float topMoveAmount = 0.2f;
        [SerializeField] private float springCompressAmount = 0.25f;

        [Header("Launch")]
        [SerializeField] private float launchVelocity = 14f;
        [SerializeField] private bool scaleWithPlayerJumpForce = true;
        [SerializeField] private float jumpForceMultiplier = 1.1f;
        [SerializeField] private float retriggerCooldown = 0.2f;

        [Header("VFX")]
        [SerializeField] private GameObject[] fxPrefabs;
        [SerializeField] private bool useVfxPool = true;

        private readonly Dictionary<int, float> nextTriggerTimeByPlayer = new Dictionary<int, float>();

        private Vector3 topInitialLocalPosition;
        private Vector3 springInitialLocalScale;
        private float bounceTimer;
        private float bounceAngle;

        private const float BounceTime = 0.5f;

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        private void Awake()
        {
            if (topVisual == null)
                topVisual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;

            if (springVisual == null)
                springVisual = transform.childCount > 1 ? transform.GetChild(1).gameObject : null;

            if (topVisual != null)
                topInitialLocalPosition = topVisual.transform.localPosition;

            if (springVisual != null)
                springInitialLocalScale = springVisual.transform.localScale;
        }

        private void Update()
        {
            if (bounceTimer <= 0f)
            {
                RestoreVisualImmediate();
                return;
            }

            bounceTimer -= Time.deltaTime;
            float springValue = CalcSpringVal(ref bounceAngle);
            SetBounce(springValue);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBounce(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBounce(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other))
                return;

            nextTriggerTimeByPlayer.Remove(player.GetInstanceID());
        }

        private void TryBounce(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsBodyCollider(other) || !player.IsAlive)
                return;

            int id = player.GetInstanceID();
            float now = Time.time;
            if (nextTriggerTimeByPlayer.TryGetValue(id, out float nextAllowed) && now < nextAllowed)
                return;

            nextTriggerTimeByPlayer[id] = now + Mathf.Max(0.01f, retriggerCooldown);

            float targetY = Mathf.Max(0f, launchVelocity);
            if (scaleWithPlayerJumpForce && player.Stats != null)
                targetY = Mathf.Max(targetY, player.Stats.JumpForce * Mathf.Max(0f, jumpForceMultiplier));

            if (player.Movement != null)
            {
                Vector2 velocity = player.Movement.Velocity;
                velocity.y = targetY;
                player.Movement.SetVelocity(velocity);
            }

            if (player.StateMachine != null)
            {
                PlayerState jumpState = player.GetFormJumpState(player.CurrentFormId);
                if (jumpState != null && player.StateMachine.CurrentState != jumpState)
                    player.StateMachine.ChangeState(jumpState);
            }

            bounceTimer = Mathf.Max(0.01f, bounceDuration);
            bounceAngle = 0f;
            SpawnFx();
        }

        public static float CalcSpringVal(ref float angle)
        {
            float dt = Time.deltaTime;
            angle += dt * (Mathf.PI * 2f) / BounceTime;
            return Mathf.Sin(angle) * Mathf.Exp(-angle * 0.22f);
        }

        public void SetBounce(float springValue)
        {
            if (topVisual != null)
            {
                Vector3 p = topInitialLocalPosition;
                p.y += springValue * topMoveAmount;
                topVisual.transform.localPosition = p;
            }

            if (springVisual != null)
            {
                Vector3 s = springInitialLocalScale;
                s.y = Mathf.Max(0.05f, springInitialLocalScale.y - springValue * springCompressAmount);
                springVisual.transform.localScale = s;
            }
        }

        private void RestoreVisualImmediate()
        {
            if (topVisual != null)
                topVisual.transform.localPosition = topInitialLocalPosition;

            if (springVisual != null)
                springVisual.transform.localScale = springInitialLocalScale;
        }

        private void SpawnFx()
        {
            if (fxPrefabs == null || fxPrefabs.Length == 0)
                return;

            Vector3 pos = topVisual != null ? topVisual.transform.position : transform.position;

            for (int i = 0; i < fxPrefabs.Length; i++)
            {
                GameObject prefab = fxPrefabs[i];
                if (prefab == null)
                    continue;

                if (useVfxPool)
                {
                    VfxPoolManager.Instance?.Spawn(prefab, pos, Quaternion.identity, null);
                    continue;
                }

                GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
                Destroy(fx, 2f);
            }
        }
    }
}
