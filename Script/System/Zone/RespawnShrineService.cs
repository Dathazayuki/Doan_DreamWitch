using UnityEngine;

namespace DreamKnight.Systems.Zone
{
    /// <summary>
    /// Simple singleton service to track the current respawn shrine location.
    /// Only supports a single shrine at a time (multi-shrine support removed).
    /// Stores both position and scene name so respawn works in any scene with a shrine.
    /// </summary>
    public static class RespawnShrineService
    {
        private static Vector3 shrinePosition = Vector3.zero;
        private static string shrineSceneName = string.Empty;
        private static bool hasShrine = false;

        /// <summary>
        /// Register the current shrine location with its scene name.
        /// </summary>
        public static void RegisterShrine(Vector3 position, string sceneName)
        {
            shrinePosition = position;
            shrineSceneName = sceneName;
            hasShrine = true;
            Debug.Log($"[RespawnShrineService] Shrine registered at {position} in scene '{sceneName}'");
        }

        /// <summary>
        /// Get the current shrine respawn position.
        /// </summary>
        public static Vector3 GetShrinePosition()
        {
            if (!hasShrine)
            {
                Debug.LogWarning("[RespawnShrineService] No shrine registered! Returning zero.");
                return Vector3.zero;
            }
            return shrinePosition;
        }

        /// <summary>
        /// Get the scene name where the shrine is located.
        /// </summary>
        public static string GetShrineSceneName()
        {
            if (!hasShrine)
            {
                Debug.LogWarning("[RespawnShrineService] No shrine registered! Returning empty string.");
                return string.Empty;
            }
            return shrineSceneName;
        }

        /// <summary>
        /// Check if a shrine has been registered.
        /// </summary>
        public static bool HasRegisteredShrine => hasShrine;

        /// <summary>
        /// Clear the shrine (used when resetting or leaving a scene).
        /// </summary>
        public static void ClearShrine()
        {
            hasShrine = false;
            shrinePosition = Vector3.zero;
            shrineSceneName = string.Empty;
            Debug.Log("[RespawnShrineService] Shrine cleared");
        }
    }
}

