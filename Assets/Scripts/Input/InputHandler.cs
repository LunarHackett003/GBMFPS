using Starlight.Connection;
using Starlight.PlayerInteraction;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Starlight.InputHandling
{
    public class InputHandler : MonoBehaviour
    {
        internal static InputHandler instance;
        [SerializeField] internal PlayerInput input;
        [SerializeField] internal Vector2 moveVec, lookVec;
        [SerializeField] internal bool jumpInput, focusInput, fireInput, sprintInput, crouchInput, meleeInput;
        [SerializeField] internal bool holdCrouch;
        [SerializeField] internal bool forwardToMantle;
        internal float moveMagnitude;
        [SerializeField] internal Vector2 lookSpeed = new(15, -15);

        [SerializeField] internal bool paused;
        [SerializeField] GameObject pauseMenu;

        public string filepath;
        [SerializeField] internal Slider xSensSlider, ySensSlider;
        [SerializeField] internal Toggle holdCrouchToggle;
        [SerializeField] internal TextMeshProUGUI joinCodeDisplay;

        public class Settings
        {
            public Vector2 lookSpeed = new(15, -15);
            public bool holdCrouch = false;
        }
        public Settings mySaveSettings = new();
        private void Awake()
        {
#if UNITY_EDITOR
            filepath = Application.dataPath;
#else
            filepath = Application.persistentDataPath;
#endif
            filepath += "/inputsettings.txt";
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
            //Now we need to load our settings
            LoadSettings();
        }
        private void OnApplicationQuit()
        {
            SaveSettings();
        }
        void LoadSettings()
        {
            if (!File.Exists(filepath))
            {
                SaveSettings();
            }
            JsonUtility.FromJsonOverwrite(File.ReadAllText(filepath), mySaveSettings);
            lookSpeed = mySaveSettings.lookSpeed;
            holdCrouch = mySaveSettings.holdCrouch;
            xSensSlider.value = lookSpeed.x;
            ySensSlider.value = -lookSpeed.y;
            holdCrouchToggle.isOn = holdCrouch;
        }
        void SaveSettings()
        {
            mySaveSettings.lookSpeed = lookSpeed;
            mySaveSettings.holdCrouch = holdCrouch;

            if(!File.Exists(filepath))
            {
                File.Create(filepath).Close();
            }
            StreamWriter sw = new(filepath, false);
            sw.Write(JsonUtility.ToJson(mySaveSettings));
            sw.Close();
            sw.Dispose();
        }
        private void FixedUpdate()
        {
            if (!(ConnectionManager.Instance && ConnectionManager.Instance.inGame))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }
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
        public void GetPauseInput(InputAction.CallbackContext context)
        {

            if (context.performed && ((ConnectionManager.Instance && ConnectionManager.Instance.inGame) || !NetworkManager.Singleton))
            {
                TogglePause();
            }
        }
        public void GetMeleeInput(InputAction.CallbackContext context)
        {
            meleeInput = context.performed || context.started;
        }
        void TogglePause()
        {
            paused = !paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
            pauseMenu.SetActive(paused);
        }
        public void Unpause()
        {
            paused = true;
            TogglePause();
        }
        public void SetHorizontalLookSpeed(float value)
        {
            lookSpeed.x = value;
        }
        public void SetVerticalLookSpeed(float value)
        {
            lookSpeed.y = -value;
        }
        public void SetCrouchHold(bool value)
        {
            holdCrouch = value;
        }
        public void TryQuitGame()
        {
            ConnectionManager.Instance.TryQuitGame();
            pauseMenu.SetActive(false);
            paused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}