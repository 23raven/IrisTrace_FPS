using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Abilities
{
    /// <summary>Routes two abstract input slots to any MonoBehaviour implementing ICharacterAbility.</summary>
    [DisallowMultipleComponent]
    public sealed class AbilityInputHandler : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private MonoBehaviour ability1Component;
        [SerializeField] private MonoBehaviour ability2Component;

        private IAbilityInput input;
        private ICharacterAbility ability1;
        private ICharacterAbility ability2;

        public void Configure(MonoBehaviour inputBehaviour, MonoBehaviour firstAbility, MonoBehaviour secondAbility)
        {
            inputSource = inputBehaviour;
            ability1Component = firstAbility;
            ability2Component = secondAbility;
            ResolveContracts();
        }

        private void Awake() => ResolveContracts();

        private void ResolveContracts()
        {
            input = inputSource as IAbilityInput;
            ability1 = ability1Component as ICharacterAbility;
            ability2 = ability2Component as ICharacterAbility;
        }

        private void Update()
        {
            if (input == null)
                return;

            if (input.Ability1Pressed)
                ability1?.TryActivate();
            if (input.Ability2Pressed)
                ability2?.TryActivate();
        }
    }
}
