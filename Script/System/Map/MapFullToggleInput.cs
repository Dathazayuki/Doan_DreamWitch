using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DreamKnight.Player;
using DreamKnight.UI;
using DreamKnight.Systems.Zone;
using System.Collections;
using System.Collections.Generic;

namespace DreamKnight.Systems.Map
{
    [DisallowMultipleComponent]
    public class MapFullToggleInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MapRenderTextureController mapController;
        [SerializeField] private GameObject fullMapMenuRoot;
        [SerializeField] private RectTransform mapCursor;
        [SerializeField] private RectTransform fullMapClickArea;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private UIStateManager uiStateManager;

        [Header("Auto Rebind On Scene Loaded")]
        [SerializeField] private bool autoRebindOnSceneLoaded = true;
        [SerializeField] private string fullMapMenuRootTag = "MapFullMenu";
        [SerializeField] private string fullMapMenuRootName = "MenuFullMapRoot";

        [Header("Toggle Key")]
        [SerializeField] private KeyCode toggleMapKey = KeyCode.M;
        [SerializeField] private KeyCode teleportSelectedPortalKey = KeyCode.F;

        [Header("Pan/Zoom")]
        [SerializeField] private float zoomSensitivity = 0.04f;
        [SerializeField] private bool enableMousePanZoom = true;
        [SerializeField] private bool panWithLeftMouseHold = true;
        [SerializeField] private float mousePanSensitivity = 0.02f;
        [SerializeField] private float mouseWheelZoomSensitivity = 0.01f;
        [SerializeField] private bool placeCursorOnLeftClick = true;
        [SerializeField] private float clickToDragThresholdPixels = 8f;
        [SerializeField] private float portalMarkerSelectRadiusPixels = 24f;
        [SerializeField] private float selectedPortalCursorScaleMultiplier = 0.72f;
        [SerializeField] private bool keepCursorLockedToSelectedPortal = true;

        [Header("Portal Teleport")]
        [SerializeField] private float teleportJumpVelocity = 8f;
        [SerializeField] private float teleportDelayBeforeSpawn = 0.15f;

        [Header("Pause While Full Map Open")]
        [SerializeField] private bool pauseGameWhenFullMapOpen = true;

        private float previousTimeScale = 1f;
        private bool pausedByFullMap;
        private bool leftPointerDownOnMap;
        private bool leftDragInProgress;
        private Vector2 leftPointerDownScreenPos;
        private Vector2 lastMouseScreenPos;
        private PlayerController playerController;
        private PortalPoint selectedPortal;
        private readonly List<PortalPoint> portalsCache = new List<PortalPoint>(32);
        private bool teleportInProgress;
        private Vector3 mapCursorDefaultScale = Vector3.one;

        private void Awake()
        {
            if (mapController == null)
                mapController = FindAnyObjectByType<MapRenderTextureController>();

            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (uiStateManager == null)
                uiStateManager = UIStateManager.Instance;

            if (mapCursor != null)
            {
                mapCursorDefaultScale = mapCursor.localScale;
                SetMapCursorVisible(false);
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            PortalCheckpointService.OnActiveTeleportPortalChanged += HandleActiveTeleportPortalChanged;
            ApplyUiState(false);
        }

        private void Start()
        {
            if (autoRebindOnSceneLoaded)
                RebindSceneUiReferences();
        }

        private void OnDisable()
        {
            SetPausedByFullMap(false);
            SetMapCursorVisible(false);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            PortalCheckpointService.OnActiveTeleportPortalChanged -= HandleActiveTeleportPortalChanged;
            SetPausedByFullMap(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleMapKey))
                ToggleFullMap();

            if (mapController == null || !mapController.IsFullMapOpen)
                return;

            RectTransform targetRect = ResolveTargetMapRect();
            bool canUsePortalCursor = HasActiveTeleportPortal();

            Vector2 move = Vector2.zero;

            ProcessLeftMouseMapInteraction(targetRect, ref move, canUsePortalCursor);
            if (!canUsePortalCursor)
                SetMapCursorVisible(false);

            mapController.SetFullMapMoveInput(move);

            float zoomValue = 0f;

            if (enableMousePanZoom)
            {
                float mouseWheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(mouseWheel) > 0.0001f)
                    zoomValue += mouseWheel * mouseWheelZoomSensitivity;
            }

            if (Mathf.Abs(zoomValue) > 0.0001f)
                mapController.AddZoom(-zoomValue * zoomSensitivity);

            if (canUsePortalCursor && keepCursorLockedToSelectedPortal)
                UpdateCursorFromSelectedPortal(targetRect);

