using System;
using System.Collections.Generic;
using System.Reflection;
using Mv;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MvEnemyBase), true)]
[CanEditMultipleObjects]
public class MvEnemyBaseEditor : Editor
{
    private const string BaseFoldoutPrefix = "MvEnemyBaseEditor.BaseFoldout.";
    private static readonly Dictionary<Type, Dictionary<string, Type>> FieldOwnerCache = new Dictionary<Type, Dictionary<string, Type>>();

    private bool baseFoldout;

    private void OnEnable()
    {
        baseFoldout = SessionState.GetBool(GetBaseFoldoutKey(), false);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        List<string> enemyFields = new List<string>();
        List<string> baseFields = new List<string>();
        CollectTopLevelFields(enemyFields, baseFields);

        DrawProperties(enemyFields);

        if (baseFields.Count > 0)
        {
            EditorGUILayout.Space(6f);
            baseFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(baseFoldout, "Enemy Base");
            SessionState.SetBool(GetBaseFoldoutKey(), baseFoldout);

            if (baseFoldout)
            {
                EditorGUI.indentLevel++;
                DrawProperties(baseFields);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        SerializedProperty script = serializedObject.FindProperty("m_Script");
        if (script == null)
            return;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(script);
        }
    }

    private void CollectTopLevelFields(List<string> enemyFields, List<string> baseFields)
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        Type targetType = serializedObject.targetObject != null ? serializedObject.targetObject.GetType() : null;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
                continue;

            if (iterator.depth != 0)
                continue;

            Type owner = ResolveFieldOwner(targetType, iterator.propertyPath);
            if (owner == typeof(MvEnemyBase))
                baseFields.Add(iterator.propertyPath);
            else
                enemyFields.Add(iterator.propertyPath);
        }
    }

    private void DrawProperties(List<string> propertyPaths)
    {
        for (int i = 0; i < propertyPaths.Count; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPaths[i]);
            if (property != null)
                EditorGUILayout.PropertyField(property, true);
        }
    }

    private static Type ResolveFieldOwner(Type targetType, string propertyPath)
    {
        if (targetType == null || string.IsNullOrEmpty(propertyPath))
            return null;

        string fieldName = propertyPath;
        int dotIndex = fieldName.IndexOf('.');
        if (dotIndex >= 0)
            fieldName = fieldName.Substring(0, dotIndex);

        Dictionary<string, Type> ownerMap = GetFieldOwnerMap(targetType);
        return ownerMap.TryGetValue(fieldName, out Type owner) ? owner : null;
    }

    private static Dictionary<string, Type> GetFieldOwnerMap(Type targetType)
    {
        if (FieldOwnerCache.TryGetValue(targetType, out Dictionary<string, Type> cached))
            return cached;

        Dictionary<string, Type> result = new Dictionary<string, Type>(StringComparer.Ordinal);
        Type type = targetType;
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fields = type.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null)
                    continue;

                if (!result.ContainsKey(field.Name))
                    result[field.Name] = field.DeclaringType;
            }

            type = type.BaseType;
        }

        FieldOwnerCache[targetType] = result;
        return result;
    }

    private string GetBaseFoldoutKey()
    {
        Type type = serializedObject.targetObject != null ? serializedObject.targetObject.GetType() : typeof(MvEnemyBase);
        return BaseFoldoutPrefix + type.FullName;
    }
}
