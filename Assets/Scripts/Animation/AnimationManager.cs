using Starlight.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starlight.Animation {
    public class AnimationManager : MonoBehaviour
    {
        [SerializeField] internal Animator animator;
        [SerializeField] PlayerMotor pm;
        private void FixedUpdate()
        {
            animator.SetFloat("Horizontal", pm.movementDamped.x);
            animator.SetFloat("Vertical", pm.movementDamped.y);
            animator.SetFloat("Crouch", pm.crouchLerp);
            animator.SetBool("Slide", pm.sliding);
        }
    }
}