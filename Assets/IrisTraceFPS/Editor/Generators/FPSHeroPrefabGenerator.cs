using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IrisTrace.Runtime.Characters;
using IrisTrace.Runtime.Weapons;
using IrisTrace.Runtime.Input;
using IrisTrace.Runtime.Abilities;
using IrisTrace.Runtime.UI;
using IrisTrace.Runtime.Audio;

namespace IrisTrace.Editor.Generators
{
    /// <summary>Creates a configured FPS hero prefab and places an instance in the active scene.</summary>
    public static class FPSHeroPrefabGenerator
    {
        private const string GeneratedRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Characters";

        public static GameObject Generate(CharacterDefinition definition, WeaponDefinition weaponDefinition, string safeName)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (weaponDefinition == null)
                throw new ArgumentNullException(nameof(weaponDefinition));
            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "New FPS Hero";

            string characterFolder = $"{GeneratedRoot}/{safeName}";
            EnsureAssetFolder(characterFolder);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{characterFolder}/{safeName}.prefab");

            GameObject buildRoot = new GameObject(safeName);
            GameObject prefabAsset;
            try
            {
                CharacterController characterController = buildRoot.AddComponent<CharacterController>();
                characterController.height = 1.8f;
                characterController.radius = 0.32f;
                characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.08f;
                characterController.minMoveDistance = 0f;

                CharacterHealth health = buildRoot.AddComponent<CharacterHealth>();
                health.Initialize(definition.MaxHealth);

                GameObject cameraObject = new GameObject("PlayerCamera");
                cameraObject.transform.SetParent(buildRoot.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
                cameraObject.tag = "MainCamera";

                Camera playerCamera = cameraObject.AddComponent<Camera>();
                playerCamera.fieldOfView = definition.CameraFieldOfView;
                playerCamera.nearClipPlane = 0.03f;
                playerCamera.farClipPlane = 1000f;
                cameraObject.AddComponent<AudioListener>();

                GameObject weaponMount = new GameObject("WeaponMount");
                weaponMount.transform.SetParent(cameraObject.transform, false);
                weaponMount.transform.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
                weaponMount.transform.localRotation = Quaternion.identity;

                AudioSource weaponAudioSource = weaponMount.AddComponent<AudioSource>();
                weaponAudioSource.playOnAwake = false;
                weaponAudioSource.loop = false;
                weaponAudioSource.spatialBlend = 0f;
                HitscanWeapon weapon = weaponMount.AddComponent<HitscanWeapon>();
                weapon.Configure(weaponDefinition, playerCamera, buildRoot.transform);
                WeaponAudioController weaponAudio = weaponMount.AddComponent<WeaponAudioController>();
                weaponAudio.Configure(weaponDefinition, weapon, weaponAudioSource);

                IrisTraceInputReader inputReader = buildRoot.AddComponent<IrisTraceInputReader>();
                inputReader.Configure(
                    definition.MoveForwardBinding,
                    definition.MoveBackwardBinding,
                    definition.MoveLeftBinding,
                    definition.MoveRightBinding,
                    definition.JumpBinding,
                    definition.FireBinding,
                    definition.ReloadBinding,
                    definition.Ability1Binding,
                    definition.Ability2Binding,
                    definition.CrouchBinding);

                WeaponInputHandler weaponInput = buildRoot.AddComponent<WeaponInputHandler>();
                weaponInput.Configure(weapon, inputReader);

                MonoBehaviour ability1 = CreateAbility(buildRoot, definition.Ability1Type,
                    definition.Ability1Value, definition.Ability1Cooldown);
                MonoBehaviour ability2 = CreateAbility(buildRoot, definition.Ability2Type,
                    definition.Ability2Value, definition.Ability2Cooldown);
                AbilityInputHandler abilityInput = buildRoot.AddComponent<AbilityInputHandler>();
                abilityInput.Configure(inputReader, ability1, ability2,
                    definition.Ability1Binding, definition.Ability2Binding);

                // The HUD reads telemetry interfaces and does not depend on concrete weapon classes.
                buildRoot.AddComponent<FPSRuntimeHUD>();

                FPSHeroController heroController = buildRoot.AddComponent<FPSHeroController>();
                heroController.Configure(definition, characterController, playerCamera, inputReader);

                AudioSource characterAudioSource = buildRoot.AddComponent<AudioSource>();
                characterAudioSource.playOnAwake = false;
                characterAudioSource.loop = false;
                characterAudioSource.spatialBlend = 0f;
                CharacterAudioController characterAudio = buildRoot.AddComponent<CharacterAudioController>();
                characterAudio.Configure(definition, heroController, characterAudioSource);

                prefabAsset = PrefabUtility.SaveAsPrefabAsset(buildRoot, prefabPath, out bool success);
                if (!success || prefabAsset == null)
                    throw new InvalidOperationException("Unity could not save the generated prefab. Check the Console for details.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(buildRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene scene = SceneManager.GetActiveScene();
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Prefab was saved, but Unity could not create a scene instance.");

            instance.name = definition.DisplayName;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(instance, "Generate FPS Hero");

            Camera generatedCamera = instance.GetComponentInChildren<Camera>(true);
            DisableOtherMainCameras(scene, generatedCamera);
            DisableOtherAudioListeners(scene, instance.GetComponentInChildren<AudioListener>(true));

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            return instance;
        }

        private static MonoBehaviour CreateAbility(GameObject root, AbilityKind kind, float value, float cooldown)
        {
            switch (kind)
            {
                case AbilityKind.None:
                    return null;
                case AbilityKind.Dash:
                    DashAbility dash = root.AddComponent<DashAbility>();
                    dash.Configure(value, cooldown);
                    return dash;
                case AbilityKind.Heal:
                    HealAbility heal = root.AddComponent<HealAbility>();
                    heal.Configure(value, cooldown);
                    return heal;
                case AbilityKind.Sprint:
                    return root.AddComponent<SprintAbility>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported built-in ability type.");
            }
        }

        private static void DisableOtherMainCameras(Scene scene, Camera generatedCamera)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera == null || camera == generatedCamera || !camera.enabled || camera.gameObject.scene != scene)
                    continue;
                if (!camera.CompareTag("MainCamera"))
                    continue;

                Undo.RecordObject(camera, "Disable Previous Main Camera");
                camera.enabled = false;
                EditorUtility.SetDirty(camera);
            }
        }

        private static void DisableOtherAudioListeners(Scene scene, AudioListener generatedListener)
        {
            AudioListener[] listeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                if (listener == null || listener == generatedListener || !listener.enabled || listener.gameObject.scene != scene)
                    continue;

                Undo.RecordObject(listener, "Disable Previous Audio Listener");
                listener.enabled = false;
                EditorUtility.SetDirty(listener);
            }
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
