using Cinemachine;
using Cinemachine.Utility;
using Starlight.Connection;
using Starlight.InputHandling;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Starlight.Player
{
    public class PlayerMotor : NetworkBehaviour
    {
        [SerializeField] internal bool isNotMine;
        [SerializeField] internal Transform head, focusPositionReceiver, focusPositionTarget, swayTransform, aimRotationTransform, aimRotationAnchor;

        [SerializeField] internal Rigidbody rb;

        [SerializeField, Header("Aim")] internal Vector2 lookSpeed;
        [SerializeField] internal Vector2 lookAngle, lookPitchClamp;
        [SerializeField] internal Vector2 aimRecoilInfluence;

        [SerializeField, Header("Movement")] internal Vector2 movementDamped;
        internal Vector2 moveDampVelocity;
        [SerializeField] internal float movementDampTime;
        [SerializeField] internal float moveForceMultiplier, airControlForce;
        [SerializeField, Header("Movement - Physics"), Tooltip("X corresponds to drag while on the ground and moving normally or while wall-running, " +
            "Y corresponds to drag while airborne or sliding.")] internal Vector2 playerDrag;
        [SerializeField] internal float groundCheckDistance, groundCheckRadius, groundNormalDotThreshold;
        [SerializeField] internal LayerMask groundCheckLayermask;
        [SerializeField] internal bool grounded;
        [SerializeField] Vector3 groundNormal;

        [SerializeField, Header("Aim Swaying")] internal Transform aimSwayTransform;
        [SerializeField] internal float aimSwaySpeed, aimSwayRotMultiply, aimSwayPosMultiply, aimSwayFocusMultiply;
        [SerializeField] internal Vector2 oldLookAngle, lookAngleDelta, currentAimSwayPos, currentAimSwayRot, maxAimSway, lookSwayScale;

        [SerializeField, Header("Recoil")] float globalRecoilMultiplier;
        [SerializeField] internal float recoilRotMultiply, recoilPosMultiply, recoilLerpSpeed, recoilDecaySpeed;
        internal Vector3 currentRecoilPos, currentRecoilRot, targetRecoilPos, targetRecoilRot;

        [SerializeField, Header("Movement Bobbing")] internal Transform bobbingTransform;
        [SerializeField] internal Vector3 bobPosSpeed, bobRotSpeed, bobPosMultiply, bobRotMultiply, currBobPos, currBobRot;
        [SerializeField] internal Vector2 bobPosLerpSpeedMoving, bobRotLerpSpeedMoving, bobPosScaleMoving, bobRotScaleMoving, bobPosSpeedMoving, bobRotSpeedMoving;
        [SerializeField] float bobFocusMultiply;
        [SerializeField] internal float posTime, rotTime;
        internal Vector2 posAngle, rotAngle;
        [SerializeField, Header("Movement Pose Offset")] internal Vector3 movePosAdd;
        [SerializeField] internal Vector3 moveRotAdd;

        [SerializeField, Header("Focus")] internal CinemachineVirtualCamera playerCam;
        [SerializeField] internal float currentFocus, focusSpeed, focusAimSlowCoefficient, focusZoomLevel;
        [SerializeField] internal Vector2 focusFOV = new(80, 70);

        internal float dampedMoveMagnitude;
        bool CheckOwnership { get { return (!(IsOwner || (!ConnectionManager.Instance || ConnectionManager.Instance.localTesting))); } }
        private void Awake()
        {
            isNotMine = CheckOwnership;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void LateUpdate()
        {
            //If we dont own this object or are not in local testing mode, we won't do anything
            if (isNotMine)
                return;

            ViewDynamics();
        }

        void ViewDynamics()
        {
            //First we do the view bobbing maths
            float bobPosMult = Mathf.Lerp(bobPosScaleMoving.x, bobPosScaleMoving.y, dampedMoveMagnitude);
            float bobPosSpeed = Mathf.Lerp(bobPosSpeedMoving.x, bobPosSpeedMoving.y, dampedMoveMagnitude);
            float bobRotMult = Mathf.Lerp(bobRotScaleMoving.x, bobRotScaleMoving.y, dampedMoveMagnitude);
            float bobRotSpeed = Mathf.Lerp(bobRotSpeedMoving.x, bobRotSpeedMoving.y, dampedMoveMagnitude);

            float focusLerp = Mathf.Lerp(1, bobFocusMultiply, currentFocus);
            //Since we're using sin/cosine, we can modulo these by 360 to get a decent amount of curve.
            posTime = (posTime + (Time.smoothDeltaTime * bobPosSpeed)) % 360;
            rotTime = (rotTime + (Time.smoothDeltaTime * bobRotSpeed)) % 360;

            posAngle = new()
            {
                x = Mathf.Sin(posTime * this.bobPosSpeed.x),
                y = Mathf.Cos(posTime * this.bobPosSpeed.y)
            };
            rotAngle = new()
            {
                x = Mathf.Sin(rotTime * this.bobRotSpeed.x),
                y = Mathf.Cos(rotTime * this.bobRotSpeed.y)
            };

            Vector3 moveAddPos = new() { x = movePosAdd.x * movementDamped.x, z = movementDamped.y * movePosAdd.y };
            Vector3 moveAddRot = new() { x = moveRotAdd.x * movementDamped.y, y = moveRotAdd.y * movementDamped.x, z = moveRotAdd.z * movementDamped.x};

            //Set them here
            currBobPos = Vector3.Lerp(currBobPos, (Vector3)(posAngle * bobPosMult).ScaleReturn(bobPosMultiply) + moveAddPos, Time.smoothDeltaTime * Mathf.Lerp(bobPosLerpSpeedMoving.x, bobPosLerpSpeedMoving.y, dampedMoveMagnitude)) * focusLerp;
            currBobRot = Vector3.Lerp(currBobRot, (Vector3)(rotAngle * bobRotMult).ScaleReturn(bobRotMultiply) + moveAddRot, Time.smoothDeltaTime * Mathf.Lerp(bobRotLerpSpeedMoving.x, bobRotLerpSpeedMoving.y, dampedMoveMagnitude)) * focusLerp;

            //Then we do the aiming stuff so the player can rotate
            lookAngle += (Time.smoothDeltaTime * lookSpeed * InputHandler.instance.lookVec) / Mathf.Lerp(1, 1 + (focusZoomLevel * focusAimSlowCoefficient), currentFocus);
            aimRecoilInfluence = currentRecoilRot * aimRecoilInfluence;
            lookAngle.y = Mathf.Clamp(lookAngle.y + aimRecoilInfluence.x, lookPitchClamp.x, lookPitchClamp.y);
            lookAngleDelta = (oldLookAngle - lookAngle);
            aimRotationTransform.localRotation = Quaternion.Euler(lookAngle.y, aimRecoilInfluence.y, 0);
            transform.localRotation = Quaternion.Euler(0, lookAngle.x, 0);
            lookAngle.x %= 360;
            oldLookAngle = lookAngle;
            //Then we do the view sway maths

            float swayFocusLerp = Mathf.Lerp(1, aimSwayFocusMultiply, currentFocus);
            Vector2 lookSwayPosM = ((lookAngleDelta * lookSwayScale) * aimSwayPosMultiply).ClampThis(-maxAimSway, maxAimSway);
            Vector2 lookSwayRotM = ((lookAngleDelta * lookSwayScale) * aimSwayRotMultiply).ClampThis(-maxAimSway, maxAimSway);

            currentAimSwayPos = Vector2.Lerp(currentAimSwayPos, lookSwayPosM, Time.smoothDeltaTime * aimSwaySpeed) * swayFocusLerp;
            currentAimSwayRot = Vector2.Lerp(currentAimSwayRot, lookSwayRotM, Time.smoothDeltaTime * aimSwaySpeed) * swayFocusLerp;
            bobbingTransform.SetLocalPositionAndRotation(currBobPos + (Vector3)currentAimSwayPos * aimSwayPosMultiply, Quaternion.Euler(currBobRot + (Vector3)currentAimSwayRot * aimSwayRotMultiply));
        }

        private void FixedUpdate()
        {
            if (isNotMine)
                return;
            ChecKGround();
            Movement();
        }
        void ChecKGround()
        {
            grounded = Physics.SphereCast(transform.position, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundCheckLayermask)
                && Vector3.Dot(Vector3.up, hit.normal) > groundNormalDotThreshold;
            groundNormal = hit.normal;
        }
        void Movement()
        {
            movementDamped = Vector2.SmoothDamp(movementDamped, InputHandler.instance.moveVec, ref moveDampVelocity, movementDampTime);
            dampedMoveMagnitude = movementDamped.magnitude;
            Vector3 moveVec = moveForceMultiplier * new Vector3() { x = movementDamped.x , z = movementDamped.y}.ProjectOntoPlane(grounded ? groundNormal : Vector3.up);
            if (rb)
            {
                rb.drag = grounded ? playerDrag.x : playerDrag.y;
                rb.AddForce(transform.rotation * moveVec * (grounded ? moveForceMultiplier : airControlForce));
            }
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * groundCheckDistance, groundCheckRadius);
        }
    }
}