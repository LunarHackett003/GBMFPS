using Starlight.Connection;
using Starlight.PlayerInteraction;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Starlight.InputHandling
{
    public class InputHandler : MonoBehaviour
    {
        internal static InputHandler instance;
        [SerializeField] internal PlayerInput input;
        [SerializeField] internal Vector2 moveVec, lookVec;
        [SerializeField] internal bool jumpInput, focusInput, fireInput, sprintInput, crouchInput;
        [SerializeField] internal bool holdCrouch;
        [SerializeField] internal bool forwardToMantle;
        internal float moveMagnitude;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            //We need to find the input system UI module and the main camera for the scene, and apply them to the input manager
            input.camera = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.CompareTag("MainCamera"));
            input.uiInputModule = FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        }

        public void GetSocialMenuInput(InputAction.CallbackContext context)
        {
            if (SocialMenuUI.instance)
            {
                if (context.performed && SocialMenuUI.instance.activated)
                    SocialMenuUI.instance.ToggleSocialMenu();
            }
        }
        public void GetPlayerListInput(InputAction.CallbackContext context)
        {
            if (ConnectionManager.Instance)
            {
                if (ConnectionManager.Instance.inGame)
                {
                    if (context.performed)
                    {
                        ConnectionManager.Instance.ToggleLobbyCG(true);
                    }
                    if (context.canceled)
                    {
                        ConnectionManager.Instance.ToggleLobbyCG(false);
                    }
                }
            }
        }
        public void GetJumpInput(InputAction.CallbackContext context)
        {
            jumpInput = context.performed;
        }
        public void GetFireInput(InputAction.CallbackContext context)
        {
            fireInput = context.performed || context.started;
        }
        public void GetSprintInput(InputAction.CallbackContext context)
        {
            sprintInput = context.performed || context.started;
        }
        public void GetFocusInput(InputAction.CallbackContext context)
        {
            focusInput = context.performed || context.started;
        }
        public void GetCrouchInput(InputAction.CallbackContext context)
        {
            crouchInput = context.performed || context.started;
        }
        public void GetMoveInput(InputAction.CallbackContext context)
        {
            moveVec = context.ReadValue<Vector2>();
        }
        public void GetLookInput(InputAction.CallbackContext context)
        {
            lookVec = context.ReadValue<Vector2>();
        }

    }
}