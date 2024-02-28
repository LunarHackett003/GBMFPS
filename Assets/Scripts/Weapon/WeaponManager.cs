using Starlight.InputHandling;
using Starlight.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starlight.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        [SerializeField] internal bool usingWeapon;
        [SerializeField] internal PlayerMotor pm;
        [SerializeField] internal Transform damageOrigin;
        [SerializeField] Vector3 meleePosition, meleeBounds;
        [SerializeField] float meleeBoxcastDistance;
        [SerializeField] float meleeDamage;

        private void FixedUpdate()
        {
            pm.animationManager.animator.SetBool("Melee", InputHandler.instance.meleeInput || (!usingWeapon && InputHandler.instance.fireInput));
        }
        internal void Melee()
        {
            Debug.DrawRay(damageOrigin.TransformPoint(meleePosition), damageOrigin.forward * meleeBoxcastDistance, Color.red, 1f);
            if (Physics.BoxCast(damageOrigin.TransformPoint(meleePosition), meleeBounds / 2, damageOrigin.forward, out RaycastHit hit, damageOrigin.rotation, meleeBoxcastDistance))
            {
                if (hit.rigidbody.GetComponent<Entity>())
                {
                    hit.collider.GetComponent<Hitbox>().HitEntity(meleeDamage, damageOrigin.position) ;
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            if (damageOrigin)
            {
                Gizmos.matrix = damageOrigin.localToWorldMatrix;
                Gizmos.DrawWireCube(meleePosition, meleeBounds);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}