            if (canUsePortalCursor && Input.GetKeyDown(teleportSelectedPortalKey))
                TryTeleportToSelectedPortal();
        }

        public void OpenFullMapForPortalTeleport(PortalPoint sourcePortal)
        {
            if (sourcePortal == null || mapController == null)
                return;

            PortalCheckpointService.SetActiveTeleportPortal(sourcePortal);
            if (!mapController.IsFullMapOpen)
            {
                mapController.ResetFullMapState(false);
                mapController.SetMapOpen(true);
                ApplyUiState(true);
            }

            selectedPortal = sourcePortal;
            RectTransform rect = ResolveTargetMapRect();
            if (rect != null && mapController.TryProjectWorldToFullMapLocalPoint(GetPortalMarkerWorldPosition(sourcePortal), rect, out Vector2 p))
            {
                PlaceCursorAtLocalPoint(rect, p);
                SetCursorSelectableVisual(true);
            }
        }

        private void ToggleFullMap()
        {
            if (mapController == null)
                return;

            bool open = !mapController.IsFullMapOpen;

            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (open && uiStateManager != null && uiStateManager.IsAnyUIPanelActive())
                return;

            if (open)
                mapController.ResetFullMapState(false);

            mapController.SetMapOpen(open);
            ApplyUiState(open);
            if (open)
                SyncCursorToActivePortal();
        }

        private RectTransform ResolveTargetMapRect()
        {
            RectTransform targetRect = fullMapClickArea;
            if (targetRect == null && fullMapMenuRoot != null)
            {
                RawImage candidate = fullMapMenuRoot.GetComponentInChildren<RawImage>(true);
                if (candidate != null)
                    targetRect = candidate.rectTransform;
            }

            return targetRect;
        }

        private void ProcessLeftMouseMapInteraction(RectTransform targetRect, ref Vector2 move, bool allowCursorSelection)
        {
            if (targetRect == null)
                return;

            Vector2 mouseScreenPos = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, mouseScreenPos, uiCamera))
                {
                    leftPointerDownOnMap = true;
                    leftDragInProgress = false;
                    leftPointerDownScreenPos = mouseScreenPos;
                    lastMouseScreenPos = mouseScreenPos;
                }
                else
                {
                    leftPointerDownOnMap = false;
                    leftDragInProgress = false;
                }
            }

            if (leftPointerDownOnMap && Input.GetMouseButton(0))
            {
                Vector2 frameDelta = mouseScreenPos - lastMouseScreenPos;
                lastMouseScreenPos = mouseScreenPos;

                if (!leftDragInProgress)
                {
                    float threshold = Mathf.Max(0f, clickToDragThresholdPixels);
                    if ((mouseScreenPos - leftPointerDownScreenPos).sqrMagnitude >= threshold * threshold)
                        leftDragInProgress = true;
                }

                if (enableMousePanZoom && panWithLeftMouseHold && leftDragInProgress)
                {
                    float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                    move += new Vector2(-frameDelta.x, -frameDelta.y) * (mousePanSensitivity / dt);
                }
            }

            if (leftPointerDownOnMap && Input.GetMouseButtonUp(0))
            {
                if (allowCursorSelection && placeCursorOnLeftClick && !leftDragInProgress)
                {
                    if (!TrySelectPortalMarkerByScreenPosition(targetRect, mouseScreenPos))
                    {
                        selectedPortal = null;
                        SetCursorSelectableVisual(false);
                    }
                }

                leftPointerDownOnMap = false;
                leftDragInProgress = false;
            }

            if (enableMousePanZoom && !panWithLeftMouseHold)
            {
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                Vector2 axisDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                move += new Vector2(-axisDelta.x, -axisDelta.y) * (mousePanSensitivity / dt);
            }
        }

        private bool TrySelectPortalMarkerByScreenPosition(RectTransform mapRect, Vector2 screenPosition)
        {
            if (mapController == null || mapRect == null)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screenPosition, uiCamera, out Vector2 clickLocalPoint))
                return false;

            if (!mapRect.rect.Contains(clickLocalPoint))
                return false;

            PortalCheckpointService.GetRegisteredPortals(portalsCache);
            float maxRadius = Mathf.Max(4f, portalMarkerSelectRadiusPixels);
            float bestSqr = maxRadius * maxRadius;
            PortalPoint bestPortal = null;
            Vector2 bestPortalLocalPoint = Vector2.zero;

            for (int i = 0; i < portalsCache.Count; i++)
            {
                PortalPoint portal = portalsCache[i];
                if (portal == null || !portal.IsUnlocked)
                    continue;

                Vector3 markerWorldPosition = GetPortalMarkerWorldPosition(portal);
                if (!mapController.TryProjectWorldToFullMapLocalPoint(markerWorldPosition, mapRect, out Vector2 portalLocalPoint))
                    continue;

                float sqr = (portalLocalPoint - clickLocalPoint).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;

                bestSqr = sqr;
                bestPortal = portal;
                bestPortalLocalPoint = portalLocalPoint;
            }

            selectedPortal = bestPortal;
            if (selectedPortal == null)
            {
                SetCursorSelectableVisual(false);
                return false;
            }

            PlaceCursorAtLocalPoint(mapRect, bestPortalLocalPoint);
            SetCursorSelectableVisual(true);
            return true;
        }

        private void UpdateCursorFromSelectedPortal(RectTransform mapRect)
        {
            if (selectedPortal == null || mapRect == null || mapController == null)
                return;

            if (!selectedPortal.IsUnlocked)
            {
                selectedPortal = null;
                SetCursorSelectableVisual(false);
                return;
            }

            Vector3 markerWorldPosition = GetPortalMarkerWorldPosition(selectedPortal);
            if (!mapController.TryProjectWorldToFullMapLocalPoint(markerWorldPosition, mapRect, out Vector2 localPoint))
                return;

            PlaceCursorAtLocalPoint(mapRect, localPoint);
            SetCursorSelectableVisual(true);
        }

        private void PlaceCursorAtScreenPosition(RectTransform targetRect, Vector2 screenPosition)
        {
            if (mapCursor == null || targetRect == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPosition, uiCamera, out Vector2 localPoint))
                return;

            if (!targetRect.rect.Contains(localPoint))
                return;

            PlaceCursorAtLocalPoint(targetRect, localPoint);
        }

        private void PlaceCursorAtLocalPoint(RectTransform targetRect, Vector2 localPoint)
        {
            if (mapCursor == null || targetRect == null)
                return;

            if (mapCursor.parent == targetRect)
            {
                mapCursor.anchoredPosition = localPoint;
                return;
            }

            RectTransform cursorParent = mapCursor.parent as RectTransform;
            if (cursorParent != null)
            {
                Vector3 worldPoint = targetRect.TransformPoint(localPoint);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPoint);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(cursorParent, screenPoint, uiCamera, out Vector2 parentLocalPoint))
                {
                    mapCursor.anchoredPosition = parentLocalPoint;
                    return;
                }
            }

            mapCursor.position = targetRect.TransformPoint(localPoint);
        }

        private void TryTeleportToSelectedPortal()
        {
            if (!PortalCheckpointService.TryGetActiveTeleportPortal(out PortalPoint sourcePortal) || sourcePortal == null)
                return;

            if (teleportInProgress || selectedPortal == null || !selectedPortal.IsUnlocked)
                return;

            ResolvePlayerControllerIfMissing();
            if (playerController == null)
                return;

            StartCoroutine(TeleportToSelectedPortalRoutine(sourcePortal));
        }

        private IEnumerator TeleportToSelectedPortalRoutine(PortalPoint sourcePortal)
        {
            teleportInProgress = true;
            PortalPoint destinationPortal = selectedPortal;

            mapController?.SetMapOpen(false);
            ApplyUiState(false);

            ResolvePlayerControllerIfMissing();
            if (playerController != null && playerController.Movement != null)
            {
                Vector2 v = playerController.Movement.Velocity;
                playerController.Movement.SetVelocity(new Vector2(v.x, Mathf.Max(v.y, teleportJumpVelocity)));
            }

            float delay = Mathf.Max(0f, teleportDelayBeforeSpawn);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (destinationPortal != null && playerController != null)
            {
                playerController.transform.position = destinationPortal.GetArrivalWorldPosition();
                playerController.Movement?.StopMovement();
            }

            selectedPortal = null;
            PortalCheckpointService.ClearActiveTeleportPortal(sourcePortal);
            SetCursorSelectableVisual(false);

            teleportInProgress = false;
        }

        private void ResolvePlayerControllerIfMissing()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();
        }

        private void ApplyUiState(bool fullMapOpen)
        {
            uiStateManager = uiStateManager != null ? uiStateManager : UIStateManager.Instance;
            if (uiStateManager != null)
            {
                if (fullMapOpen)
                    uiStateManager.Open(UIState.Map);
                else
                    uiStateManager.Close(UIState.Map);
            }
            else if (fullMapMenuRoot != null)
            {
                fullMapMenuRoot.SetActive(fullMapOpen);
            }

            bool isMapOpen = uiStateManager != null
                ? uiStateManager.IsOpen(UIState.Map)
                : (fullMapMenuRoot != null && fullMapMenuRoot.activeSelf);

            SetPausedByFullMap(isMapOpen);

            if (mapController != null)
            {
                if (!fullMapOpen)
                {
                    mapController.SetFullMapMoveInput(Vector2.zero);
                    selectedPortal = null;
                    SetCursorSelectableVisual(false);
                }
            }
        }

        private void SetPausedByFullMap(bool fullMapOpen)
        {
            if (!pauseGameWhenFullMapOpen)
            {
                if (pausedByFullMap)
                {
                    Time.timeScale = previousTimeScale;
                    pausedByFullMap = false;
                }
                return;
            }

            if (fullMapOpen)
            {
                if (!pausedByFullMap)
                {
                    previousTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                    pausedByFullMap = true;
                }
            }
            else if (pausedByFullMap)
            {
                Time.timeScale = previousTimeScale;
                pausedByFullMap = false;
            }
        }

        private void HandleActiveTeleportPortalChanged(PortalPoint portal)
        {
            if (mapController == null || !mapController.IsFullMapOpen)
            {
                SetMapCursorVisible(false);
                return;
            }

            if (portal == null)
            {
                selectedPortal = null;
                SetCursorSelectableVisual(false);
                return;
            }

            SyncCursorToActivePortal();
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (!autoRebindOnSceneLoaded)
                return;

            if (mapController == null)
                mapController = FindAnyObjectByType<MapRenderTextureController>();

            RebindSceneUiReferences();

            if (mapController != null)
                mapController.ResetFullMapState(true);

            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            selectedPortal = null;
            PortalCheckpointService.ClearActiveTeleportPortal(null);
            SetCursorSelectableVisual(false);

            ApplyUiState(false);
        }

        private Vector3 GetPortalMarkerWorldPosition(PortalPoint portal)
        {
            if (portal == null)
                return Vector3.zero;

            MapMarkerSource source = portal.GetComponentInChildren<MapMarkerSource>(true);
            if (source != null && source.MarkerType == MapMarkerType.Portal)
                return source.transform.position + source.WorldOffset;

            return portal.WorldPosition;
        }

        private void SetCursorSelectableVisual(bool selectable)
        {
            if (mapCursor == null)
                return;

            bool shouldShow = selectable && HasActiveTeleportPortal();
            if (!shouldShow)
                mapCursor.localScale = mapCursorDefaultScale;

            SetMapCursorVisible(shouldShow);
            if (!shouldShow)
                return;

            if (mapCursorDefaultScale.sqrMagnitude <= 0.0001f)
                mapCursorDefaultScale = mapCursor.localScale;

            float scaleMul = Mathf.Max(0.1f, selectedPortalCursorScaleMultiplier);
            mapCursor.localScale = mapCursorDefaultScale * scaleMul;
        }

        private void SyncCursorToActivePortal()
        {
            if (mapController == null || !mapController.IsFullMapOpen)
            {
                SetMapCursorVisible(false);
                return;
            }

            if (!PortalCheckpointService.TryGetActiveTeleportPortal(out PortalPoint activePortal) || activePortal == null)
            {
                selectedPortal = null;
                SetCursorSelectableVisual(false);
                return;
            }

            selectedPortal = activePortal;
            RectTransform rect = ResolveTargetMapRect();
            if (rect != null && mapController.TryProjectWorldToFullMapLocalPoint(GetPortalMarkerWorldPosition(activePortal), rect, out Vector2 p))
            {
                PlaceCursorAtLocalPoint(rect, p);
                SetCursorSelectableVisual(true);
            }
            else
            {
                SetCursorSelectableVisual(false);
            }
        }

        private bool HasActiveTeleportPortal()
        {
            return PortalCheckpointService.TryGetActiveTeleportPortal(out PortalPoint portal) && portal != null;
        }

        private void SetMapCursorVisible(bool visible)
        {
            if (mapCursor != null && mapCursor.gameObject.activeSelf != visible)
                mapCursor.gameObject.SetActive(visible);
        }

        private void RebindSceneUiReferences()
        {
            GameObject found = FindByTagOrName(fullMapMenuRootTag, fullMapMenuRootName);
            if (found != null)
                fullMapMenuRoot = found;
        }

        private static GameObject FindByTagOrName(string tagName, string objectName)
        {
            if (!string.IsNullOrWhiteSpace(tagName))
            {
                try
                {
                    GameObject byTag = GameObject.FindGameObjectWithTag(tagName);
                    if (byTag != null)
                        return byTag;
                }
                catch (UnityException)
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(objectName))
            {
                GameObject byName = GameObject.Find(objectName);
                if (byName != null)
                    return byName;
            }

            return null;
        }
    }
}
