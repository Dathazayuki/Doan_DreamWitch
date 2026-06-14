using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace DreamKnight.EditorTools
{
    public static class LegacyUiPrefabMigrationTool
    {
        private const string MenuRoot = "Tools/DreamKnight/UI Migration/";

        [MenuItem(MenuRoot + "1) Scan Missing Scripts In Current Prefab")]
        public static void ScanMissingScriptsInCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            try
            {
                int missingCount = CountMissingScriptsRecursive(root.transform);
                if (missingCount > 0)
                    Debug.Log($"[UI Migration] Missing={missingCount} in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                else
                    Debug.Log($"[UI Migration] No missing scripts in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, false);
            }
        }

        [MenuItem(MenuRoot + "2) Remove Missing Scripts In Current Prefab")]
        public static void RemoveMissingScriptsInCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            bool changed = false;
            try
            {
                int removedInPrefab = RemoveMissingScriptsRecursive(root.transform);
                if (removedInPrefab > 0)
                {
                    changed = true;
                    Debug.Log($"[UI Migration] Removed missing scripts={removedInPrefab} in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                }
                else
                {
                    Debug.Log($"[UI Migration] No missing scripts to remove in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                }
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, changed);
            }
        }

        [MenuItem(MenuRoot + "3) Auto Rebind Image/TMP Assets In Current Prefab")]
        public static void AutoRebindUiAssetsInCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            Dictionary<string, List<string>> spritePathsByName = BuildAssetIndex("t:Sprite");
            Dictionary<string, List<string>> fontPathsByName = BuildAssetIndex("t:TMP_FontAsset");

            int reboundSprites = 0;
            int reboundFonts = 0;
            bool changed = false;

            try
            {
                Image[] images = root.GetComponentsInChildren<Image>(true);
                for (int imgIndex = 0; imgIndex < images.Length; imgIndex++)
                {
                    Image image = images[imgIndex];
                    if (image == null || image.sprite != null)
                        continue;

                    string assetName = image.gameObject.name;
                    if (!TryFindBestAssetPath(prefabPath, assetName, spritePathsByName, out string spritePath))
                        continue;

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                        continue;

                    image.sprite = sprite;
                    EditorUtility.SetDirty(image);
                    reboundSprites++;
                    changed = true;
                }

                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int textIndex = 0; textIndex < texts.Length; textIndex++)
                {
                    TMP_Text text = texts[textIndex];
                    if (text == null || text.font != null)
                        continue;

                    string assetName = text.gameObject.name;
                    if (!TryFindBestAssetPath(prefabPath, assetName, fontPathsByName, out string fontPath))
                        continue;

                    TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
                    if (font == null)
                        continue;

                    text.font = font;
                    EditorUtility.SetDirty(text);
                    reboundFonts++;
                    changed = true;
                }

                if (changed)
                {
                    Debug.Log($"[UI Migration] Rebound assets in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                }
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, changed);
            }

            Debug.Log($"[UI Migration] Rebind complete. Prefab changed: {changed}, sprites rebound: {reboundSprites}, TMP fonts rebound: {reboundFonts}");
        }

        [MenuItem(MenuRoot + "4) Run Full Migration On Current Prefab")]
        public static void RunFullMigrationOnCurrentPrefab()
        {
            ConvertLegacyUiToUnityUiOnCurrentPrefabKeepLegacy();
            RemoveMissingScriptsInCurrentPrefab();
            AutoRebindUiAssetsInCurrentPrefab();
            ScanMissingScriptsInCurrentPrefab();
            Debug.Log("[UI Migration] Current prefab migration pass completed.");
        }

        [MenuItem(MenuRoot + "5) Convert Legacy UI -> Unity UI/TMP (Keep Legacy)")]
        public static void ConvertLegacyUiToUnityUiOnCurrentPrefabKeepLegacy()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            int convertedImageCount = 0;
            int convertedTextCount = 0;
            bool changed = false;

            try
            {
                Component[] components = root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    Component source = components[i];
                    if (source == null)
                        continue;

                    if (source is Image || source is TMP_Text || source is Canvas || source is CanvasRenderer)
                        continue;

                    if (TryConvertLegacyImageComponent(source, out bool imageChanged))
                    {
                        if (imageChanged)
                        {
                            convertedImageCount++;
                            changed = true;
                        }
                        continue;
                    }

                    if (TryConvertLegacyTextComponent(source, out bool textChanged))
                    {
                        if (textChanged)
                        {
                            convertedTextCount++;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    Debug.Log($"[UI Migration] Legacy -> Unity conversion done. Image={convertedImageCount}, TMP_Text={convertedTextCount} in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                }
                else
                {
                    Debug.Log($"[UI Migration] No convertible legacy UI components found in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                }
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, changed);
            }
        }

        [MenuItem(MenuRoot + "6) Translate TMP JP -> VI In Current Prefab")]
        public static void TranslateTmpJpToViInCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            if (!TryGetTranslationTable(out JpViTranslationTableSO table, out string tablePath))
                return;

            int translatedCount = 0;
            int unchangedCount = 0;
            int japaneseDetectedCount = 0;
            var unmapped = new HashSet<string>(StringComparer.Ordinal);
            bool changed = false;

            try
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text tmp = texts[i];
                    if (tmp == null)
                        continue;

                    string src = tmp.text;
                    if (string.IsNullOrWhiteSpace(src))
                    {
                        unchangedCount++;
                        continue;
                    }

                    bool hasJp = ContainsJapanese(src);
                    if (hasJp)
                        japaneseDetectedCount++;

                    string result = TranslateTextWithTable(src, table, out bool mapped);

                    if (mapped && !string.Equals(src, result, StringComparison.Ordinal))
                    {
                        tmp.text = result;
                        EditorUtility.SetDirty(tmp);
                        translatedCount++;
                        changed = true;
                    }
                    else
                    {
                        unchangedCount++;
                        if (hasJp)
                            unmapped.Add(src);
                    }
                }

                // Saving is handled in finally.
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, changed);
            }

            Debug.Log($"[UI Migration] TMP JP->VI done on {prefabPath}. Table={tablePath}, translated={translatedCount}, unchanged={unchangedCount}, jpDetected={japaneseDetectedCount}, unmapped={unmapped.Count}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
            if (unmapped.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[UI Migration] Unmapped JP texts:");
                foreach (string line in unmapped)
                    sb.AppendLine("- " + line);
                Debug.Log(sb.ToString(), AssetDatabase.LoadMainAssetAtPath(prefabPath));
            }
        }

        [MenuItem(MenuRoot + "7) List Unmapped JP TMP Texts In Current Prefab")]
        public static void ListUnmappedJpTmpTextsInCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            if (!TryGetTranslationTable(out JpViTranslationTableSO table, out string _))
                return;

            var unmapped = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text tmp = texts[i];
                    if (tmp == null)
                        continue;

                    string src = tmp.text;
                    if (string.IsNullOrWhiteSpace(src) || !ContainsJapanese(src))
                        continue;

                    TranslateTextWithTable(src, table, out bool mapped);
                    if (!mapped)
                        unmapped.Add(src);
                }
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, false);
            }

            if (unmapped.Count == 0)
            {
                Debug.Log($"[UI Migration] No unmapped JP TMP text in {prefabPath}", AssetDatabase.LoadMainAssetAtPath(prefabPath));
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[UI Migration] Unmapped JP TMP text count={unmapped.Count} in {prefabPath}");
            foreach (string line in unmapped)
                sb.AppendLine("- " + line);
            Debug.Log(sb.ToString(), AssetDatabase.LoadMainAssetAtPath(prefabPath));
        }

        [MenuItem(MenuRoot + "8) Collect JP TMP Texts To Translation Table")]
        public static void CollectJpTmpTextsToTranslationTableFromCurrentPrefab()
        {
            if (!TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload))
                return;

            if (!TryGetTranslationTable(out JpViTranslationTableSO table, out string tablePath))
                return;

            int collected = 0;
            int alreadyExists = 0;
            int ignoredNonJapanese = 0;
            bool changed = false;

            try
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    TMP_Text tmp = texts[i];
                    if (tmp == null)
                        continue;

                    string src = NormalizeForExactMatch(tmp.text);
                    if (string.IsNullOrEmpty(src) || !ContainsJapanese(src))
                    {
                        ignoredNonJapanese++;
                        continue;
                    }

                    if (HasJapaneseEntry(table, src))
                    {
                        alreadyExists++;
                        continue;
                    }

                    table.Entries.Add(new JpViTranslationTableSO.Entry
                    {
                        japanese = src,
                        vietnamese = string.Empty,
                        enabled = true
                    });

                    collected++;
                    changed = true;
                }
            }
            finally
            {
                SaveAndReleasePrefabContext(root, prefabPath, shouldUnload, false);
            }

            if (changed)
            {
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[UI Migration] Collected JP TMP text from {prefabPath} -> {tablePath}. added={collected}, exists={alreadyExists}, ignoredNonJP={ignoredNonJapanese}", AssetDatabase.LoadMainAssetAtPath(tablePath));
        }

        private static bool TryGetCurrentPrefabContext(out string prefabPath, out GameObject root, out bool shouldUnload)
        {
            prefabPath = null;
            root = null;
            shouldUnload = false;

            PrefabStage currentStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null && !string.IsNullOrWhiteSpace(currentStage.assetPath) && currentStage.prefabContentsRoot != null)
            {
                prefabPath = currentStage.assetPath;
                root = currentStage.prefabContentsRoot;
                shouldUnload = false;
                return true;
            }

            if (!TryGetCurrentPrefabPath(out prefabPath))
                return false;

            root = PrefabUtility.LoadPrefabContents(prefabPath);
            shouldUnload = true;
            return root != null;
        }

        private static void SaveAndReleasePrefabContext(GameObject root, string prefabPath, bool shouldUnload, bool changed)
        {
            if (root == null)
                return;

            if (changed)
            {
                if (shouldUnload)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                else
                    PrefabUtility.SavePrefabAsset(root);
            }

            if (shouldUnload)
                PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem(MenuRoot + "9) Auto Translate Empty JP->VI In Table (Online)")]
        public static void AutoTranslateEmptyEntriesInTableOnline()
        {
            if (!TryGetTranslationTable(out JpViTranslationTableSO table, out string tablePath))
                return;

            if (table.Entries == null || table.Entries.Count == 0)
            {
                Debug.LogWarning("[UI Migration] Translation table has no entries.", AssetDatabase.LoadMainAssetAtPath(tablePath));
                return;
            }

            if (!table.AutoTranslateEnabled)
            {
                Debug.LogWarning("[UI Migration] Auto translate is disabled in translation table asset.", AssetDatabase.LoadMainAssetAtPath(tablePath));
                return;
            }

            if (string.IsNullOrWhiteSpace(table.TranslateEndpoint))
            {
                Debug.LogWarning("[UI Migration] Missing translate endpoint in translation table asset.", AssetDatabase.LoadMainAssetAtPath(tablePath));
                return;
            }

            int translated = 0;
            int skipped = 0;
            int failed = 0;

            try
            {
                int total = table.Entries.Count;
                for (int i = 0; i < table.Entries.Count; i++)
                {
                    JpViTranslationTableSO.Entry e = table.Entries[i];
                    float progress = total <= 0 ? 1f : (i + 1f) / total;
                    EditorUtility.DisplayProgressBar("Auto Translate JP->VI", $"Entry {i + 1}/{total}", progress);

                    if (e == null || !e.enabled)
                    {
                        skipped++;
                        continue;
                    }

                    string jp = NormalizeForExactMatch(e.japanese);
                    if (string.IsNullOrWhiteSpace(jp))
                    {
                        skipped++;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(e.vietnamese))
                    {
                        skipped++;
                        continue;
                    }

                    if (!TryTranslateOnline(jp, table, out string vi, out string error))
                    {
                        failed++;
                        if (!string.IsNullOrWhiteSpace(error))
                            Debug.LogWarning($"[UI Migration] Auto translate failed: {error} | JP: {jp}", AssetDatabase.LoadMainAssetAtPath(tablePath));
                        continue;
                    }

                    vi = NormalizeForExactMatch(vi);
                    if (string.IsNullOrWhiteSpace(vi))
                    {
                        failed++;
                        continue;
                    }

                    e.vietnamese = vi;
                    translated++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (translated > 0)
            {
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[UI Migration] Auto translate table done. translated={translated}, skipped={skipped}, failed={failed}, table={tablePath}", AssetDatabase.LoadMainAssetAtPath(tablePath));
        }

        private static bool TryGetCurrentPrefabPath(out string prefabPath)
        {
            prefabPath = null;

            PrefabStage currentStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null && !string.IsNullOrWhiteSpace(currentStage.assetPath))
            {
                prefabPath = currentStage.assetPath;
                return true;
            }

            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
                if (!string.IsNullOrWhiteSpace(selectedPath) && PrefabUtility.GetPrefabAssetType(selectedObject) != PrefabAssetType.NotAPrefab)
                {
                    prefabPath = selectedPath;
                    return true;
                }
            }

            UnityEngine.Object selectedAsset = Selection.activeObject;
            if (selectedAsset != null)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedAsset);
                if (!string.IsNullOrWhiteSpace(selectedPath) && string.Equals(Path.GetExtension(selectedPath), ".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    prefabPath = selectedPath;
                    return true;
                }
            }

            Debug.LogWarning("[UI Migration] No prefab is currently open/selected. Open a prefab in Prefab Mode or select a prefab asset first.");
            return false;
        }

        private static int CountMissingScriptsRecursive(Transform root)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);
            for (int i = 0; i < root.childCount; i++)
                count += CountMissingScriptsRecursive(root.GetChild(i));
            return count;
        }

        private static int RemoveMissingScriptsRecursive(Transform root)
        {
            int before = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);
            if (before > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root.gameObject);

            int removed = before;
            for (int i = 0; i < root.childCount; i++)
                removed += RemoveMissingScriptsRecursive(root.GetChild(i));

            return removed;
        }

        private static Dictionary<string, List<string>> BuildAssetIndex(string filter)
        {
            var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;

                if (!index.TryGetValue(fileName, out List<string> list))
                {
                    list = new List<string>();
                    index[fileName] = list;
                }

                list.Add(path);
            }

            return index;
        }

        private static bool TryFindBestAssetPath(string prefabPath, string objectName, Dictionary<string, List<string>> index, out string bestPath)
        {
            bestPath = null;
            if (string.IsNullOrWhiteSpace(objectName) || index == null)
                return false;

            if (!index.TryGetValue(objectName, out List<string> candidates) || candidates == null || candidates.Count == 0)
                return false;

            if (candidates.Count == 1)
            {
                bestPath = candidates[0];
                return true;
            }

            string prefabDirectory = Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
            int bestScore = int.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                int score = 0;

                if (!string.IsNullOrEmpty(prefabDirectory) && candidate.StartsWith(prefabDirectory, StringComparison.OrdinalIgnoreCase))
                    score += 1000;

                score += GetCommonPrefixLength(prefabPath, candidate);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = candidate;
                }
            }

            return !string.IsNullOrEmpty(bestPath);
        }

        private static int GetCommonPrefixLength(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0;

            int len = Mathf.Min(a.Length, b.Length);
            int i = 0;
            while (i < len && char.ToLowerInvariant(a[i]) == char.ToLowerInvariant(b[i]))
                i++;

            return i;
        }

        private static bool TryConvertLegacyImageComponent(Component source, out bool changed)
        {
            changed = false;

            Sprite sprite = ReadSpriteFromComponent(source);
            if (sprite == null)
                return false;

            GameObject go = source.gameObject;
            CanvasRenderer canvasRenderer = go.GetComponent<CanvasRenderer>();
            if (canvasRenderer == null)
                canvasRenderer = go.AddComponent<CanvasRenderer>();

            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
                changed = true;
            }

            if (image.sprite == null)
            {
                image.sprite = sprite;
                changed = true;
            }

            if (TryReadColorFromComponent(source, out Color color) && image.color != color)
            {
                image.color = color;
                changed = true;
            }

            if (TryReadBoolFromComponent(source, "raycastTarget", out bool raycast) && image.raycastTarget != raycast)
            {
                image.raycastTarget = raycast;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(go);

            return true;
        }

        private static bool TryConvertLegacyTextComponent(Component source, out bool changed)
        {
            changed = false;

            string text = ReadStringFromComponent(source, "text", "m_Text", "label", "content");
            if (string.IsNullOrEmpty(text) && !LooksLikeLegacyTextType(source.GetType()))
                return false;

            GameObject go = source.gameObject;
            CanvasRenderer canvasRenderer = go.GetComponent<CanvasRenderer>();
            if (canvasRenderer == null)
                canvasRenderer = go.AddComponent<CanvasRenderer>();

            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                {
                    tmp = go.AddComponent<TextMeshProUGUI>();
                    changed = true;
                }
            }

            if (!string.IsNullOrEmpty(text) && tmp.text != text)
            {
                tmp.text = text;
                changed = true;
            }

            if (TryReadFloatFromComponent(source, out float fontSize, "fontSize", "m_FontSize") && Math.Abs(tmp.fontSize - fontSize) > 0.01f)
            {
                tmp.fontSize = Mathf.Max(1f, fontSize);
                changed = true;
            }

            if (TryReadColorFromComponent(source, out Color color) && tmp.color != color)
            {
                tmp.color = color;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(go);

            return changed || LooksLikeLegacyTextType(source.GetType());
        }

        private static bool LooksLikeLegacyTextType(Type t)
        {
            string name = t.Name;
            return name.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Sprite ReadSpriteFromComponent(Component source)
        {
            return ReadObjectFromComponent<Sprite>(source, "sprite", "m_Sprite", "sourceImage", "image", "m_Image", "mainTexture");
        }

        private static bool TryReadColorFromComponent(Component source, out Color value)
        {
            value = Color.white;

            if (TryReadFieldValue(source, out Color color, "color", "m_Color", "fontColor", "tint"))
            {
                value = color;
                return true;
            }

            return false;
        }

        private static string ReadStringFromComponent(Component source, params string[] names)
        {
            if (TryReadFieldValue(source, out string value, names))
                return value;

            return string.Empty;
        }

        private static bool TryReadBoolFromComponent(Component source, string name, out bool value)
        {
            value = false;
            return TryReadFieldValue(source, out value, name);
        }

        private static bool TryReadFloatFromComponent(Component source, out float value, params string[] names)
        {
            value = 0f;
            return TryReadFieldValue(source, out value, names);
        }

        private static T ReadObjectFromComponent<T>(Component source, params string[] names) where T : UnityEngine.Object
        {
            if (TryReadFieldValue(source, out T value, names))
                return value;

            return null;
        }

        private static bool TryReadFieldValue<T>(Component source, out T value, params string[] candidateNames)
        {
            value = default;
            if (source == null || candidateNames == null || candidateNames.Length == 0)
                return false;

            SerializedObject so = new SerializedObject(source);
            for (int i = 0; i < candidateNames.Length; i++)
            {
                SerializedProperty p = so.FindProperty(candidateNames[i]);
                if (p != null && TryReadSerializedProperty(p, out value))
                    return true;
            }

            Type type = source.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (int i = 0; i < candidateNames.Length; i++)
            {
                string name = candidateNames[i];
                FieldInfo field = type.GetField(name, flags);
                if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
                {
                    object raw = field.GetValue(source);
                    if (raw is T cast)
                    {
                        value = cast;
                        return true;
                    }
                }

                PropertyInfo prop = type.GetProperty(name, flags);
                if (prop != null && prop.CanRead && typeof(T).IsAssignableFrom(prop.PropertyType))
                {
                    object raw = prop.GetValue(source, null);
                    if (raw is T cast)
                    {
                        value = cast;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryReadSerializedProperty<T>(SerializedProperty p, out T value)
        {
            value = default;
            try
            {
                if (typeof(T) == typeof(string) && p.propertyType == SerializedPropertyType.String)
                {
                    value = (T)(object)p.stringValue;
                    return true;
                }

                if (typeof(T) == typeof(float) && p.propertyType == SerializedPropertyType.Float)
                {
                    value = (T)(object)p.floatValue;
                    return true;
                }

                if (typeof(T) == typeof(bool) && p.propertyType == SerializedPropertyType.Boolean)
                {
                    value = (T)(object)p.boolValue;
                    return true;
                }

                if (typeof(T) == typeof(Color) && p.propertyType == SerializedPropertyType.Color)
                {
                    value = (T)(object)p.colorValue;
                    return true;
                }

                if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) && p.propertyType == SerializedPropertyType.ObjectReference)
                {
                    UnityEngine.Object obj = p.objectReferenceValue;
                    if (obj is T cast)
                    {
                        value = cast;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryGetTranslationTable(out JpViTranslationTableSO table, out string tablePath)
        {
            table = null;
            tablePath = null;

            string[] guids = AssetDatabase.FindAssets("t:JpViTranslationTableSO", new[] { "Assets" });
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[UI Migration] Missing translation table asset. Create one via Create > DreamKnight > Localization > JP-VI Translation Table.");
                return false;
            }

            tablePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            table = AssetDatabase.LoadAssetAtPath<JpViTranslationTableSO>(tablePath);
            if (table == null)
            {
                Debug.LogWarning("[UI Migration] Could not load JP-VI translation table asset.");
                return false;
            }

            return true;
        }

        private static string TranslateTextWithTable(string src, JpViTranslationTableSO table, out bool mapped)
        {
            mapped = false;
            if (string.IsNullOrEmpty(src) || table == null || table.Entries == null || table.Entries.Count == 0)
                return src;

            string normalizedSrc = NormalizeForExactMatch(src);

            for (int i = 0; i < table.Entries.Count; i++)
            {
                JpViTranslationTableSO.Entry e = table.Entries[i];
                if (e == null || !e.enabled || string.IsNullOrEmpty(e.japanese))
                    continue;

                if (string.Equals(src, e.japanese, StringComparison.Ordinal)
                    || string.Equals(normalizedSrc, NormalizeForExactMatch(e.japanese), StringComparison.Ordinal))
                {
                    mapped = true;
                    return e.vietnamese ?? string.Empty;
                }
            }

            if (!table.UseContainsReplacement)
                return src;

            string result = src;
            for (int i = 0; i < table.Entries.Count; i++)
            {
                JpViTranslationTableSO.Entry e = table.Entries[i];
                if (e == null || !e.enabled || string.IsNullOrEmpty(e.japanese))
                    continue;

                if (result.IndexOf(e.japanese, StringComparison.Ordinal) >= 0)
                {
                    result = result.Replace(e.japanese, e.vietnamese ?? string.Empty);
                    mapped = true;
                }
            }

            return result;
        }

        private static bool ContainsJapanese(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= '\u3040' && c <= '\u30FF')
                    || (c >= '\u31F0' && c <= '\u31FF')
                    || (c >= '\u4E00' && c <= '\u9FFF')
                    || (c >= '\uFF66' && c <= '\uFF9D'))
                    return true;
            }

            return false;
        }

        private static string NormalizeForExactMatch(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s
                .Replace("\r\n", "\n")
                .Replace('\u3000', ' ')
                .Trim();
        }

        private static bool HasJapaneseEntry(JpViTranslationTableSO table, string japanese)
        {
            if (table == null || table.Entries == null || string.IsNullOrEmpty(japanese))
                return false;

            string normalized = NormalizeForExactMatch(japanese);
            for (int i = 0; i < table.Entries.Count; i++)
            {
                JpViTranslationTableSO.Entry e = table.Entries[i];
                if (e == null || string.IsNullOrEmpty(e.japanese))
                    continue;

                if (string.Equals(normalized, NormalizeForExactMatch(e.japanese), StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        [Serializable]
        private sealed class LibreTranslateResponse
        {
            public string translatedText;
        }

        [Serializable]
        private sealed class MyMemoryResponseData
        {
            public string translatedText;
        }

        [Serializable]
        private sealed class MyMemoryResponse
        {
            public MyMemoryResponseData responseData;
        }

        private static bool TryTranslateOnline(string japaneseText, JpViTranslationTableSO table, out string vietnamese, out string error)
        {
            vietnamese = string.Empty;
            error = string.Empty;

            string endpoint = table.TranslateEndpoint?.Trim();
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                error = "Empty endpoint.";
                return false;
            }

            var endpoints = new List<string>(4) { endpoint };
            if (table.UseFallbackPublicEndpoints)
            {
                AddUniqueEndpoint(endpoints, "https://libretranslate.com/translate");
                AddUniqueEndpoint(endpoints, "https://translate.argosopentech.com/translate");
                AddUniqueEndpoint(endpoints, "https://api.mymemory.translated.net/get");
            }

            var errors = new StringBuilder();
            for (int i = 0; i < endpoints.Count; i++)
            {
                string ep = endpoints[i];
                bool ok;
                string epError;

                if (ep.IndexOf("mymemory", StringComparison.OrdinalIgnoreCase) >= 0)
                    ok = TryTranslateWithMyMemory(ep, japaneseText, out vietnamese, out epError);
                else
                    ok = TryTranslateWithLibreStyle(ep, table.ApiKey, table.RequestTimeoutSeconds, japaneseText, out vietnamese, out epError);

                if (ok)
                    return true;

                if (errors.Length > 0)
                    errors.Append(" | ");
                errors.Append(ep).Append(": ").Append(epError);
            }

            error = errors.ToString();
            return false;
        }

        private static void AddUniqueEndpoint(List<string> endpoints, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return;

            for (int i = 0; i < endpoints.Count; i++)
            {
                if (string.Equals(endpoints[i], endpoint, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            endpoints.Add(endpoint);
        }

        private static bool TryTranslateWithLibreStyle(string endpoint, string apiKey, int timeoutSeconds, string japaneseText, out string vietnamese, out string error)
        {
            vietnamese = string.Empty;
            error = string.Empty;

            WWWForm form = new WWWForm();
            form.AddField("q", japaneseText);
            form.AddField("source", "ja");
            form.AddField("target", "vi");
            form.AddField("format", "text");
            if (!string.IsNullOrWhiteSpace(apiKey))
                form.AddField("api_key", apiKey);

            using (UnityWebRequest req = UnityWebRequest.Post(endpoint, form))
            {
                req.timeout = Mathf.Clamp(timeoutSeconds, 5, 120);
                UnityWebRequestAsyncOperation op = req.SendWebRequest();
                while (!op.isDone)
                {
                }

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    error = req.error;
                    return false;
                }

                string json = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "Empty response.";
                    return false;
                }

                LibreTranslateResponse parsed = JsonUtility.FromJson<LibreTranslateResponse>(json);
                if (parsed == null || string.IsNullOrWhiteSpace(parsed.translatedText))
                {
                    error = "Response missing translatedText. Raw=" + TrimForLog(json, 180);
                    return false;
                }

                vietnamese = parsed.translatedText;
                return true;
            }
        }

        private static bool TryTranslateWithMyMemory(string endpoint, string japaneseText, out string vietnamese, out string error)
        {
            vietnamese = string.Empty;
            error = string.Empty;

            string q = UnityWebRequest.EscapeURL(japaneseText);
            string url = endpoint;
            if (url.IndexOf('?') >= 0)
                url += "&q=" + q + "&langpair=ja|vi";
            else
                url += "?q=" + q + "&langpair=ja|vi";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 20;
                UnityWebRequestAsyncOperation op = req.SendWebRequest();
                while (!op.isDone)
                {
                }

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    error = req.error;
                    return false;
                }

                string json = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "Empty response.";
                    return false;
                }

                MyMemoryResponse parsed = JsonUtility.FromJson<MyMemoryResponse>(json);
                if (parsed == null || parsed.responseData == null || string.IsNullOrWhiteSpace(parsed.responseData.translatedText))
                {
                    error = "Response missing responseData.translatedText. Raw=" + TrimForLog(json, 180);
                    return false;
                }

                vietnamese = parsed.responseData.translatedText;
                return true;
            }
        }

        private static string TrimForLog(string s, int maxLength)
        {
            if (string.IsNullOrEmpty(s) || maxLength <= 0 || s.Length <= maxLength)
                return s ?? string.Empty;

            return s.Substring(0, maxLength) + "...";
        }
    }
}
