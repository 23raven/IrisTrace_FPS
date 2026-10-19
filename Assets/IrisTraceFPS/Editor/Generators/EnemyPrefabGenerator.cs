using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using IrisTrace.Runtime.Enemies;

namespace IrisTrace.Editor.Generators
{
    /// <summary>Builds and places an enemy prefab from an EnemyDefinition asset.</summary>
    public static class EnemyPrefabGenerator
    {
        private const string GeneratedRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Enemies";

        public static GameObject Generate(EnemyDefinition definition, string safeName)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "New Enemy";

            string enemyFolder = $"{GeneratedRoot}/{safeName}";
            EnsureAssetFolder(enemyFolder);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{enemyFolder}/{safeName}.prefab");

            GameObject buildRoot = new GameObject(safeName);
            GameObject prefabAsset;
            try
            {
                EnemyHealth health = buildRoot.AddComponent<EnemyHealth>();
                health.Configure(definition.MaxHealth);

                if (definition.Behavior == EnemyBehaviorMode.BasicAI)
                {
                    NavMeshAgent agent = buildRoot.AddComponent<NavMeshAgent>();
                    agent.enabled = false;
                    agent.speed = definition.MoveSpeed;
                    agent.angularSpeed = 540f;
                    agent.acceleration = 12f;
                    agent.radius = 0.35f;
                    agent.height = 2f;
                    agent.stoppingDistance = Mathf.Max(0.2f, definition.AttackRange * 0.75f);

                    BasicEnemyAI brain = buildRoot.AddComponent<BasicEnemyAI>();
                    brain.Configure(definition, health, agent);
                }

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
                    throw new InvalidOperationException("Unity could not save the enemy prefab.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buildRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = SceneManager.GetActiveScene();
            int existingCount = CountExistingEnemies(scene);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Prefab was saved, but Unity could not create a scene instance.");

            instance.transform.position = new Vector3(existingCount * 1.7f, 0f, 8f);
            instance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(instance, "Create IrisTrace Enemy");
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            return instance;
        }

        private static int CountExistingEnemies(Scene scene)
        {
            int count = 0;
            EnemyHealth[] existingEnemies = Resources.FindObjectsOfTypeAll<EnemyHealth>();
            foreach (EnemyHealth enemy in existingEnemies)
                if (enemy != null && enemy.gameObject.scene == scene)
                    count++;

            TrainingTarget[] legacyTargets = Resources.FindObjectsOfTypeAll<TrainingTarget>();
            foreach (TrainingTarget target in legacyTargets)
                if (target != null && target.gameObject.scene == scene)
                    count++;

            return count;
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
