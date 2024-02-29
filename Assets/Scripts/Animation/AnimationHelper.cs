using Starlight.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starlight.Animation
{
    public class AnimationHelper : MonoBehaviour
    {
        [SerializeField] WeaponManager wm;
        private void Start()
        {
            wm = GetComponentInParent<WeaponManager>();
        }
        public void MeleeCallback()
        {
            wm.Melee();
        }
        public void WeaponSwitch()
        {
            wm.WeaponSwitchAnimationCallback();
        }
    }
}