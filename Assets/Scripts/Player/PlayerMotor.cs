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
        //fix compile errors pls unity
        [SerializeField] internal bool isNotMine;
        [SerializeField] internal Transform head, focusPositionReceiver, focusPositionTarget, swayTransform, aimRotationTransform, aimRotationAnchor;

        [SerializeField] internal Rigidbody rb;

        [SerializeField, Header("Aim")] internal Vector2 lookAngle;
        [SerializeField]internal Vector2 lookPitchClamp;
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
        [SerializeField] internal Vector2 oldLookAngle, lookAngleDelta; 
        [SerializeField] internal Vector3 currentAimSwayPos, currentAimSwayRot, maxAimSway, lookSwayPosScale, lookSwayRotScale;

        [SerializeField, Header("Recoil")] float globalRecoilMultiplier;
        [SerializeField] internal float recoilRotMultiply, recoilPosMultiply, recoilLerpSpeed, recoilDecaySpeed;
        internal Vector3 currentRecoilPos, currentRecoilRot, targetRecoilPos, targetRecoilRot;
        [SerializeField, Header("Impact Recoil")] internal Vector3 targetImpactRecoil;
        internal Vector3 impactdampvel;
        [SerializeField] internal Vector3 currentImpactRecoil, currentImpactAngle;
        [SerializeField] internal float impactImpulseMultiplier, impactPosMultiplier, impactAngleMultiplier, impactDecayTime, jumpRecoil, impactLerpSpeed;

        [SerializeField, Header("Movement Bobbing")] internal Transform bobbingTransform;
        [SerializeField] internal Vector3 bobPosSpeed, bobRotSpeed, bobPosMultiply, bobRotMultiply, currBobPos, currBobRot;
        [SerializeField] internal Vector2 bobPosLerpSpeedMoving, bobRotLerpSpeedMoving, bobPosScaleMoving, bobRotScaleMoving, bobPosSpeedMoving, bobRotSpeedMoving;
        [SerializeField] float bobFocusMultiply;
        [SerializeField] internal float posTime, rotTime;
        internal Vector2 posAngle, rotAngle;
        [SerializeField, Header("Movement Pose Offset")] internal Vector3 movePosAdd;
        [SerializeField] internal Vector3 moveRotAdd;

        [SerializeField, Header("Focus")] internal CinemachineVirtualCamera playerCam;
        [SerializeField] internal float currentFocus, focusSpeed, focusAimSlowCoefficient, focusZoomLevel, focusMoveSpeedMultiplier;
        [SerializeField] internal Vector2 focusFOV = new(80, 70);

        [SerializeField, Header("Wall Movement")] internal float wallrideForwardForce;
        [SerializeField] internal Vector3 wallCheckOrigin, wallCheckSize, currentWallNormal;
        [SerializeField] internal float wallrideCheckDistance, wallrideStickForce, wallrideJumpForce, wallrideMaxTime, 
            wallrideDownForce, currentWallrideTime, wallrideEnableAfterGroundedTime, wallrideReEnableTime, wallrideCurrentReEnableTime, wallrideTurnSpeed, wallrideClimbStartVelocity;
        [SerializeField] internal bool canWallride, isWallriding;
        [SerializeField] internal WallrideSide wallrideSide;
        internal enum WallrideSide
        {
            none = 0,
            left = 1,
            right = 2,
            front = 4
        }
        [SerializeField, Header("Wall Movement - Mantling")] internal float mantleSpeed;
        [SerializeField] internal float mantleCheckDistance, mantleStandHeight, mantlePreventHeight, mantleProgress, mantleRetryTime, mantleMinHeight;
        internal float currentMantleRetryTime;
        [SerializeField] Vector3 mantleLowerCastPosition, mantleUpperCastPosition, mantleGroundCheckPosition;
        [SerializeField] internal AnimationCurve mantleUpPosCurve, mantleLateralPosCurve;
        [SerializeField] internal Vector3 mantleStartPos, mantleEndPos, mantleEndPosOffset;
        [SerializeField] internal bool mantling;
        [SerializeField, Header("Other Movement")] internal float jumpUpwardForce;
        [SerializeField] internal bool doubleJumped, sliding, sprinting;
        [SerializeField] internal float slideInitialForce, slideDrag, slideControlForce, slideMinimumVelocity,jumpLateralForce, slideTilt, slideTiltSpeed, currentSlideTilt, slideAdditiveFOV;
        [SerializeField] internal float sprintForce;

        [SerializeField, Header("Crouching")] internal float crouchHeight;
        [SerializeField] internal bool crouching;
        [SerializeField] internal float standHeight, crouchSpeed, crouchMoveSpeedMultiplier, crouchLerp, capsuleHeadHeightBuffer;
        [SerializeField] internal Transform crouchTransform;
        [SerializeField] internal CapsuleCollider capsule;
        internal float dampedMoveMagnitude;
        internal enum MovementState
        {
            onFoot = 0,
            airborne = 1,
            wallriding = 2,
            sliding = 4,
            mantling = 8
        }
        [SerializeField] internal MovementState moveState;
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
        internal float focusLerp;
        internal Vector3 lookSwayPosM;
        internal Vector3 lookSwayRotM;
        internal float swayFocusLerp;
        internal bool myCrouchInput;

        public NetworkVariable<float> capsuleHeight = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> capsuleCentre = new(writePerm: NetworkVariableWritePermission.Owner);

        bool jumppressed = false;
        [SerializeField] bool crouchpressed = false;
        bool sprintpressed = false;
        void ViewDynamics()
        {

            //First we do the view bobbing maths
            float bobPosMult = Mathf.Lerp(bobPosScaleMoving.x, bobPosScaleMoving.y, dampedMoveMagnitude);
            float bobPosSpeed = Mathf.Lerp(bobPosSpeedMoving.x, bobPosSpeedMoving.y, dampedMoveMagnitude);
            float bobRotMult = Mathf.Lerp(bobRotScaleMoving.x, bobRotScaleMoving.y, dampedMoveMagnitude);
            float bobRotSpeed = Mathf.Lerp(bobRotSpeedMoving.x, bobRotSpeedMoving.y, dampedMoveMagnitude);

            focusLerp = Mathf.Lerp(1, bobFocusMultiply, currentFocus);
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
            Vector3 moveAddRot = new() { x = moveRotAdd.x * movementDamped.y, y = moveRotAdd.y * movementDamped.x, z = moveRotAdd.z * movementDamped.x };

            //Set them here
            currBobPos = Vector3.Lerp(currBobPos, (Vector3)(posAngle * bobPosMult).ScaleReturn(bobPosMultiply) + moveAddPos, Time.smoothDeltaTime * Mathf.Lerp(bobPosLerpSpeedMoving.x, bobPosLerpSpeedMoving.y, dampedMoveMagnitude)) * focusLerp;
            currBobRot = Vector3.Lerp(currBobRot, (Vector3)(rotAngle * bobRotMult).ScaleReturn(bobRotMultiply) + moveAddRot, Time.smoothDeltaTime * Mathf.Lerp(bobRotLerpSpeedMoving.x, bobRotLerpSpeedMoving.y, dampedMoveMagnitude)) * focusLerp;

            //Then we do the aiming stuff so the player can rotate
            lookAngle += InputHandler.instance.paused ? Vector2.zero : Time.smoothDeltaTime * InputHandler.instance.lookSpeed * InputHandler.instance.lookVec / Mathf.Lerp(1, 1 + (focusZoomLevel * focusAimSlowCoefficient), currentFocus);
            aimRecoilInfluence = currentRecoilRot * aimRecoilInfluence;
            lookAngle.y = Mathf.Clamp(lookAngle.y + aimRecoilInfluence.x, lookPitchClamp.x, lookPitchClamp.y);
            lookAngleDelta = (oldLookAngle - lookAngle);
            aimRotationTransform.localRotation = Quaternion.Euler(lookAngle.y, aimRecoilInfluence.y, Mathf.Lerp(0, slideTilt, currentSlideTilt));
            transform.localRotation = Quaternion.Euler(0, lookAngle.x, 0);
            lookAngle.x %= 360;
            oldLookAngle = lookAngle;
            //Then we do the view sway maths
            swayFocusLerp = Mathf.Lerp(1, aimSwayFocusMultiply, currentFocus);
            lookSwayPosM = lookAngleDelta.ScaleReturn(lookSwayPosScale * aimSwayPosMultiply).ClampThis(-maxAimSway, maxAimSway);
            lookSwayRotM = new Vector3(lookAngleDelta.y, lookAngleDelta.x, lookAngleDelta.y).ScaleReturn(lookSwayRotScale * aimSwayRotMultiply).ClampThis(-maxAimSway, maxAimSway);


            targetImpactRecoil = Vector3.SmoothDamp(targetImpactRecoil, Vector3.zero, ref impactdampvel, impactDecayTime * targetImpactRecoil.magnitude);
            currentImpactRecoil = Vector3.Lerp(currentImpactRecoil, targetImpactRecoil, Time.smoothDeltaTime * impactLerpSpeed);
            Vector3 angularImpactRecoil = new Vector3(currentImpactRecoil.y, currentImpactRecoil.x, 0) * impactAngleMultiplier;
            playerCam.transform.SetLocalPositionAndRotation(currentImpactRecoil * impactPosMultiplier, Quaternion.Euler(angularImpactRecoil));


            currentAimSwayPos = swayFocusLerp * Vector3.Lerp(currentAimSwayPos, lookSwayPosM, Time.smoothDeltaTime * aimSwaySpeed);
            currentAimSwayRot = swayFocusLerp * Vector3.Lerp(currentAimSwayRot, lookSwayRotM, Time.smoothDeltaTime * aimSwaySpeed);
            bobbingTransform.localPosition = currBobPos + currentAimSwayPos;
            Focus();
            bobbingTransform.localRotation = Quaternion.Euler(currBobRot + currentAimSwayRot);


        }

        void InputLogic()
        {
            movementDamped = Vector2.SmoothDamp(movementDamped, InputHandler.instance.paused ? Vector2.zero : InputHandler.instance.moveVec, ref moveDampVelocity, movementDampTime);
            dampedMoveMagnitude = movementDamped.magnitude;

            //Are we sprinting?
            sprinting = InputHandler.instance.sprintInput && moveState == MovementState.onFoot;
            //Are we crouching?
            if (InputHandler.instance.holdCrouch)
            {
                crouching = InputHandler.instance.crouchInput;
                if (!crouchpressed && crouching)
                {
                    Crouch();
                }
            }
            else
            {
                if (!crouchpressed)
                {
                    if (InputHandler.instance.crouchInput) {
                        crouching = !crouching;
                        if (crouching)
                            Crouch();
                    }
                }
            }
            crouchpressed = (InputHandler.instance.crouchInput == true);

            currentFocus += Time.fixedDeltaTime * focusSpeed * (InputHandler.instance.focusInput ? 1 : -1);
            currentFocus = Mathf.Clamp01(currentFocus);

            if (InputHandler.instance.focusInput)
                sprinting = false;
            if (!sliding)
            {
                //If we're crouching before we sprint, we want to stop crouching and start sprinting, which means we need to stand up.
                if (crouching && sprinting)
                {
                    crouching = false;
                    InputHandler.instance.crouchInput = false;
                }
            }
        }
        void Crouch()
        {
            switch (moveState)
            {
                case MovementState.airborne:
                    break;
                case MovementState.wallriding:
                    //Cancel wallride
                    break;
                case MovementState.sliding:
                    //cancel slide
                    sliding = false;
                    break;
                case MovementState.mantling:
                    //Cancel mantle
                    rb.isKinematic = false;
                    mantling = false;
                    StartCoroutine(MantleEnable());
                    break;
                case MovementState.onFoot:
                    if (sprinting)
                    {
                        Slide();
                    }
                    break;
                default:
                    break;
            }
        }
        private void FixedUpdate()
        {
            if (isNotMine)
            {
                capsule.height = capsuleHeight.Value;
                capsule.center = new Vector3(0, capsuleCentre.Value, 0);
                return;
            }
            CheckGround();
            InputLogic();
            rb.isKinematic = mantling;
            crouchLerp = Mathf.Clamp01(crouchLerp + (Time.fixedDeltaTime * ((crouching || sliding) ? 1 : -1) * crouchSpeed));
            crouchTransform.localPosition = new Vector3(0, Mathf.Lerp(standHeight, crouchHeight, crouchLerp), 0);
            capsule.height = Mathf.Lerp(standHeight, crouchHeight, crouchLerp) + capsuleHeadHeightBuffer;
            capsule.center = new Vector3(0, (capsule.height - capsuleHeadHeightBuffer) / 2, 0);
            currentSlideTilt = Mathf.Clamp01(Time.fixedDeltaTime * slideTiltSpeed * (sliding ? 1 : -1));
            if (capsuleHeight.Value != capsule.height && capsuleCentre.Value != capsule.center.y) {
                capsuleHeight.Value = capsule.height;
                capsuleCentre.Value = capsule.center.y;
                    }
            if (InputHandler.instance.jumpInput && !InputHandler.instance.paused)
            {
                if (!jumppressed)
                {
                    Jump();
                    jumppressed = true;
                }
            }
            else
            {
                jumppressed = false;
            }

            if (!grounded)
            {
                if (mantling)
                    moveState = MovementState.mantling;
                else if (isWallriding)
                    moveState = MovementState.wallriding;
                else
                    moveState = MovementState.airborne;
            }
            else if (sliding)
                moveState = MovementState.sliding;
            else
                moveState = MovementState.onFoot;
            switch (moveState)
            {
                case MovementState.onFoot:
                    Movement();
                    break;
                case MovementState.airborne:
                    Movement();
                    WallRide();
                    break;
                case MovementState.wallriding:
                    WallRide();
                    break;
                case MovementState.sliding:
                    rb.AddForce(InputHandler.instance.moveVec.x * slideControlForce * transform.right);
                    if (rb.velocity.magnitude < slideMinimumVelocity)
                    {
                        sliding = false;
                        crouching = true;
                        InputHandler.instance.crouchInput = true;
                    }
                    break;
                case MovementState.mantling:
                    break;
                default:
                    break;
            }
            rb.drag = sliding ? slideDrag : (grounded ? playerDrag.x : playerDrag.y);
            rb.useGravity = !isWallriding;
            if (isWallriding)
            {
                currentWallrideTime += Time.fixedDeltaTime;
                if(currentWallrideTime > wallrideMaxTime)
                {
                    isWallriding = false;
                    StartCoroutine(WallrideDelay(wallrideReEnableTime));
                }
            }
            else
            {
                currentWallrideTime = 0;
            }
        }
        void Focus()
        {

            playerCam.m_Lens.FieldOfView = Mathf.Lerp(focusFOV.x + Mathf.Lerp(0, slideAdditiveFOV, currentSlideTilt), focusFOV.y, currentFocus);
            focusPositionReceiver.position = Vector3.Lerp(focusPositionReceiver.parent.position, focusPositionTarget.position, currentFocus);
            focusPositionReceiver.localPosition -= currBobPos + currentAimSwayPos;

        }
        void CheckGround()
        {
            grounded = Physics.SphereCast(transform.position, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundCheckLayermask)
                && Vector3.Dot(Vector3.up, hit.normal) > groundNormalDotThreshold;
            groundNormal = hit.normal;
            if (hit.collider)
                doubleJumped = false;
        }
        void Movement()
        {


            Vector3 moveVec = (sprinting ? sprintForce : Mathf.Lerp(moveForceMultiplier, crouchMoveSpeedMultiplier, crouchLerp)) *
                Mathf.Lerp(1, focusMoveSpeedMultiplier, currentFocus) * new Vector3() { x = movementDamped.x, y=0, z = movementDamped.y }.ProjectOntoPlane(grounded ? groundNormal : Vector3.up);
            if (rb)
            {
                rb.AddForce(transform.rotation * moveVec * (grounded ? moveForceMultiplier : airControlForce));
            }
        }
        void Slide()
        {
            sliding = true;
            crouching = false;
            moveState = MovementState.sliding;
            InputHandler.instance.crouchInput = false;
            rb.AddForce(transform.forward * slideInitialForce);
        }
        void TryMantle()
        {
            if (currentMantleRetryTime <= 0 && ((InputHandler.instance.forwardToMantle ? InputHandler.instance.moveVec.y > 0.5f : InputHandler.instance.jumpInput) || isWallriding))
            {
                print("checking mantle");
                if (Physics.Raycast(transform.TransformPoint(mantleLowerCastPosition), transform.forward, out RaycastHit hit, mantleCheckDistance, groundCheckLayermask))
                {
                    print("mantle check 1");
                    //Check the dot product of the negative wall normal and our forward direction, and return if the wall isn't steep enough, or is too steep
                    if (Vector3.Dot(-hit.normal, transform.forward) < 0.8f || !Physics.Raycast(transform.TransformPoint(mantleUpperCastPosition), transform.forward))
                        return;
                    print("mantle check 2");
                    //We've hit something on the bottom position, AND haven't hit something at the top position, so we can climb up the wall in front of us.
                    //The mantle start position is our current position, so set that now
                    mantleStartPos = transform.position;
                    //Now we need to get the end position. we do this with another raycast
                    //If we don't hit the ground, or the ground isn't walkable, we won't attempt to mantle.
                    if (!Physics.Raycast(transform.TransformPoint(mantleGroundCheckPosition), Vector3.down, out hit, 1.5f, groundCheckLayermask) || Vector3.Dot(hit.normal, Vector3.up) < groundNormalDotThreshold)
                        return;
                    print("mantle check 3");
                    //The surface we hit here is walkable, so we need to check just how much space we have above that point.
                    if (Physics.Raycast(hit.point + (Vector3.down * 0.1f), Vector3.up, out RaycastHit hit2, mantlePreventHeight, groundCheckLayermask))
                    {
                        //We've hit something here, so we need to make sure that we've got enough space to actually climb up without hitting our head.
                        //If the hit distance is less than our crouch distance, we'll cancel the mantle as we won't have the space to stand or crouch once we climb.
                        if (hit2.distance < mantleMinHeight)
                            return;
                        //We have enough space to crouch in this spot, so we'll force the player to crouch.
                        //We'll need to make sure we implement forced crouching so the player can't try to stand up if there's no space to do so.
                        crouching = true;
                    }
                    //We didn't hit anything above us so we're probably safe to stand up at the end point.
                    //we now need to set the end point from our last hit point.
                    mantleEndPos = hit.point + mantleEndPosOffset;
                    //We're now set to mantle!
                    //We'll start a coroutine for the mantle lerp
                    StartCoroutine(MantleLerp());
                }
            }
        }
        IEnumerator MantleEnable()
        {
            currentMantleRetryTime = mantleRetryTime;
            while (currentMantleRetryTime >= 0)
            {
                currentMantleRetryTime -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        void WallRide()
        {
            if (grounded || !canWallride)
            {
                isWallriding = false;
                return;
            }
            //We can actually wallride if we're not on the ground
            //So lets do the maths and stuff for that. This method also includes the mantle checks, which override wallriding.
            //First we'll do the mantle check, because we need to exit early if we *can* mantle
            TryMantle();
            //Now we've checke if we can mantle, we stop this method here because we can't mantle AND Wallride at the same time.
            if (mantling)
                return;
            print("checking wallride");
            //We're not mantling so lets try and wallride.
            //First check forward wallride. If we are wallriding "into" a wall, we'll start to climb it
            //We want to box cast in front of us first.
            print("checking front");
            if (WallCast(transform.forward, out RaycastHit hit))
            {
                wallrideSide = WallrideSide.front;
                if (!isWallriding)
                {
                    rb.velocity = new Vector3(rb.velocity.x, wallrideClimbStartVelocity, rb.velocity.z);
                }
                rb.AddForce(movementDamped.y * wallrideForwardForce * transform.up + (-currentWallNormal * wallrideStickForce));
                isWallriding = true;
                print("hit front");
                return;
            }
            print("checking left");
            if (WallCast(-transform.right, out hit))
            {
                wallrideSide = WallrideSide.left;
                rb.AddForce(movementDamped.y * wallrideForwardForce * transform.forward + (-currentWallNormal * wallrideStickForce));
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.1f, rb.velocity.z);
                isWallriding = true;
                print("hit left");
                return;
            }
            print("checking right");

            if (WallCast(transform.right, out hit))
            {
                wallrideSide = WallrideSide.right;
                rb.AddForce(movementDamped.y * wallrideForwardForce * transform.forward + (-currentWallNormal * wallrideStickForce));
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.1f, rb.velocity.z);
                isWallriding = true;
                print("hit right");

                return;
            }
            else
            {
                if (isWallriding)
                {
                    StartCoroutine(WallrideDelay(wallrideCurrentReEnableTime));
                }
                isWallriding = false;
            }

        }
        bool WallCast(Vector3 direction, out RaycastHit hit)
        {
            if (Physics.BoxCast(transform.TransformPoint(wallCheckOrigin), wallCheckSize / 2, direction, out hit, transform.rotation, wallrideCheckDistance, groundCheckLayermask))
            {
                //If the wall is too steep/too shallow, we can't wallride it.
                if (Vector3.Dot(-hit.normal, direction) < 0.9f || (wallrideCurrentReEnableTime > 0 && hit.normal == currentWallNormal))
                    return false;
                currentWallNormal = hit.normal;
                return true;
            }
            return false;
        }
        IEnumerator WallrideDelay(float time)
        {
            isWallriding = false;
            rb.AddForce(currentWallNormal * wallrideJumpForce);
            wallrideCurrentReEnableTime = time;
            while (wallrideCurrentReEnableTime > 0)
            {
                wallrideCurrentReEnableTime -= Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        IEnumerator MantleLerp()
        {
            float time = 0;
            float lerpTime = (mantleEndPos.y - mantleStartPos.y) / mantleSpeed;
            Vector3 yPos = new(0, mantleStartPos.y, 0);
            Vector3 lateralPos = new(mantleStartPos.x, 0, mantleStartPos.z);
            mantling = true;
            while (time < 1 && mantling)
            {
                time += Time.fixedDeltaTime / lerpTime;
                yPos.y = Mathf.Lerp(mantleStartPos.y, mantleEndPos.y, mantleUpPosCurve.Evaluate(time));
                lateralPos.x = Mathf.Lerp(mantleStartPos.x, mantleEndPos.x, mantleLateralPosCurve.Evaluate(time));
                lateralPos.z = Mathf.Lerp(mantleStartPos.z, mantleEndPos.z, mantleLateralPosCurve.Evaluate(time));
                transform.position = lateralPos + yPos;
                yield return new WaitForFixedUpdate();
            }
            mantling = false;
            rb.AddForce(movementDamped * moveForceMultiplier, ForceMode.Impulse);
            yield break;
        }
        void Jump()
        {
            if (moveState == MovementState.airborne)
            {
                if (!doubleJumped)
                    doubleJumped = true;
                else
                    return;
            }
            Vector3 jumpVec = Vector3.zero;
            switch (moveState)
            {
                case MovementState.onFoot:
                    jumpVec = transform.TransformDirection(new Vector3(movementDamped.x * jumpLateralForce, jumpUpwardForce, movementDamped.y * jumpLateralForce));
                    StartCoroutine(WallrideDelay(wallrideEnableAfterGroundedTime));
                    break;
                case MovementState.airborne:
                    jumpVec = transform.TransformDirection(new Vector3(movementDamped.x * jumpLateralForce, jumpUpwardForce, movementDamped.y * jumpLateralForce));
                    break;
                case MovementState.wallriding:
                    jumpVec = (currentWallNormal + Vector3.up + playerCam.transform.forward).normalized * wallrideJumpForce;
                    StartCoroutine(WallrideDelay(wallrideReEnableTime));
                    isWallriding = false;
                    break;
                case MovementState.sliding:
                    jumpVec = transform.TransformDirection(new Vector3(movementDamped.x * jumpLateralForce, jumpUpwardForce, movementDamped.y * jumpLateralForce));
                    sliding = false;
                    StartCoroutine(WallrideDelay(wallrideEnableAfterGroundedTime));
                    break;
                case MovementState.mantling:
                    jumpVec = (-transform.forward + Vector3.up) * wallrideJumpForce;
                    rb.isKinematic = false;
                    mantling = false;
                    StartCoroutine(MantleEnable());
                    break;
                default:
                    break;
            }

            rb.AddForce(jumpVec, ForceMode.Impulse);
            targetImpactRecoil += jumpVec * jumpRecoil;

        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
            Gizmos.DrawWireSphere(transform.position + Vector3.down * groundCheckDistance, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.TransformPoint(mantleUpperCastPosition), transform.forward * mantleCheckDistance);
            Gizmos.DrawRay(transform.TransformPoint(mantleLowerCastPosition), transform.forward * mantleCheckDistance);

            Gizmos.DrawRay(transform.TransformPoint(mantleGroundCheckPosition), Vector3.down * 1.5f);
            Vector3 pos = transform.TransformPoint(wallCheckOrigin);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos, wallCheckSize);
            Gizmos.DrawWireCube(pos + transform.right * wallrideCheckDistance, wallCheckSize);
            Gizmos.DrawWireCube(pos + transform.forward * wallrideCheckDistance, wallCheckSize);
            Gizmos.DrawWireCube(pos -transform.right * wallrideCheckDistance, wallCheckSize);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (crouching && Vector3.Dot(collision.impulse.normalized, groundNormal) > 0.5f )
            {
                Slide();
            }
            targetImpactRecoil += playerCam.transform.TransformDirection(collision.impulse * impactImpulseMultiplier);
        }
    }
}