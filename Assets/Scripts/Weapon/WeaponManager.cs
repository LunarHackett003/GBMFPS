using Starlight.InputHandling;
using Starlight.Player;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Starlight.Weapons
{
    public class WeaponManager : NetworkBehaviour
    {
        [SerializeField] internal bool usingWeapon;
        [SerializeField] internal PlayerMotor pm;
        [SerializeField] internal Transform damageOrigin;
        [SerializeField] Vector3 meleePosition, meleeBounds;
        [SerializeField] float meleeBoxcastDistance;
        [SerializeField] float meleeDamage;

        //Weapons
        public enum CurrentEquippableSlot
        {
            /// <summary>
            /// When the player is unarmed or is not currently using a weapon
            /// </summary>
            none = 0,
            /// <summary>
            /// The player's primary weapon
            /// </summary>
            primary = 1,
            /// <summary>
            /// The player's secondary weapon
            /// </summary>
            secondary = 2,
            /// <summary>
            /// A gadget, temporary pickup weapon, or anything else.
            /// </summary>
            other = 4,
        }
        public CurrentEquippableSlot currentSlot;
        [SerializeField, Header("Weapons")] internal Equippable currentEquippable;
        [SerializeField] internal Weapon[] weapons;
        [SerializeField] internal int equippableIndex;
        public NetworkVariable<int> netEquipIndex = new(0, readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Owner);

        [SerializeField] internal bool gadgetPressed;
        [SerializeField] internal bool usingGadget;


        [SerializeField] Transform leftHand, rightHand;
        [SerializeField] Transform objectInLeftHand, objectInRightHand;
        [SerializeField] Transform weaponTargetPoint;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!pm.isNotMine)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    weapons[i].gameObject.layer = LayerMask.NameToLayer("Player");
                }
            }
        }

        private void FixedUpdate()
        {
            if (pm.isNotMine)
                return;
            pm.animationManager.animator.SetBool("Melee", InputHandler.instance.meleeInput || (currentSlot == CurrentEquippableSlot.none && InputHandler.instance.fireInput));
        }
        private void LateUpdate()
        {
            if (objectInLeftHand)
            {
                objectInLeftHand.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            }
            if (objectInRightHand)
            {
                objectInRightHand.SetPositionAndRotation(rightHand.position, rightHand.rotation);
            }
            if (currentEquippable)
            {
                currentEquippable.transform.SetPositionAndRotation(weaponTargetPoint.position, weaponTargetPoint.rotation);
            }
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
        public void SwitchWeapons(int newIndex)
        {
            equippableIndex = newIndex;
            pm.animationManager.AnimatorBoolToggle("WeaponSwitch", 0.2f, true);
            netEquipIndex.Value = newIndex;
        }
        public void WeaponSwitchAnimationCallback()
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].gameObject.SetActive(false);
            }
            if (equippableIndex < weapons.Length)
            {
                currentEquippable = weapons[equippableIndex];
            }
            else
                currentEquippable = null;
            pm.animationManager.UpdateAnimations();
        }
        public void ThrowGrenade()
        {
            if((InputHandler.instance.grenadeInput || InputHandler.instance.tacticalInput) && !(gadgetPressed))
            {
                StartCoroutine(ThrowGrenadeCoroutine(InputHandler.instance.grenadeInput ? "Grenade" : "Gadget"));
                gadgetPressed = true;
            }
        }
        IEnumerator ThrowGrenadeCoroutine(string gadgetType)
        {
            pm.animationManager.animator.SetBool(gadgetType, true);
            while (InputHandler.instance.grenadeInput || InputHandler.instance.tacticalInput)
            {
                yield return new WaitForFixedUpdate();
            }
            pm.animationManager.animator.SetBool(gadgetType, false);
        }
    }
}