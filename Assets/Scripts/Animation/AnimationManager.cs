using Starlight.Player;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Starlight.Animation {
    public class AnimationManager : NetworkBehaviour
    {
        [SerializeField] internal Animator animator;
        [SerializeField] PlayerMotor pm;
        [SerializeField] AnimatorOverrideController controller;
        [SerializeField] internal AnimationClipPair[] defaultAnimations;
        protected AnimationClipOverrides clipOverrides;


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();


            controller = new(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = controller;
            clipOverrides = new(controller.overridesCount);
            controller.GetOverrides(clipOverrides);

            UpdateAnimations();
        }
        private void Start()
        {

        }

        private void FixedUpdate()
        {
            if (pm.isNotMine)
                return;

            animator.SetFloat("Horizontal", pm.movementDamped.x);
            animator.SetFloat("Vertical", pm.movementDamped.y);
            animator.SetFloat("Crouch", pm.crouchLerp);
            animator.SetBool("Slide", pm.sliding);
        }

        public void UpdateAnimations()
        {
            var clips = pm.wm.currentEquippable ? pm.wm.currentEquippable.overrideClips : defaultAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    clipOverrides[clips[i].name] = clips[i].animationClip;
            }
            controller.ApplyOverrides(clipOverrides);
        }
        public void AnimatorBoolToggle(string animation, float time, bool targetvalue = true)
        {
            StartCoroutine(ToggleAnimationBoolean(animation, time, targetvalue));
        }
        IEnumerator ToggleAnimationBoolean(string parameter, float time, bool targetvalue = true)
        {
            animator.SetBool(parameter, targetvalue);
            yield return new WaitForSeconds(time);
            animator.SetBool(parameter, !targetvalue);
        }
    }
}