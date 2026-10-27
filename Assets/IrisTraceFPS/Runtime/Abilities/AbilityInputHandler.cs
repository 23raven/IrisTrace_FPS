using System;
using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>
    /// Routes abstract input slots to configured abilities. Exposes slot telemetry
    /// through a Core contract so HUD and extensions do not depend on this class.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AbilityInputHandler : MonoBehaviour, IAbilitySlotProvider
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private MonoBehaviour ability1Component;
        [SerializeField] private MonoBehaviour ability2Component;
        [SerializeField] private string ability1Binding = "<Keyboard>/q";
        [SerializeField] private string ability2Binding = "<Keyboard>/e";

        private IAbilityInput input;
        private IAbilityHoldInput holdInput;
        private ICharacterAbility ability1;
        private ICharacterAbility ability2;

        public ICharacterAbility AbilitySlot1 => ability1;
        public ICharacterAbility AbilitySlot2 => ability2;
        public string AbilitySlot1Binding => ability1Binding;
        public string AbilitySlot2Binding => ability2Binding;

        /// <summary>Raised after an ability successfully starts. Slot is 1 or 2.</summary>
        public event Action<int, string> AbilityActivated;

        public void Configure(MonoBehaviour inputBehaviour, MonoBehaviour firstAbility, MonoBehaviour secondAbility)
        {
            Configure(inputBehaviour, firstAbility, secondAbility, "<Keyboard>/q", "<Keyboard>/e");
        }

        public void Configure(MonoBehaviour inputBehaviour, MonoBehaviour firstAbility, MonoBehaviour secondAbility,
            string firstBinding, string secondBinding)
        {
            inputSource = inputBehaviour;
            ability1Component = firstAbility;
            ability2Component = secondAbility;
            ability1Binding = SafeBinding(firstBinding, "<Keyboard>/q");
            ability2Binding = SafeBinding(secondBinding, "<Keyboard>/e");
            ResolveContracts();
        }

        private void Awake() => ResolveContracts();

        private void ResolveContracts()
        {
            input = inputSource as IAbilityInput;
            holdInput = inputSource as IAbilityHoldInput;
            ability1 = ability1Component as ICharacterAbility;
            ability2 = ability2Component as ICharacterAbility;
        }

        private void Update()
        {
            if (input == null)
            {
                ReleaseHeldAbilities();
                return;
            }

            HandleSlot(1, ability1, input.Ability1Pressed, holdInput != null && holdInput.Ability1Held);
            HandleSlot(2, ability2, input.Ability2Pressed, holdInput != null && holdInput.Ability2Held);
        }

        private void HandleSlot(int slot, ICharacterAbility ability, bool pressed, bool held)
        {
            if (ability is IHoldCharacterAbility holdAbility)
            {
                bool wasActive = holdAbility.IsActive;
                holdAbility.SetInputHeld(held);
                if (!wasActive && holdAbility.IsActive)
                    AbilityActivated?.Invoke(slot, ability.AbilityName);
                return;
            }

            if (pressed && ability != null && ability.TryActivate())
                AbilityActivated?.Invoke(slot, ability.AbilityName);
        }

        private void ReleaseHeldAbilities()
        {
            if (ability1 is IHoldCharacterAbility firstHold)
                firstHold.SetInputHeld(false);
            if (ability2 is IHoldCharacterAbility secondHold)
                secondHold.SetInputHeld(false);
        }

        private void OnDisable() => ReleaseHeldAbilities();

        /// <summary>Attempts to activate a configured one-shot slot. Hold abilities require holding their binding.</summary>
        public bool TryActivateSlot(int slot)
        {
            ICharacterAbility ability = slot == 1 ? ability1 : slot == 2 ? ability2 : null;
            if (ability == null || ability is IHoldCharacterAbility)
                return false;

            bool activated = ability.TryActivate();
            if (activated)
                AbilityActivated?.Invoke(slot, ability.AbilityName);
            return activated;
        }

        private static string SafeBinding(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
