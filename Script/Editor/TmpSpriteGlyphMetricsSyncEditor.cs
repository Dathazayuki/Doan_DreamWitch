#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace DreamKnight.EditorTools
{
    public static class TmpSpriteGlyphMetricsSyncEditor
    {
        [MenuItem("Tools/TMP/Sync Glyph Metrics = Glyph Rect")]
        private static void SyncSelectedSpriteAsset()
        {
            TMP_SpriteAsset spriteAsset = Selection.activeObject as TMP_SpriteAsset;
            if (spriteAsset == null)
            {
                Debug.LogWarning("Select a TMP_SpriteAsset in Project window first.");
                return;
            }

            int updatedCount = 0;
            for (int i = 0; i < spriteAsset.spriteGlyphTable.Count; i++)
            {
                TMP_SpriteGlyph glyph = spriteAsset.spriteGlyphTable[i];
                GlyphRect rect = glyph.glyphRect;

                glyph.metrics = new GlyphMetrics(
                    rect.width,
                    rect.height,
                    0f,
                    rect.height,
                    rect.width
                );

                updatedCount++;
            }

            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TMP] Synced {updatedCount} glyph metrics in {spriteAsset.name}.");
        }

        [MenuItem("Tools/TMP/Sync Glyph Metrics = Glyph Rect", true)]
        private static bool ValidateSyncSelectedSpriteAsset()
        {
            return Selection.activeObject is TMP_SpriteAsset;
        }
    }
}
#endif
