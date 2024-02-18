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
            if (context.performed && SocialMenuUI.instance.activated)
                SocialMenuUI.instance.ToggleSocialMenu();
        }
        public void GetPlayerListInput(InputAction.CallbackContext context)
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
}