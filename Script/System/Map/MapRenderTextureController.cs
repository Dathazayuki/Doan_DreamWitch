using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DreamKnight.Systems.Zone;
using System.Collections;
using System.Collections.Generic;

namespace DreamKnight.Systems.Map
{
    [DisallowMultipleComponent]
    public class MapRenderTextureController : MonoBehaviour
    {
        private sealed class RuntimeMarkerBinding
        {
            public Transform Source;
            public Transform Icon;
            public Vector3 Offset;
        }

        private static MapRenderTextureController instance;
        public static MapRenderTextureController Instance => instance;

        private static readonly HashSet<string> unlockedAreas = new HashSet<string>();
        private readonly List<MapSubArea> sceneSubAreas = new List<MapSubArea>();

        public static void ClearUnlockedAreas()
        {
            unlockedAreas.Clear();
        }

        [Header("References")]
        [SerializeField] private Camera mapCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private RenderTexture miniMapRenderTexture;
        [SerializeField] private RenderTexture fullMapRenderTexture;
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private RawImage fullMapImage;

        [Header("Map Render Layers")]
        [SerializeField] private string tileMapOnlyLayerName = "TileMapOnly";
        [SerializeField] private string markerOnlyLayerName = "MarkerOnly";

        [Header("Runtime Marker Spawn")]
        [SerializeField] private bool spawnRuntimeMarkers = true;
        [SerializeField] private LayerMask markerSourceLayers;
        [SerializeField] private bool includeInactiveMarkerSources;
        [SerializeField] private bool autoRebuildMarkersOnSourceChange = true;
        [SerializeField] private int rebuildMarkerDelayFramesAfterSourceChange = 1;
        [SerializeField] private float emptyBindingsRetryInterval = 0.5f;
        [SerializeField] private bool logMarkerSpawn;

        [Header("Camera Visibility")]
        [SerializeField] private bool forceHideMapLayersFromOtherCameras = true;

        [Header("Map Visual")]
        [SerializeField] private CameraClearFlags mapClearFlags = CameraClearFlags.SolidColor;
        [SerializeField] private Color mapBackgroundColor = new Color(0.08f, 0.12f, 0.2f, 1f);

        [Header("Mini Map Follow")]
        [SerializeField] private bool followInMiniMap = true;
        [SerializeField] private float followSmooth = 10f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);
        [SerializeField] private string autoFindFollowTargetTag = "Player";

        [Header("Ortho Size")]
        [SerializeField] private float miniOrthoSize = 12f;
        [SerializeField] private float fullOrthoSize = 35f;

        [Header("Full Map Pan")]
        [SerializeField] private float fullMapPanSpeed = 20f;
        [SerializeField] private float fullMapZoomMin = 8f;
        [SerializeField] private float fullMapZoomMax = 120f;
        [SerializeField] private bool keepMiniVisibleWhenFull = true;

        [Header("Clamp Bounds (Optional)")]
        [SerializeField] private bool clampToBounds;
        [SerializeField] private Vector2 minBounds = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 maxBounds = new Vector2(100f, 100f);

