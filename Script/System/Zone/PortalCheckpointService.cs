using UnityEngine;
using System.Collections.Generic;

namespace DreamKnight.Systems.Zone
{
    public static class PortalCheckpointService
    {
        public static event System.Action<string> OnPortalUnlockChanged;
        public static event System.Action OnPortalRegistryChanged;
        public static event System.Action<PortalPoint> OnActiveTeleportPortalChanged;

        private const string PortalUnlockStateResourcePath = "PortalUnlockState";
        private static readonly List<PortalPoint> registeredPortals = new List<PortalPoint>(32);
        private static PortalUnlockStateSO unlockState;
        private static PortalPoint activeTeleportPortal;

        public static bool IsUnlocked(string portalId)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return false;

            return GetOrCreateUnlockState().IsUnlocked(portalId);
        }

        public static void Unlock(string portalId)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return;

            if (!GetOrCreateUnlockState().Unlock(portalId))
                return;

            OnPortalUnlockChanged?.Invoke(portalId);
        }

        public static void SetUnlockState(PortalUnlockStateSO state)
        {
            unlockState = state;
        }

        public static void ResetUnlockState(bool clearUnlockedPortalIds = true)
        {
            PortalUnlockStateSO state = GetOrCreateUnlockState();
            if (clearUnlockedPortalIds)
                state.ResetState();
        }

        public static void ResetSessionState()
        {
            registeredPortals.Clear();
            activeTeleportPortal = null;
            OnPortalRegistryChanged?.Invoke();
            OnActiveTeleportPortalChanged?.Invoke(null);
        }

        public static void CaptureUnlockedPortalIds(List<string> output)
        {
            GetOrCreateUnlockState().GetUnlockedPortalIds(output);
        }

        public static void LoadUnlockedPortalIds(IEnumerable<string> portalIds)
        {
            GetOrCreateUnlockState().LoadUnlockedPortalIds(portalIds);
            OnPortalRegistryChanged?.Invoke();

            if (portalIds == null)
                return;

            foreach (string portalId in portalIds)
            {
                if (!string.IsNullOrWhiteSpace(portalId))
                    OnPortalUnlockChanged?.Invoke(portalId);
            }
        }

        public static void RegisterPortal(PortalPoint portal)
        {
            if (portal == null || registeredPortals.Contains(portal))
                return;

            registeredPortals.Add(portal);
            OnPortalRegistryChanged?.Invoke();
        }

        public static void UnregisterPortal(PortalPoint portal)
        {
            if (portal == null)
                return;

            if (registeredPortals.Remove(portal))
                OnPortalRegistryChanged?.Invoke();
        }

        public static int GetRegisteredPortals(List<PortalPoint> output)
        {
            if (output == null)
                return 0;

            output.Clear();
            for (int i = 0; i < registeredPortals.Count; i++)
            {
                PortalPoint portal = registeredPortals[i];
                if (portal != null)
                    output.Add(portal);
            }

            return output.Count;
        }

        public static bool TryGetActiveTeleportPortal(out PortalPoint portal)
        {
            portal = activeTeleportPortal;
            return portal != null;
        }

        public static void SetActiveTeleportPortal(PortalPoint portal)
        {
            if (activeTeleportPortal == portal)
                return;

            activeTeleportPortal = portal;
            OnActiveTeleportPortalChanged?.Invoke(activeTeleportPortal);
        }

        public static void ClearActiveTeleportPortal(PortalPoint portal)
        {
            if (portal != null && activeTeleportPortal != portal)
                return;

            if (activeTeleportPortal == null)
                return;

            activeTeleportPortal = null;
            OnActiveTeleportPortalChanged?.Invoke(null);
        }

        private static PortalUnlockStateSO GetOrCreateUnlockState()
        {
            if (unlockState != null)
                return unlockState;

            unlockState = Resources.Load<PortalUnlockStateSO>(PortalUnlockStateResourcePath);
            if (unlockState == null)
                unlockState = ScriptableObject.CreateInstance<PortalUnlockStateSO>();

            return unlockState;
        }
    }
}
