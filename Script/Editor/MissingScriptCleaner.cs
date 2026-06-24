using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DreamKnight.EditorTools
{
    public static class MissingScriptCleaner
    {
        private const string UndoName = "Remove Missing Scripts";

        [MenuItem("Tools/DreamKnight/Cleanup/Remove Missing Scripts In Active Scene")]
        private static void RemoveMissingScriptsInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Missing Script Cleaner", "No active loaded scene was found.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Remove Missing Scripts",
                    $"Remove all missing script components in scene '{activeScene.name}'?",
                    "Remove",
                    "Cancel"))
            {
                return;
            }

            CleanupResult result = CleanScene(activeScene);
            ShowResult($"Scene '{activeScene.name}'", result);
        }

        [MenuItem("Tools/DreamKnight/Cleanup/Remove Missing Scripts In Selected Objects")]
        private static void RemoveMissingScriptsInSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Missing Script Cleaner", "Select one or more scene objects first.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Remove Missing Scripts",
                    "Remove missing script components from selected scene objects and their children?",
                    "Remove",
                    "Cancel"))
            {
                return;
            }

            CleanupResult result = CleanSelectedObjects(selectedObjects);
            ShowResult("Selected Objects", result);
        }

        private static CleanupResult CleanScene(Scene scene)
        {
            CleanupResult result = new CleanupResult();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                CleanHierarchy(root, result);
            }

            if (result.RemovedCount > 0)
                EditorSceneManager.MarkSceneDirty(scene);

            return result;
        }

        private static CleanupResult CleanSelectedObjects(IReadOnlyList<GameObject> selectedObjects)
        {
            CleanupResult result = new CleanupResult();
            HashSet<GameObject> visitedObjects = new HashSet<GameObject>();
            HashSet<Scene> dirtyScenes = new HashSet<Scene>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                CleanHierarchy(selectedObject, result, visitedObjects, dirtyScenes);
            }

            foreach (Scene scene in dirtyScenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            return result;
        }

        private static void CleanHierarchy(GameObject root, CleanupResult result)
        {
            CleanHierarchy(root, result, null, null);
        }

        private static void CleanHierarchy(
            GameObject root,
            CleanupResult result,
            HashSet<GameObject> visitedObjects,
            HashSet<Scene> dirtyScenes)
        {
            if (root == null || !root.scene.IsValid())
                return;

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                GameObject target = child.gameObject;
                if (visitedObjects != null && !visitedObjects.Add(target))
                    continue;

                result.ScannedCount++;

                Undo.RegisterCompleteObjectUndo(target, UndoName);
                int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                if (removedCount <= 0)
                    continue;

                result.RemovedCount += removedCount;
                EditorUtility.SetDirty(target);

                if (dirtyScenes != null)
                    dirtyScenes.Add(target.scene);
            }
        }

        private static void ShowResult(string targetName, CleanupResult result)
        {
            string message = $"{targetName}\nScanned: {result.ScannedCount} GameObjects\nRemoved: {result.RemovedCount} missing script component(s)";
            Debug.Log($"[MissingScriptCleaner] {message}");
            EditorUtility.DisplayDialog("Missing Script Cleaner", message, "OK");
        }

        private struct CleanupResult
        {
            public int ScannedCount;
            public int RemovedCount;
        }
    }
}
