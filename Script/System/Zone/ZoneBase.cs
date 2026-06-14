using System;
using System.Collections.Generic;
using DreamKnight.Player;
using Mv;
using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    [Flags]
    public enum ZoneHitTarget : uint
    {
        None = 0u,
        Player = 1u,
        Enemy = 2u
    }

    [DisallowMultipleComponent]
    public abstract class ZoneBase : MonoBehaviour
    {
        [Header("Zone")]
        [SerializeField] protected BoxCollider2D[] boxColliders;
        [SerializeField] private bool autoMoveRefreshBounds = true;

        protected Bounds[] zoneBounds;

        private bool isZoneEnterPlayer;
        private bool onceZoneEnterPlayer;

        private readonly HashSet<int> playersInside = new HashSet<int>();
        private readonly HashSet<int> enemiesInside = new HashSet<int>();

        public abstract ZoneType Type { get; }
        protected virtual ZoneHitTarget HitTarget => ZoneHitTarget.Player;

        public BoxCollider2D[] BoxColliders => boxColliders;
        protected bool OnceZoneEnterPlayer => onceZoneEnterPlayer;

        protected virtual void Awake()
        {
            InitializeZone();
        }

        protected virtual void Update()
        {
            if (autoMoveRefreshBounds)
                RefreshBounds();

            TickZone();
        }

        public virtual void InitializeZone()
        {
            if (boxColliders == null || boxColliders.Length == 0)
                boxColliders = GetComponentsInChildren<BoxCollider2D>(true);

            for (int i = 0; i < boxColliders.Length; i++)
            {
                if (boxColliders[i] != null)
                    boxColliders[i].isTrigger = true;
            }

            RefreshBounds();
        }

        public void RefreshBounds()
        {
            if (boxColliders == null)
            {
                zoneBounds = Array.Empty<Bounds>();
                return;
            }

            zoneBounds = new Bounds[boxColliders.Length];
            for (int i = 0; i < boxColliders.Length; i++)
            {
                zoneBounds[i] = boxColliders[i] != null ? boxColliders[i].bounds : default;
            }
        }

        protected virtual void TickZone() { }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if ((HitTarget & ZoneHitTarget.Player) != 0)
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                // Chỉ nhận collider body chính của Player (từ form đang active),
                // bỏ qua các child trigger collider (hitbox attack, ...) để tránh kích hoạt nhầm.
                if (player != null && IsPlayerBodyCollider(player, other))
                {
                    int id = player.GetInstanceID();
                    if (playersInside.Add(id))
                    {
                        isZoneEnterPlayer = true;
                        onceZoneEnterPlayer = true;
                        OnPlayerEnterZone(player);
                    }
                }
            }

            if ((HitTarget & ZoneHitTarget.Enemy) != 0)
            {
                MvEnemyBase enemy = other.GetComponentInParent<MvEnemyBase>();
                if (enemy != null && other.gameObject == enemy.gameObject)
                {
                    int id = enemy.GetInstanceID();
                    if (enemiesInside.Add(id))
                        OnEnemyEnterZone(enemy);
                }
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if ((HitTarget & ZoneHitTarget.Player) != 0)
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                if (player != null && IsPlayerBodyCollider(player, other))
                    OnPlayerStayZone(player);
            }

            if ((HitTarget & ZoneHitTarget.Enemy) != 0)
            {
                MvEnemyBase enemy = other.GetComponentInParent<MvEnemyBase>();
                if (enemy != null && other.gameObject == enemy.gameObject && !IsIgnoreEnemyStayZone(enemy))
                    OnEnemyStayZone(enemy);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if ((HitTarget & ZoneHitTarget.Player) != 0)
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                if (player != null && IsPlayerBodyCollider(player, other))
                {
                    playersInside.Remove(player.GetInstanceID());
                    isZoneEnterPlayer = playersInside.Count > 0;
                    OnPlayerExitZone(player);
                }
            }

            if ((HitTarget & ZoneHitTarget.Enemy) != 0)
            {
                MvEnemyBase enemy = other.GetComponentInParent<MvEnemyBase>();
                if (enemy != null && other.gameObject == enemy.gameObject)
                {
                    enemiesInside.Remove(enemy.GetInstanceID());
                    OnEnemyExitZone(enemy);
                }
            }
        }

        protected virtual void OnPlayerEnterZone(PlayerController player) { }
        protected virtual void OnPlayerStayZone(PlayerController player) { }
        protected virtual void OnPlayerExitZone(PlayerController player) { }

        protected virtual void OnEnemyEnterZone(MvEnemyBase enemy) { }
        protected virtual void OnEnemyStayZone(MvEnemyBase enemy) { }
        protected virtual void OnEnemyExitZone(MvEnemyBase enemy) { }

        protected virtual bool IsIgnoreEnemyStayZone(MvEnemyBase enemy)
        {
            return enemy == null || !enemy.IsAlive;
        }

        protected bool IsZoneEnterPlayer => isZoneEnterPlayer;

        // ── Helper ───────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra xem collider đã chạm vào zone có phải là body collider chính của Player không.
        /// Hỗ trợ cả cấu trúc cũ (collider trên root) và cấu trúc mới (collider trong form child prefab).
        /// </summary>
        private static bool IsPlayerBodyCollider(PlayerController player, Collider2D other)
        {
            // Cách cũ: collider gắn trực tiếp trên root Player
            if (other.gameObject == player.gameObject) return true;

            // Cách mới: collider thuộc form prefab child → query PlayerFormManager
            var formManager = player.GetComponent<PlayerFormManager>();
            if (formManager != null && formManager.ActiveBodyColliderObject == other.gameObject)
                return true;

            return false;
        }
    }
}
