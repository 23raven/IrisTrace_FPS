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

            HandleSlot(ability1, input.Ability1Pressed, holdInput != null && holdInput.Ability1Held);
            HandleSlot(ability2, input.Ability2Pressed, holdInput != null && holdInput.Ability2Held);
        }

        private static void HandleSlot(ICharacterAbility ability, bool pressed, bool held)
        {
            if (ability is IHoldCharacterAbility holdAbility)
            {
                holdAbility.SetInputHeld(held);
                return;
            }

            if (pressed && ability != null)
                ability.TryActivate();
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
            return ability.TryActivate();
        }

        private static string SafeBinding(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
