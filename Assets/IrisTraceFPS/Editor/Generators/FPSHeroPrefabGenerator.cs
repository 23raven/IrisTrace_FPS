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

                // Procedural placeholder hands are intentionally disabled. Custom hands remain optional.
                if (definition.FirstPersonArmsPrefab != null)
                {
                    Transform armsMount = new GameObject("ArmsMount").transform;
                    armsMount.SetParent(cameraObject.transform, false);
                    armsMount.localPosition = definition.FirstPersonArmsLocalPosition;
                    armsMount.localRotation = Quaternion.Euler(definition.FirstPersonArmsLocalEuler);
                    armsMount.localScale = definition.FirstPersonArmsLocalScale;

                    GameObject armsInstance = PrefabUtility.InstantiatePrefab(definition.FirstPersonArmsPrefab) as GameObject;
                    if (armsInstance == null)
                        armsInstance = UnityEngine.Object.Instantiate(definition.FirstPersonArmsPrefab);
                    armsInstance.name = definition.FirstPersonArmsPrefab.name;
                    armsInstance.transform.SetParent(armsMount, false);
                    armsInstance.transform.localPosition = Vector3.zero;
                    armsInstance.transform.localRotation = Quaternion.identity;
                    armsInstance.transform.localScale = Vector3.one;
                }

                // The aim pivot is animated independently from WeaponMount. This prevents ADS offsets
                // from fighting recoil/sway/bobbing, which remain on the child WeaponMount.
                GameObject weaponAimPivot = new GameObject("WeaponAimPivot");
                weaponAimPivot.transform.SetParent(cameraObject.transform, false);
                weaponAimPivot.transform.localPosition = Vector3.zero;
                weaponAimPivot.transform.localRotation = Quaternion.identity;
                weaponAimPivot.transform.localScale = Vector3.one;

                GameObject weaponMount = new GameObject("WeaponMount");
                weaponMount.transform.SetParent(weaponAimPivot.transform, false);
                weaponMount.transform.localPosition = weaponDefinition.ViewModelMountLocalPosition;
                weaponMount.transform.localRotation = Quaternion.Euler(weaponDefinition.ViewModelMountLocalEuler);
                weaponMount.transform.localScale = weaponDefinition.ViewModelMountLocalScale;

                GameObject weaponVisualRoot = new GameObject("WeaponVisualRoot");
                weaponVisualRoot.transform.SetParent(weaponMount.transform, false);
                weaponVisualRoot.transform.localPosition = Vector3.zero;
                weaponVisualRoot.transform.localRotation = Quaternion.identity;
                weaponVisualRoot.transform.localScale = Vector3.one;

                if (weaponDefinition.ViewModelPrefab != null)
                {
                    GameObject weaponVisual = PrefabUtility.InstantiatePrefab(weaponDefinition.ViewModelPrefab) as GameObject;
                    if (weaponVisual == null)
                        weaponVisual = UnityEngine.Object.Instantiate(weaponDefinition.ViewModelPrefab);
                    weaponVisual.name = weaponDefinition.ViewModelPrefab.name;
                    weaponVisual.transform.SetParent(weaponVisualRoot.transform, false);
                    weaponVisual.transform.localPosition = weaponDefinition.ViewModelLocalPosition;
                    weaponVisual.transform.localRotation = Quaternion.Euler(weaponDefinition.ViewModelLocalEuler);
                    weaponVisual.transform.localScale = weaponDefinition.ViewModelLocalScale;
                }
                else
                {
                    EnsurePlaceholderMaterials();
                    GameObject placeholderWeapon = BuildPlaceholderWeapon(weaponVisualRoot.transform);
                    placeholderWeapon.transform.localPosition = weaponDefinition.ViewModelLocalPosition;
                    placeholderWeapon.transform.localRotation = Quaternion.Euler(weaponDefinition.ViewModelLocalEuler);
                    placeholderWeapon.transform.localScale = weaponDefinition.ViewModelLocalScale;
                }

                AudioSource weaponAudioSource = weaponMount.AddComponent<AudioSource>();
                weaponAudioSource.playOnAwake = false;
                weaponAudioSource.loop = false;
                weaponAudioSource.spatialBlend = 0f;
                HitscanWeapon weapon = weaponMount.AddComponent<HitscanWeapon>();
                weapon.Configure(weaponDefinition, playerCamera, buildRoot.transform);

                // Procedural recoil/reload feedback animates the weapon mount only; aiming remains camera-driven.
                WeaponViewmodelMotion viewmodelMotion = weaponMount.AddComponent<WeaponViewmodelMotion>();
                viewmodelMotion.Configure(weaponDefinition, weapon);

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

                WeaponAimController aimController = buildRoot.AddComponent<WeaponAimController>();
                aimController.Configure(playerCamera, weaponAimPivot.transform, weaponVisualRoot,
                    weaponDefinition, inputReader);

                MonoBehaviour ability1 = CreateAbility(buildRoot, definition.Ability1Type,
                    definition.Ability1Value, definition.Ability1Cooldown);
                MonoBehaviour ability2 = CreateAbility(buildRoot, definition.Ability2Type,
                    definition.Ability2Value, definition.Ability2Cooldown);
                AbilityInputHandler abilityInput = buildRoot.AddComponent<AbilityInputHandler>();
                abilityInput.Configure(inputReader, ability1, ability2,
                    definition.Ability1Binding, definition.Ability2Binding);

                // Visual ability feedback is optional and can be customized on the generated prefab.
                AbilityActivationFeedback abilityFeedback = buildRoot.AddComponent<AbilityActivationFeedback>();
                abilityFeedback.Configure(abilityInput);

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

        private const string GeneratedMaterialsRoot = "Assets/IrisTraceFPS_Generated/Materials";
        private static Material placeholderDarkMaterial;
        private static Material placeholderAccentMaterial;
        private static Material placeholderSleeveMaterial;
        private static Material placeholderGloveMaterial;

        private static void EnsurePlaceholderMaterials()
        {
            EnsureAssetFolder(GeneratedMaterialsRoot);
            placeholderDarkMaterial = LoadOrCreateMaterial("IrisTrace_Viewmodel_Dark", new Color(0.12f, 0.13f, 0.15f, 1f));
            placeholderAccentMaterial = LoadOrCreateMaterial("IrisTrace_Viewmodel_Accent", new Color(0.45f, 0.69f, 0.11f, 1f));
            placeholderSleeveMaterial = LoadOrCreateMaterial("IrisTrace_Viewmodel_Sleeve", new Color(0.20f, 0.22f, 0.25f, 1f));
            placeholderGloveMaterial = LoadOrCreateMaterial("IrisTrace_Viewmodel_Glove", new Color(0.07f, 0.08f, 0.09f, 1f));
        }

        private static Material LoadOrCreateMaterial(string materialName, Color color)
        {
            string path = GeneratedMaterialsRoot + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
                return null;

            material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject BuildPlaceholderWeapon(Transform parent)
        {
            GameObject root = new GameObject("DefaultWeaponViewmodel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            CreateVisualPrimitive(PrimitiveType.Cube, "WeaponBody", root.transform,
                new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(0.14f, 0.10f, 0.34f), placeholderDarkMaterial);
            CreateVisualPrimitive(PrimitiveType.Cylinder, "Barrel", root.transform,
                new Vector3(0f, 0f, 0.23f), new Vector3(90f, 0f, 0f), new Vector3(0.025f, 0.14f, 0.025f), placeholderDarkMaterial);
            CreateVisualPrimitive(PrimitiveType.Cube, "Stock", root.transform,
                new Vector3(0f, -0.005f, -0.21f), new Vector3(0f, 0f, 0f), new Vector3(0.15f, 0.085f, 0.12f), placeholderSleeveMaterial);
            CreateVisualPrimitive(PrimitiveType.Cube, "Grip", root.transform,
                new Vector3(0f, -0.085f, -0.035f), new Vector3(12f, 0f, 0f), new Vector3(0.065f, 0.13f, 0.075f), placeholderGloveMaterial);
            CreateVisualPrimitive(PrimitiveType.Cube, "TopRail", root.transform,
                new Vector3(0f, 0.064f, 0.015f), Vector3.zero, new Vector3(0.05f, 0.022f, 0.15f), placeholderAccentMaterial);
            CreateVisualPrimitive(PrimitiveType.Cube, "Sight", root.transform,
                new Vector3(0f, 0.086f, 0.085f), Vector3.zero, new Vector3(0.025f, 0.02f, 0.025f), placeholderDarkMaterial);
            return root;
        }

        private static void CreateVisualPrimitive(PrimitiveType type, string objectName, Transform parent,
            Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = objectName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.Euler(localEuler);
            visual.transform.localScale = localScale;
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
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