        [Header("Persistence")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool enforceSingleton = true;

        [Header("Scene Load Handling")]
        [SerializeField] private bool autoHandleSceneLoaded = true;
        [SerializeField] private int rebuildMarkerDelayFramesAfterSceneLoaded = 1;

        [Header("Scene Map Profile")]
        [SerializeField] private bool useSceneMapProfile = true;
        [SerializeField] private bool renderImmediatelyAfterSceneLoaded = true;

        private bool isFullMapOpen;
        private Vector3 miniCameraPos;
        private Vector2 fullMapPanInput;
        private Vector2 fullMapOffset;
        private float fullMapZoomOffset;
        private GameObject runtimeMarkerRoot;
        private readonly List<RuntimeMarkerBinding> runtimeMarkerBindings = new List<RuntimeMarkerBinding>(32);
        private bool runtimeMarkerRebuildQueued;
        private float nextEmptyBindingsRetryTime;

        private void Reset()
        {
            mapCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (enforceSingleton)
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                instance = this;
            }

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (mapCamera == null)
                mapCamera = GetComponent<Camera>();

            if (mapCamera == null)
            {
                Debug.LogError("[MapRenderTextureController] Missing Camera.");
                enabled = false;
                return;
            }

            SetupCameraForMap();
            RebuildRuntimeMarkers();
            ExcludeMapLayersFromNonMapCameras();

            BindRenderTextureToUi();

            ResolveFollowTargetIfMissing();
            InitializeSceneSubAreas();

            miniCameraPos = followTarget != null
                ? followTarget.position + cameraOffset
                : mapCamera.transform.position;

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            CleanupRuntimeMarkers();

            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (mapCamera == null)
                return;

            ResolveFollowTargetIfMissing();

            UpdateMiniCameraFollow();

            if (isFullMapOpen)
                UpdateFullMapPan();

            UpdateRuntimeMarkers();

            RenderMiniMap();
            if (isFullMapOpen)
                RenderFullMap();
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetMapOpen(bool open)
        {
            isFullMapOpen = open;

            if (miniMapImage != null)
                miniMapImage.gameObject.SetActive(keepMiniVisibleWhenFull || !open);

            if (fullMapImage != null)
                fullMapImage.gameObject.SetActive(open);
        }

        public void ToggleMapOpen()
        {
            SetMapOpen(!isFullMapOpen);
        }

        public bool IsFullMapOpen => isFullMapOpen;

        public void SetRenderTextures(RenderTexture miniTexture, RenderTexture fullTexture)
        {
            miniMapRenderTexture = miniTexture;
            fullMapRenderTexture = fullTexture;
            BindRenderTextureToUi();
        }

        public void ForceRebuildRuntimeMarkers()
        {
            RebuildRuntimeMarkers();
        }

        public void SetFullMapMoveInput(Vector2 moveInput)
        {
            fullMapPanInput = moveInput;
        }

        public void AddZoom(float delta)
        {
            if (!isFullMapOpen)
                return;

            fullMapZoomOffset = Mathf.Clamp(fullMapZoomOffset + delta, fullMapZoomMin - fullOrthoSize, fullMapZoomMax - fullOrthoSize);
        }

        private void SetupCameraForMap()
        {
            mapCamera.orthographic = true;
            mapCamera.clearFlags = mapClearFlags;
            mapCamera.backgroundColor = mapBackgroundColor;
            mapCamera.orthographicSize = miniOrthoSize;
            mapCamera.depth = 50f;

            // Quan trọng: map camera chỉ render vào RenderTexture bằng Camera.Render(),
            // không tham gia pipeline render màn hình chính để tránh đè MainCamera.
            mapCamera.targetTexture = null;
            mapCamera.enabled = false;

            ApplyMapCameraCullingMask();
        }

        public void ResetFullMapState(bool closeMap)
        {
            fullMapOffset = Vector2.zero;
            fullMapPanInput = Vector2.zero;
            fullMapZoomOffset = 0f;

            if (closeMap)
                SetMapOpen(false);
        }

        private void BindRenderTextureToUi()
        {
            if (miniMapImage != null)
                miniMapImage.texture = miniMapRenderTexture;

            if (fullMapImage != null)
                fullMapImage.texture = fullMapRenderTexture;
        }

        private void UpdateMiniCameraFollow()
        {
            if (!followInMiniMap || followTarget == null)
                return;

            Vector3 targetPos = followTarget.position + cameraOffset;
            targetPos.z = cameraOffset.z;
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSmooth) * Time.deltaTime);
            miniCameraPos = Vector3.Lerp(miniCameraPos, targetPos, t);
            miniCameraPos = ClampPositionForOrtho(miniCameraPos, miniOrthoSize);
        }

        private void UpdateFullMapPan()
        {
            Vector2 delta = fullMapPanInput * (fullMapPanSpeed * Time.unscaledDeltaTime);
            fullMapOffset += delta;
        }

        private void RenderMiniMap()
        {
            if (miniMapRenderTexture == null)
                return;

            Vector3 prevPos = mapCamera.transform.position;
            float prevSize = mapCamera.orthographicSize;
            RenderTexture prevTarget = mapCamera.targetTexture;

            mapCamera.transform.position = miniCameraPos;
            mapCamera.orthographicSize = miniOrthoSize;
            mapCamera.targetTexture = miniMapRenderTexture;
            mapCamera.Render();

            mapCamera.targetTexture = prevTarget;
            mapCamera.orthographicSize = prevSize;
            mapCamera.transform.position = prevPos;
        }

        private void RenderFullMap()
        {
            if (fullMapRenderTexture == null)
                return;

            Vector3 prevPos = mapCamera.transform.position;
            float prevSize = mapCamera.orthographicSize;
            RenderTexture prevTarget = mapCamera.targetTexture;

            float fullSize = Mathf.Clamp(fullOrthoSize + fullMapZoomOffset, fullMapZoomMin, fullMapZoomMax);
            Vector3 basePos = followTarget != null ? followTarget.position + cameraOffset : miniCameraPos;
            basePos += (Vector3)fullMapOffset;
            basePos.z = cameraOffset.z;
            basePos = ClampPositionForOrtho(basePos, fullSize);

            mapCamera.transform.position = basePos;
            mapCamera.orthographicSize = fullSize;
            mapCamera.targetTexture = fullMapRenderTexture;
            mapCamera.Render();

            mapCamera.targetTexture = prevTarget;
            mapCamera.orthographicSize = prevSize;
            mapCamera.transform.position = prevPos;
        }

        public bool TryProjectWorldToFullMapLocalPoint(Vector3 worldPosition, RectTransform mapRect, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            if (!isFullMapOpen || mapCamera == null || mapRect == null)
                return false;

            Rect rect = mapRect.rect;
            if (rect.width <= 0.0001f || rect.height <= 0.0001f)
                return false;

            float fullSize = Mathf.Clamp(fullOrthoSize + fullMapZoomOffset, fullMapZoomMin, fullMapZoomMax);
            Vector3 basePos = followTarget != null ? followTarget.position + cameraOffset : miniCameraPos;
            basePos += (Vector3)fullMapOffset;
            basePos.z = cameraOffset.z;
            basePos = ClampPositionForOrtho(basePos, fullSize);

            Vector3 delta = worldPosition - basePos;
            float halfHeight = fullSize;
            float textureAspect = GetFullMapProjectionAspect();
            float halfWidth = halfHeight * textureAspect;

            if (halfWidth <= 0.0001f || halfHeight <= 0.0001f)
                return false;

            float offsetX = Vector3.Dot(delta, mapCamera.transform.right);
            float offsetY = Vector3.Dot(delta, mapCamera.transform.up);
            float textureNormalizedX = offsetX / (halfWidth * 2f) + 0.5f;
            float textureNormalizedY = offsetY / (halfHeight * 2f) + 0.5f;

            Rect uvRect = fullMapImage != null ? fullMapImage.uvRect : new Rect(0f, 0f, 1f, 1f);
            float uvWidth = Mathf.Abs(uvRect.width);
            float uvHeight = Mathf.Abs(uvRect.height);
            if (uvWidth <= 0.0001f || uvHeight <= 0.0001f)
                return false;

            float normalizedX = (textureNormalizedX - uvRect.x) / uvRect.width;
            float normalizedY = (textureNormalizedY - uvRect.y) / uvRect.height;

            if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
                return false;

            Rect visibleRect = GetVisibleTextureRectInMapRect(rect, textureAspect);

            localPoint = new Vector2(
                Mathf.Lerp(visibleRect.xMin, visibleRect.xMax, normalizedX),
                Mathf.Lerp(visibleRect.yMin, visibleRect.yMax, normalizedY));
            return true;
        }

        private float GetFullMapProjectionAspect()
        {
            if (fullMapRenderTexture != null && fullMapRenderTexture.height > 0)
                return Mathf.Max(0.01f, fullMapRenderTexture.width / (float)fullMapRenderTexture.height);

            if (fullMapImage != null && fullMapImage.texture != null && fullMapImage.texture.height > 0)
                return Mathf.Max(0.01f, fullMapImage.texture.width / (float)fullMapImage.texture.height);

            if (mapCamera != null)
                return Mathf.Max(0.01f, mapCamera.aspect);

            return 1f;
        }

        private static Rect GetVisibleTextureRectInMapRect(Rect mapRect, float textureAspect)
        {
            if (mapRect.width <= 0.0001f || mapRect.height <= 0.0001f)
                return mapRect;

            float rectAspect = mapRect.width / mapRect.height;
            if (rectAspect > textureAspect)
            {
                float visibleWidth = mapRect.height * textureAspect;
                float x = mapRect.xMin + (mapRect.width - visibleWidth) * 0.5f;
                return new Rect(x, mapRect.yMin, visibleWidth, mapRect.height);
            }

            float visibleHeight = mapRect.width / textureAspect;
            float y = mapRect.yMin + (mapRect.height - visibleHeight) * 0.5f;
            return new Rect(mapRect.xMin, y, mapRect.width, visibleHeight);
        }

        private Vector3 ClampPositionForOrtho(Vector3 position, float orthoSize)
        {
            if (!clampToBounds)
                return position;

            float aspect = mapCamera != null ? mapCamera.aspect : 1f;
            float halfHeight = Mathf.Max(0.01f, orthoSize);
            float halfWidth = halfHeight * Mathf.Max(0.01f, aspect);

            float minX = minBounds.x + halfWidth;
            float maxX = maxBounds.x - halfWidth;
            float minY = minBounds.y + halfHeight;
            float maxY = maxBounds.y - halfHeight;

            if (minX > maxX)
            {
                float midX = (minBounds.x + maxBounds.x) * 0.5f;
                minX = midX;
                maxX = midX;
            }

            if (minY > maxY)
            {
                float midY = (minBounds.y + maxBounds.y) * 0.5f;
                minY = midY;
                maxY = midY;
            }

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (!autoHandleSceneLoaded)
                return;

            if (useSceneMapProfile)
                ApplySceneMapProfile();

            ResolveFollowTargetIfMissing();

            InitializeSceneSubAreas();

            ApplyMapCameraCullingMask();

            StartCoroutine(RebuildRuntimeMarkersAfterSceneLoaded());
            ExcludeMapLayersFromNonMapCameras();

            ResetFullMapState(true);

            miniCameraPos = followTarget != null
                ? followTarget.position + cameraOffset
                : mapCamera.transform.position;

            if (renderImmediatelyAfterSceneLoaded)
                StartCoroutine(RenderAfterSceneLoaded());
        }

        private void ResolveFollowTargetIfMissing()
        {
            if (followTarget != null)
                return;

            if (string.IsNullOrEmpty(autoFindFollowTargetTag))
                return;

            GameObject taggedObject = null;
            try
            {
                taggedObject = GameObject.FindGameObjectWithTag(autoFindFollowTargetTag);
            }
            catch (UnityException)
            {
                if (logMarkerSpawn)
                    Debug.Log("[MapRenderTextureController] Tag not defined for auto follow target: " + autoFindFollowTargetTag);
            }

            if (taggedObject != null)
            {
                followTarget = taggedObject.transform;
                miniCameraPos = ClampPositionForOrtho(followTarget.position + cameraOffset, miniOrthoSize);
            }
        }

        private IEnumerator RebuildRuntimeMarkersAfterSceneLoaded()
        {
            int delayFrames = Mathf.Max(0, rebuildMarkerDelayFramesAfterSceneLoaded);
            for (int i = 0; i < delayFrames; i++)
                yield return null;

            RebuildRuntimeMarkers();
        }

        private IEnumerator RenderAfterSceneLoaded()
        {
            yield return null;
            if (mapCamera == null)
                yield break;

            RenderMiniMap();
            if (isFullMapOpen)
                RenderFullMap();
        }

        private void ApplySceneMapProfile()
        {
            MapSceneProfile profile = FindAnyObjectByType<MapSceneProfile>();
            if (profile == null)
                return;

            if (profile.OverrideMiniOrthoSize)
                miniOrthoSize = Mathf.Max(0.1f, profile.MiniOrthoSize);

            if (profile.OverrideFullOrthoSize)
                fullOrthoSize = Mathf.Max(miniOrthoSize, profile.FullOrthoSize);

            if (profile.OverrideCameraOffset)
                cameraOffset = profile.CameraOffset;

            if (profile.OverrideClampBounds)
            {
                clampToBounds = true;
                minBounds = profile.MinBounds;
                maxBounds = profile.MaxBounds;
            }
            else if (profile.DisableClampBounds)
            {
                clampToBounds = false;
            }

            ApplyMapCameraCullingMask();
        }

        private void ApplyMapCameraCullingMask()
        {
            if (mapCamera == null)
                return;

            mapCamera.cullingMask = BuildMapOnlyMask();
        }

        private int BuildMapOnlyMask()
        {
            int tileMapLayer = LayerMask.NameToLayer(tileMapOnlyLayerName);
            int markerLayer = LayerMask.NameToLayer(markerOnlyLayerName);

            int mask = 0;
            if (tileMapLayer >= 0)
                mask |= 1 << tileMapLayer;
            if (markerLayer >= 0)
                mask |= 1 << markerLayer;

            if (mask == 0)
                Debug.Log("[MapRenderTextureController] Missing map layers. Check TileMapOnly / MarkerOnly layer names in inspector.");

            return mask;
        }

        private int BuildMapHiddenMask()
        {
            return BuildMapOnlyMask();
        }

        private void ExcludeMapLayersFromNonMapCameras()
        {
            if (!forceHideMapLayersFromOtherCameras)
                return;

            int hiddenMask = BuildMapHiddenMask();
            if (hiddenMask == 0)
                return;

            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCameras.Length; i++)
            {
                Camera cam = allCameras[i];
                if (cam == null || cam == mapCamera)
                    continue;

                cam.cullingMask &= ~hiddenMask;
            }
        }

        private void RebuildRuntimeMarkers()
        {
            CleanupRuntimeMarkers();
            runtimeMarkerRebuildQueued = false;

            if (!spawnRuntimeMarkers)
                return;

            int markerLayer = ResolveRuntimeMarkerLayer();
            if (markerLayer < 0)
            {
                Debug.Log("[MapRenderTextureController] Marker layer not found. Check MarkerOnly or prefab marker layer.");
                return;
            }

            runtimeMarkerRoot = new GameObject("RuntimeMarkerOnly");
            runtimeMarkerRoot.layer = markerLayer;
            MoveRuntimeMarkerRootToActiveScene();

            FindObjectsInactive inactiveMode = includeInactiveMarkerSources ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            MapMarkerSource[] sources = FindObjectsByType<MapMarkerSource>(inactiveMode, FindObjectsSortMode.None);
            int spawnedCount = 0;
            int skipLayerMask = 0;
            int skipNoPrefab = 0;
            int skipLockedPortal = 0;

            for (int i = 0; i < sources.Length; i++)
            {
                MapMarkerSource markerSource = sources[i];
                if (markerSource == null)
                    continue;

                Transform src = markerSource.transform;
                if (src == null)
                    continue;

                if (markerSourceLayers.value != 0 && (markerSourceLayers.value & (1 << src.gameObject.layer)) == 0)
                {
                    skipLayerMask++;
                    continue;
                }

                if (markerSource.MarkerType == MapMarkerType.Portal)
                {
                    PortalPoint portal = markerSource.GetComponentInParent<PortalPoint>();
                    if (portal != null && !portal.IsUnlocked)
                    {
                        skipLockedPortal++;
                        continue;
                    }
                }

                GameObject prefab = markerSource.MarkerPrefab;

                if (prefab == null)
                {
                    skipNoPrefab++;
                    continue;
                }

                Vector3 resolvedOffset = markerSource.WorldOffset;
                Vector3 resolvedScale = markerSource.MarkerScale;
                if (resolvedScale.x <= 0.0001f || resolvedScale.y <= 0.0001f || resolvedScale.z <= 0.0001f)
                    resolvedScale = Vector3.one;

                GameObject iconObj = Instantiate(prefab, runtimeMarkerRoot.transform);
                iconObj.name = "Marker_" + src.name;
                iconObj.transform.position = src.position + resolvedOffset;
                iconObj.transform.localScale = resolvedScale;
                SetLayerRecursively(iconObj, markerLayer);

                runtimeMarkerBindings.Add(new RuntimeMarkerBinding
                {
                    Source = src,
                    Icon = iconObj.transform,
                    Offset = resolvedOffset
                });

                spawnedCount++;
            }

            if (logMarkerSpawn)
            {
                Debug.Log("[MapRenderTextureController] RuntimeMarkerOnly built: sources=" + sources.Length
                    + ", spawned=" + spawnedCount
                    + ", skipLayerMask=" + skipLayerMask
                    + ", skipNoPrefab=" + skipNoPrefab
                    + ", skipLockedPortal=" + skipLockedPortal
                    + ", markerSourceLayersValue=" + markerSourceLayers.value);
            }
        }

        private int ResolveRuntimeMarkerLayer()
        {
            int markerLayer = LayerMask.NameToLayer(markerOnlyLayerName);
            if (markerLayer >= 0)
                return markerLayer;

            return -1;
        }

        private void MoveRuntimeMarkerRootToActiveScene()
        {
            if (runtimeMarkerRoot == null)
                return;

            UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
                return;

            if (runtimeMarkerRoot.scene != activeScene)
                SceneManager.MoveGameObjectToScene(runtimeMarkerRoot, activeScene);
        }

        private void UpdateRuntimeMarkers()
        {
            if (!spawnRuntimeMarkers)
                return;

            if (runtimeMarkerBindings.Count == 0)
            {
                if (autoRebuildMarkersOnSourceChange && Time.unscaledTime >= nextEmptyBindingsRetryTime)
                {
                    nextEmptyBindingsRetryTime = Time.unscaledTime + Mathf.Max(0.1f, emptyBindingsRetryInterval);
                    QueueRuntimeMarkerRebuild("bindings empty");
                }
                return;
            }

            bool removedInvalidBinding = false;

            for (int i = runtimeMarkerBindings.Count - 1; i >= 0; i--)
            {
                RuntimeMarkerBinding binding = runtimeMarkerBindings[i];
                if (binding == null || binding.Source == null || binding.Icon == null)
                {
                    runtimeMarkerBindings.RemoveAt(i);
                    removedInvalidBinding = true;
                    continue;
                }

                binding.Icon.position = binding.Source.position + binding.Offset;
            }

            if (removedInvalidBinding)
                QueueRuntimeMarkerRebuild("source destroyed or icon missing");
        }

        private void QueueRuntimeMarkerRebuild(string reason)
        {
            if (!autoRebuildMarkersOnSourceChange || runtimeMarkerRebuildQueued)
                return;

            runtimeMarkerRebuildQueued = true;
            StartCoroutine(RebuildRuntimeMarkersAfterSourceChange(reason));
        }

        private IEnumerator RebuildRuntimeMarkersAfterSourceChange(string reason)
        {
            int delayFrames = Mathf.Max(0, rebuildMarkerDelayFramesAfterSourceChange);
            for (int i = 0; i < delayFrames; i++)
                yield return null;

            runtimeMarkerRebuildQueued = false;
            RebuildRuntimeMarkers();

            if (logMarkerSpawn)
                Debug.Log("[MapRenderTextureController] Runtime markers rebuilt after source change: " + reason);
        }

        private void CleanupRuntimeMarkers()
        {
            if (runtimeMarkerRoot != null)
                Destroy(runtimeMarkerRoot);

            runtimeMarkerRoot = null;
            runtimeMarkerBindings.Clear();
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0)
                return;

            root.layer = layer;
            Transform t = root.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
            }
        }

        private void InitializeSceneSubAreas()
        {
            sceneSubAreas.Clear();

            MapSubArea[] subAreas = FindObjectsByType<MapSubArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sceneSubAreas.AddRange(subAreas);

            Vector2 playerPos = Vector2.zero;
            bool playerFound = false;
            if (followTarget != null)
            {
                playerPos = followTarget.position;
                playerFound = true;
            }
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(autoFindFollowTargetTag);
                if (playerObj != null)
                {
                    playerPos = playerObj.transform.position;
                    playerFound = true;
                }
            }

            foreach (var subArea in sceneSubAreas)
            {
                if (subArea == null) continue;

                string areaId = subArea.AreaId;
                bool isUnlocked = unlockedAreas.Contains(areaId);

                if (!isUnlocked && playerFound && subArea.ContainsPoint(playerPos))
                {
                    unlockedAreas.Add(areaId);
                    isUnlocked = true;
                }

                if (subArea.MapVisualObject != null)
                {
                    subArea.MapVisualObject.SetActive(isUnlocked);
                }
            }
        }

        public void UnlockArea(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return;

            if (!unlockedAreas.Contains(areaId))
            {
                unlockedAreas.Add(areaId);
                if (renderImmediatelyAfterSceneLoaded)
                {
                    RenderMiniMap();
                    if (isFullMapOpen)
                        RenderFullMap();
                }
            }

            foreach (var subArea in sceneSubAreas)
            {
                if (subArea != null && subArea.AreaId == areaId)
                {
                    if (subArea.MapVisualObject != null && !subArea.MapVisualObject.activeSelf)
                    {
                        subArea.MapVisualObject.SetActive(true);
                    }
                }
            }
        }

    }
}
