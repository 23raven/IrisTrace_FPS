using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IrisTrace.Runtime.Enemies;

namespace IrisTrace.Editor.Generators
{
    /// <summary>Creates and places a basic target prefab for validating hitscan damage.</summary>
    public static class TrainingTargetPrefabGenerator
    {
        private const string GeneratedRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Enemies";

        public static GameObject Generate()
        {
            string targetFolder = $"{GeneratedRoot}/TrainingTarget";
            EnsureAssetFolder(targetFolder);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/TrainingTarget.prefab");

            GameObject buildRoot = new GameObject("TrainingTarget");
            GameObject prefabAsset;
            try
            {
                TrainingTarget target = buildRoot.AddComponent<TrainingTarget>();
                target.Configure(100f);

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(buildRoot.transform, false);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                body.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(buildRoot.transform, false);
                head.transform.localPosition = new Vector3(0f, 2.08f, 0f);
                head.transform.localScale = Vector3.one * 0.62f;

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(buildRoot, prefabPath, out bool success);
                if (!success || prefabAsset == null)
                    throw new InvalidOperationException("Unity could not save the training target prefab.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buildRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = SceneManager.GetActiveScene();
            int existingCount = 0;
            TrainingTarget[] existingTargets = Resources.FindObjectsOfTypeAll<TrainingTarget>();
            foreach (TrainingTarget existing in existingTargets)
                if (existing != null && existing.gameObject.scene == scene)
                    existingCount++;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Prefab was saved, but Unity could not create a scene instance.");

            instance.name = existingCount == 0 ? "TrainingTarget" : $"TrainingTarget_{existingCount + 1}";
            instance.transform.position = new Vector3(existingCount * 1.5f, 0f, 8f);
            instance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(instance, "Create Training Target");
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            return instance;
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
