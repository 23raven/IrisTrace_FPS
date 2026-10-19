using UnityEngine;
using IrisTrace.Runtime.Core;

namespace IrisTrace.Runtime.Weapons
{
    /// <summary>Routes abstract input events to a weapon; does not depend on a particular input backend.</summary>
    [DisallowMultipleComponent]
    public sealed class WeaponInputHandler : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour weaponComponent;
        [SerializeField] private MonoBehaviour inputSource;

        private IWeapon weapon;
        private IWeaponInput input;

        public void Configure(MonoBehaviour weaponBehaviour, MonoBehaviour inputBehaviour)
        {
            weaponComponent = weaponBehaviour;
            inputSource = inputBehaviour;
            weapon = weaponBehaviour as IWeapon;
            input = inputBehaviour as IWeaponInput;
        }

        private void Awake()
        {
            if (weapon == null && weaponComponent != null)
                weapon = weaponComponent as IWeapon;
            if (input == null && inputSource != null)
                input = inputSource as IWeaponInput;
        }

        private void Update()
        {
            if (weapon == null || input == null)
                return;

            if (input.FirePressed)
                weapon.BeginFire();
            if (input.FireReleased)
                weapon.EndFire();
            if (input.ReloadPressed)
                weapon.Reload();
        }

        private void OnDisable() => weapon?.EndFire();
    }
}
