using System;
using System.Collections;
using System.Collections.Generic;
using DreamKnight.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Mv
{
    // ─────────────────────────────────────────────
    //  Data
    // ─────────────────────────────────────────────

    /// <summary>
    /// Một đợt (wave) enemy trong Arena. Các enemy phải được đặt sẵn trong scene và disabled.
    /// </summary>
    [Serializable]
    public class ArenaWave
    {
        [Tooltip("Các enemy thuộc wave này. Phải disable sẵn trong scene — zone sẽ SetActive(true) khi đến lượt.")]
        public GameObject[] enemies;
    }

    /// <summary>Trạng thái của Arena Encounter Zone.</summary>
    public enum ArenaState : byte
    {
        /// <summary>Chưa có Player vào (hoặc zone đã được khởi tạo lại).</summary>
        Idle = 0,
        /// <summary>Player đã vào, đang chiến đấu qua các wave.</summary>
        Active = 1,
        /// <summary>Tất cả wave đã clear — cửa mở, camera unlock.</summary>
        Cleared = 2,
    }

    // ─────────────────────────────────────────────
    //  MonoBehaviour
    // ─────────────────────────────────────────────

    /// <summary>
    /// Arena Encounter Zone — quản lý toàn bộ vòng đời của một encounter:
    /// <list type="bullet">
    ///   <item>Trigger Player vào → bắt đầu wave, đóng cửa, lock camera.</item>
    ///   <item>Wave tuần tự: wave N+1 bắt đầu khi toàn bộ enemy wave N đã chết.</item>
    ///   <item>Clear xong: mở cửa, hiện clear objects, unlock camera.</item>
    /// </list>
    /// </summary>
    [AddComponentMenu("DreamKnight/Enemy/Arena Encounter Zone")]
    public class ArenaEncounterZone : MonoBehaviour
    {
        // ── Waves ──────────────────────────────────────────────────────────
        [Header("Waves")]
        [Tooltip("Danh sách các đợt enemy. Wave 0 xuất hiện đầu tiên, tiếp nối sau khi clear từng wave.")]
        [SerializeField] private ArenaWave[] waves;

        [Tooltip("Delay (giây) trước khi spawn mỗi wave, kể cả wave đầu tiên sau khi Player vào zone.")]
        [SerializeField] private float waveSpawnDelay = 1.5f;

        [Tooltip("VFX prefab hiện thị tại vị trí từng enemy trong thời gian đếm ngược trước khi spawn.\n" +
                 "Prefab được lấy từ VfxPoolManager (pool). Để trống nếu không cần VFX báo hiệu.")]
        [SerializeField] private GameObject spawnIndicatorVfx;

        // ── Doors ─────────────────────────────────────────────────────────
        [Header("Lock Doors")]
        [Tooltip("Bật khi encounter bắt đầu, tắt khi đã clear.")]
        [SerializeField] private GameObject[] lockDoors;

        // ── Clear Objects ─────────────────────────────────────────────────
        [Header("Clear Objects")]
        [Tooltip("Disabled lúc đầu. Bật khi cleared (rương thưởng, checkpoint, hiệu ứng...).")]
        [SerializeField] private GameObject[] clearObjects;

        // ── Camera ────────────────────────────────────────────────────────
        [Header("Camera – Confiner & Zoom")]
        [Tooltip("CinemachineCamera cần lock. Tự tìm nếu để trống.")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        [Tooltip("Collider2D (PolygonCollider2D / BoxCollider2D) định nghĩa bounds của arena — " +
                 "gán vào CinemachineConfiner2D khi lock. IsTrigger = false.")]
        [SerializeField] private Collider2D arenaConfinerBounds;

        [Tooltip("Orthographic size mục tiêu khi camera lock vào arena.")]
        [SerializeField] private float targetOrthoSize = 5.5f;

        [Tooltip("Tốc độ lerp zoom (exp-decay). Giá trị nhỏ hơn = mượt hơn nhưng chậm hơn.")]
        [SerializeField] private float zoomLerpSpeed = 2f;

        // ── Detection ─────────────────────────────────────────────────────
        [Header("Behavior")]
        [Tooltip("Layer mask của Player. Để ~0 nếu muốn detect mọi layer.")]
        [SerializeField] private LayerMask playerLayerMask = ~0;

        // ── Events ────────────────────────────────────────────────────────
        /// <summary>Kích hoạt khi encounter bắt đầu (Player vào zone).</summary>
        public event Action OnEncounterStarted;

        /// <summary>Kích hoạt mỗi khi một wave mới bắt đầu. Tham số là chỉ số wave (0-based).</summary>
        public event Action<int> OnWaveStarted;

        /// <summary>Kích hoạt khi toàn bộ wave đã clear.</summary>
        public event Action OnCleared;

        // ── Runtime State ─────────────────────────────────────────────────
        private ArenaState arenaState = ArenaState.Idle;
        private int currentWaveIndex = -1;
        private bool isWaitingForWave;       // đang đếm ngược delay → không check clear

        // Camera
        private CinemachineConfiner2D confiner;
        private Collider2D savedConfinerBounds;
        private float savedOrthoSize;
        private float lerpTargetOrthoSize;
        private bool isCameraLerpActive;

        // ─────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Trạng thái hiện tại của arena.</summary>
        public ArenaState State => arenaState;

        /// <summary>Chỉ số wave đang diễn ra (0-based). -1 = chưa bắt đầu.</summary>
        public int CurrentWaveIndex => currentWaveIndex;

        /// <summary>True nếu encounter đã hoàn thành.</summary>
        public bool IsCleared => arenaState == ArenaState.Cleared;

        // ─────────────────────────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Ẩn clear objects ngay từ đầu
            SetObjectsActive(clearObjects, false);

            // Đảm bảo doors và enemy đều tắt lúc đầu
            SetObjectsActive(lockDoors, false);
            HideAllEnemies();
        }

        private void Start()
        {
            ResolveCamera();
        }

        private void Update()
        {
            TickCameraLerp();

            // Không check clear khi đang đếm ngược delay spawn wave tiếp theo
            if (arenaState == ArenaState.Active && !isWaitingForWave)
                CheckWaveClearProgress();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Trigger Detection
        // ─────────────────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (arenaState != ArenaState.Idle) return;
            if (!IsPlayerCollider(other)) return;

            BeginEncounter();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Encounter Flow
        // ─────────────────────────────────────────────────────────────────

        private void BeginEncounter()
        {
            arenaState = ArenaState.Active;

            SetObjectsActive(lockDoors, true);
            LockCamera();

            OnEncounterStarted?.Invoke();

            // Wave đầu tiên cũng chờ delay (cho player chuẩn bị)
            StartCoroutine(SpawnWaveWithDelay(0));
        }

        private void StartWave(int waveIndex)
        {
            if (waves == null || waveIndex < 0 || waveIndex >= waves.Length)
            {
                // Không có wave nào → clear ngay
                OnArenaCleared();
                return;
            }

            currentWaveIndex = waveIndex;
            ArenaWave wave = waves[waveIndex];

            if (wave?.enemies != null)
            {
                foreach (GameObject go in wave.enemies)
                {
                    if (go == null) continue;
                    go.SetActive(true);

                    MvEnemyBase enemy = go.GetComponent<MvEnemyBase>();
                    if (enemy != null)
                        enemy.SetCombatMode(EnemyCombatMode.Arena);
                }
            }

            OnWaveStarted?.Invoke(waveIndex);
        }

        private void CheckWaveClearProgress()
        {
            if (!IsCurrentWaveClear()) return;

            int nextWave = currentWaveIndex + 1;
            bool hasMoreWaves = waves != null && nextWave < waves.Length;

            if (hasMoreWaves)
                StartCoroutine(SpawnWaveWithDelay(nextWave));
            else
                OnArenaCleared();
        }

        /// <summary>
        /// Quy trình spawn wave có delay:
        /// <list type="number">
        ///   <item>Spawn VFX báo hiệu tại vị trí từng enemy (từ pool).</item>
        ///   <item>Chờ <see cref="waveSpawnDelay"/> giây.</item>
        ///   <item>Hủy (trả pool) tất cả VFX đã spawn.</item>
        ///   <item>Enable enemy và set Arena CombatMode.</item>
        /// </list>
        /// </summary>
        private IEnumerator SpawnWaveWithDelay(int waveIndex)
        {
            isWaitingForWave = true;

            // Bước 1: Spawn VFX báo hiệu và thu thập instance để sau release
            List<GameObject> spawnedVfx = null;

            if (spawnIndicatorVfx != null && waves != null
                && waveIndex >= 0 && waveIndex < waves.Length)
            {
                ArenaWave previewWave = waves[waveIndex];
                if (previewWave?.enemies != null)
                {
                    VfxPoolManager pool = VfxPoolManager.Instance;
                    spawnedVfx = new List<GameObject>(previewWave.enemies.Length);

                    foreach (GameObject go in previewWave.enemies)
                    {
                        if (go == null) continue;
                        GameObject vfxInstance = pool.Spawn(
                            spawnIndicatorVfx,
                            go.transform.position,
                            Quaternion.identity);

                        if (vfxInstance != null)
                            spawnedVfx.Add(vfxInstance);
                    }
                }
            }

            // Bước 2: Đếm ngược
            float delay = Mathf.Max(0f, waveSpawnDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // Bước 3: Hủy VFX — SetActive(false) kích hoạt PooledVfxAutoRelease.OnDisable
            // → tự DeferRelease về pool ở frame tiếp (không destroy, tái sử dụng được)
            if (spawnedVfx != null)
            {
                foreach (GameObject vfxInstance in spawnedVfx)
                    if (vfxInstance != null)
                        vfxInstance.SetActive(false);

                spawnedVfx.Clear();
            }

            isWaitingForWave = false;

            // Bước 4: Vẫn còn ở Active (không bị interrupted) mới spawn enemy
            if (arenaState == ArenaState.Active)
                StartWave(waveIndex);
        }

        private void OnArenaCleared()
        {
            arenaState = ArenaState.Cleared;

            SetObjectsActive(lockDoors, false);
            SetObjectsActive(clearObjects, true);
            UnlockCamera();

            OnCleared?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Wave Clear Check
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Wave hiện tại được coi là clear khi tất cả enemy không còn alive.
        /// Enemy disabled (death finalized) cũng tính là đã chết.
        /// </summary>
        private bool IsCurrentWaveClear()
        {
            if (waves == null || currentWaveIndex < 0 || currentWaveIndex >= waves.Length)
                return true;

            ArenaWave wave = waves[currentWaveIndex];
            if (wave?.enemies == null || wave.enemies.Length == 0)
                return true;

            foreach (GameObject go in wave.enemies)
            {
                if (go == null) continue;

                // GameObject bị disable → death sequence hoàn tất → tính là đã chết
                if (!go.activeSelf) continue;

                MvEnemyBase enemy = go.GetComponent<MvEnemyBase>();

                // Có MvEnemyBase nhưng vẫn alive → wave chưa clear
                if (enemy != null && enemy.IsAlive) return false;

                // Không có MvEnemyBase nhưng object vẫn active → coi là barrier còn sống
                if (enemy == null) return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Camera
        // ─────────────────────────────────────────────────────────────────

        private void ResolveCamera()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

            if (cinemachineCamera == null) return;

            confiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
            if (confiner != null)
                savedConfinerBounds = confiner.BoundingShape2D;

            savedOrthoSize = cinemachineCamera.Lens.OrthographicSize;
            lerpTargetOrthoSize = savedOrthoSize;
        }

        private void LockCamera()
        {
            if (cinemachineCamera == null) return;

            // Gán confiner bounds của arena
            if (confiner != null && arenaConfinerBounds != null)
                confiner.BoundingShape2D = arenaConfinerBounds;

            // Bắt đầu lerp zoom vào
            lerpTargetOrthoSize = targetOrthoSize;
            isCameraLerpActive = true;
        }

        private void UnlockCamera()
        {
            if (cinemachineCamera == null) return;

            // Khôi phục confiner bounds cũ
            if (confiner != null)
                confiner.BoundingShape2D = savedConfinerBounds;

            // Lerp zoom về lại
            lerpTargetOrthoSize = savedOrthoSize;
            isCameraLerpActive = true;
        }

        private void TickCameraLerp()
        {
            if (!isCameraLerpActive || cinemachineCamera == null) return;

            float current = cinemachineCamera.Lens.OrthographicSize;
            float diff = lerpTargetOrthoSize - current;

            if (Mathf.Abs(diff) <= 0.005f)
            {
                // Snap to target khi đủ gần
                LensSettings lens = cinemachineCamera.Lens;
                lens.OrthographicSize = lerpTargetOrthoSize;
                cinemachineCamera.Lens = lens;
                isCameraLerpActive = false;
                return;
            }

            // Exp-decay lerp: mượt, không overshoot
            float speed = Mathf.Max(0.01f, zoomLerpSpeed);
            float newSize = Mathf.Lerp(current, lerpTargetOrthoSize, 1f - Mathf.Exp(-speed * Time.deltaTime));

            LensSettings updatedLens = cinemachineCamera.Lens;
            updatedLens.OrthographicSize = newSize;
            cinemachineCamera.Lens = updatedLens;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private void HideAllEnemies()
        {
            if (waves == null) return;
            foreach (ArenaWave wave in waves)
            {
                if (wave?.enemies == null) continue;
                foreach (GameObject go in wave.enemies)
                    if (go != null) go.SetActive(false);
            }
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (GameObject go in objects)
                if (go != null) go.SetActive(active);
        }

        private bool IsPlayerCollider(Collider2D col)
        {
            if (col == null) return false;
            if (playerLayerMask != ~0 && (playerLayerMask.value & (1 << col.gameObject.layer)) == 0)
                return false;

            return col.GetComponent<PlayerController>() != null
                || col.GetComponentInParent<PlayerController>() != null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Gizmos
        // ─────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawZoneBounds();
            DrawWaveEnemyGizmos();
        }

        private void DrawZoneBounds()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) return;

            // Màu theo trạng thái
            Color fill, border;
            if (!Application.isPlaying || arenaState == ArenaState.Idle)
            {
                fill = new Color(1f, 0.4f, 0.1f, 0.12f);
                border = new Color(1f, 0.4f, 0.1f, 0.75f);
            }
            else if (arenaState == ArenaState.Active)
            {
                fill = new Color(1f, 0.1f, 0.1f, 0.15f);
                border = new Color(1f, 0.1f, 0.1f, 0.9f);
            }
            else
            {
                fill = new Color(0.2f, 1f, 0.3f, 0.12f);
                border = new Color(0.2f, 1f, 0.3f, 0.75f);
            }

            Gizmos.color = fill;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = border;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        private void DrawWaveEnemyGizmos()
        {
            if (waves == null) return;

            Color[] waveColors =
            {
                new Color(1f, 0.8f, 0f, 0.85f),
                new Color(0f, 0.8f, 1f, 0.85f),
                new Color(0.8f, 0f, 1f, 0.85f),
                new Color(0f, 1f, 0.4f, 0.85f),
            };

            for (int w = 0; w < waves.Length; w++)
            {
                ArenaWave wave = waves[w];
                if (wave?.enemies == null) continue;

                Gizmos.color = waveColors[w % waveColors.Length];

                foreach (GameObject go in wave.enemies)
                {
                    if (go == null) continue;
                    Gizmos.DrawWireSphere(go.transform.position, 0.35f);

                    UnityEditor.Handles.Label(
                        go.transform.position + Vector3.up * 0.5f,
                        $"W{w}");
                }
            }
        }
#endif
    }
}
