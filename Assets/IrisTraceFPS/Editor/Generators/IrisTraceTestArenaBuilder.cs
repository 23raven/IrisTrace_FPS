using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IrisTrace.Runtime.Characters;
using IrisTrace.Runtime.Enemies;
using IrisTrace.Runtime.Weapons;

namespace IrisTrace.Editor.Generators
{
    /// <summary>Creates a repeatable sample arena for checking the generated FPS workflow.</summary>
    public static class IrisTraceTestArenaBuilder
    {
        private const string SceneFolder = "Assets/IrisTraceFPS_Generated/Scenes";
        private const string ScenePath = SceneFolder + "/IrisTrace_TestArena.unity";
        private const string CharacterFolder = "Assets/IrisTraceFPS_Generated/Configurations/Characters";
        private const string WeaponFolder = "Assets/IrisTraceFPS_Generated/Configurations/Weapons";
        private const string EnemyFolder = "Assets/IrisTraceFPS_Generated/Configurations/Enemies";
        private const string CharacterPath = CharacterFolder + "/IrisTrace_TestHero.asset";
        private const string WeaponPath = WeaponFolder + "/IrisTrace_TestWeapon.asset";
        private const string EnemyPath = EnemyFolder + "/IrisTrace_TestBot.asset";

        [MenuItem("Window/IrisTrace FPS/Create Test Arena")]
        public static void CreateTestArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureAssetFolder(SceneFolder);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Replace Test Arena?",
                    "IrisTrace_TestArena.unity already exists. Replace it with a fresh test scene?",
                    "Replace", "Cancel"))
                return;

            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildEnvironment();

                CharacterDefinition character = LoadOrCreateTestCharacter();
                WeaponDefinition weapon = LoadOrCreateTestWeapon(character);
                EnemyDefinition enemy = LoadOrCreateTestEnemy();

                GameObject hero = FPSHeroPrefabGenerator.Generate(character, weapon, "IrisTrace_TestHero");
                hero.transform.position = Vector3.zero;
                hero.transform.rotation = Quaternion.identity;

                GameObject target = TrainingTargetPrefabGenerator.Generate();
                target.name = "Training Target";
                target.transform.position = new Vector3(0f, 0f, 8f);

                GameObject bot = EnemyPrefabGenerator.Generate(enemy, "IrisTrace_TestBot");
                bot.transform.position = new Vector3(5f, 0f, 11f);
                bot.transform.rotation = Quaternion.identity;

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Unity could not save the test arena scene.");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeGameObject = hero;
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
                EditorUtility.DisplayDialog("Test Arena Created",
                    "The test scene is saved at:\n" + ScenePath +
                    "\n\nPress Play to test movement, shooting, damage, abilities and the basic enemy.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Test Arena Failed",
                    "The scene could not be built completely. See the Console for details.", "OK");
            }
        }

        private static void BuildEnvironment()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Arena Floor";
            floor.transform.position = new Vector3(0f, -0.02f, 8f);
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            CreateWall("West Wall", new Vector3(-15f, 1.5f, 8f), new Vector3(0.5f, 3f, 30f));
            CreateWall("East Wall", new Vector3(15f, 1.5f, 8f), new Vector3(0.5f, 3f, 30f));
            CreateWall("North Wall", new Vector3(0f, 1.5f, 23f), new Vector3(30f, 3f, 0.5f));

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CreateWall("Left Cover", new Vector3(-5.5f, 0.75f, 5.5f), new Vector3(2.4f, 1.5f, 1f));
            CreateWall("Right Cover", new Vector3(5.5f, 0.75f, 5.5f), new Vector3(2.4f, 1.5f, 1f));
        }

        private static void CreateWall(string objectName, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = objectName;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static CharacterDefinition LoadOrCreateTestCharacter()
        {
            EnsureAssetFolder(CharacterFolder);
            CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath);
            if (definition != null)
            {
                if (string.IsNullOrWhiteSpace(definition.PrimaryWeaponId))
                {
                    SerializedObject existingSerialized = new SerializedObject(definition);
                    SetString(existingSerialized, "primaryWeaponId", Guid.NewGuid().ToString("N"));
                    existingSerialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(definition);
                }
                return definition;
            }

            string weaponId = Guid.NewGuid().ToString("N");
            definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            SetString(serialized, "id", Guid.NewGuid().ToString("N"));
            SetString(serialized, "primaryWeaponId", weaponId);
            SetString(serialized, "displayName", "IrisTrace Test Hero");
            SetFloat(serialized, "maxHealth", 100f);
            SetFloat(serialized, "walkSpeed", 5f);
            SetFloat(serialized, "sprintSpeed", 8f);
            SetFloat(serialized, "jumpHeight", 1.5f);
            SetFloat(serialized, "mouseSensitivity", 1f);
            SetFloat(serialized, "cameraFieldOfView", 90f);
            SetString(serialized, "moveForwardBinding", "<Keyboard>/w");
            SetString(serialized, "moveBackwardBinding", "<Keyboard>/s");
            SetString(serialized, "moveLeftBinding", "<Keyboard>/a");
            SetString(serialized, "moveRightBinding", "<Keyboard>/d");
            SetString(serialized, "jumpBinding", "<Keyboard>/space");
            SetString(serialized, "sprintBinding", "<Keyboard>/leftShift");
            SetString(serialized, "fireBinding", "<Mouse>/leftButton");
            SetString(serialized, "reloadBinding", "<Keyboard>/r");
            SetInt(serialized, "ability1Type", (int)AbilityKind.Dash);
            SetString(serialized, "ability1Binding", "<Keyboard>/q");
            SetFloat(serialized, "ability1Cooldown", 5f);
            SetFloat(serialized, "ability1Value", 6f);
            SetInt(serialized, "ability2Type", (int)AbilityKind.Heal);
            SetString(serialized, "ability2Binding", "<Keyboard>/e");
            SetFloat(serialized, "ability2Cooldown", 8f);
            SetFloat(serialized, "ability2Value", 25f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, CharacterPath);
            return definition;
        }

        private static WeaponDefinition LoadOrCreateTestWeapon(CharacterDefinition character)
        {
            EnsureAssetFolder(WeaponFolder);
            WeaponDefinition definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                SerializedObject serialized = new SerializedObject(definition);
                SetString(serialized, "id", character.PrimaryWeaponId);
                SetString(serialized, "displayName", "IrisTrace Test Weapon");
                SetFloat(serialized, "damage", 25f);
                SetFloat(serialized, "shotsPerSecond", 6f);
                SetInt(serialized, "magazineSize", 12);
                SetInt(serialized, "reserveAmmo", 48);
                SetFloat(serialized, "reloadDuration", 1.4f);
                SetFloat(serialized, "range", 100f);
                SetBool(serialized, "automatic", true);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, WeaponPath);
            }
            else if (definition.Id != character.PrimaryWeaponId)
            {
                SerializedObject serialized = new SerializedObject(definition);
                SetString(serialized, "id", character.PrimaryWeaponId);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }

            return definition;
        }

        private static EnemyDefinition LoadOrCreateTestEnemy()
        {
            EnsureAssetFolder(EnemyFolder);
            EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyPath);
            if (definition != null)
                return definition;

            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.Configure(Guid.NewGuid().ToString("N"), "IrisTrace Test Bot", EnemyBehaviorMode.BasicAI,
                100f, 3.5f, 14f, 2.2f, 10f, 1.25f);
            AssetDatabase.CreateAsset(definition, EnemyPath);
            return definition;
        }

        private static void SetString(SerializedObject target, string propertyName, string value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetInt(SerializedObject target, string propertyName, int value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static void SetBool(SerializedObject target, string propertyName, bool value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
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
