using Eflatun.SceneReference;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starlight.GamingService
{
    public class AuthManager : MonoBehaviour
    {
        internal static AuthManager Instance;
        internal string playerID;
        [SerializeField] internal string username;
        public bool signOutOnClose;
        [SerializeField] TMP_InputField usernameInputField;
        public SceneReference mainMenuScene;
        public GameObject loginMenu;
        [SerializeField] bool firstSignIn;
        [SerializeField] bool forceFirstSignIn;
        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
            DontDestroyOnLoad(this);
            loginMenu.SetActive(false);
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (ServicesInitializationException e)
            {
                Debug.LogException(e);
                return;
            }
            PlayerAccountService.Instance.SignedIn += SignInWithUnity;
            //If we already have a cached account, we don't want to let the player attempt to sign in.
            //We should first check this before enabling the menu.
            //We'll call SignInToGame first to allow the player to log in, and then the button we've made will trigger SignInWithUnity.

            SignInToGame();
        }
        async void SignInToGame() { 

            if (!AuthenticationService.Instance.SessionTokenExists)
            {
                if (PlayerAccountService.Instance.IsSignedIn)
                {
                    SignInWithUnity();
                }
                try
                {
                    await PlayerAccountService.Instance.StartSignInAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                //we don't have a cached account so we'll need to let the player modify their name.

            }
            else
            {
                //Sign in a cached user
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                SignInComplete();
            }
        }

        public async void SignInComplete()
        {
            //we've successfully signed in, and now we can
            if (firstSignIn || forceFirstSignIn)
            {
                username = await AuthenticationService.Instance.UpdatePlayerNameAsync(usernameInputField.text);
            }
            else
            {
                username = await AuthenticationService.Instance.GetPlayerNameAsync();
            }
            usernameInputField.text = username;
            print($"session token exists:{AuthenticationService.Instance.SessionTokenExists}");
            await SceneManager.LoadSceneAsync(mainMenuScene.Name, LoadSceneMode.Single);
        }
        async void SignInWithUnity()
        {
            try
            {
                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                var playerInfo = await AuthenticationService.Instance.GetPlayerInfoAsync();
                if (forceFirstSignIn || CheckDateTimeAgainstTodayAndNow(playerInfo.CreatedAt.Value))
                {
                    firstSignIn = true;
                    loginMenu.SetActive(true);
                }
                else
                    SignInComplete();
            }
            catch (System.Exception)
            {
                throw;
            }

        }
        bool CheckDateTimeAgainstTodayAndNow(System.DateTime dateTimeToCheck)
        {
            if(dateTimeToCheck.Date == System.DateTime.Today.Date)
            {
                Debug.Log($"checked date as {dateTimeToCheck.Date} - passed check");
            }
            else
                return false;
            if (dateTimeToCheck.Minute == System.DateTime.Today.Minute || dateTimeToCheck.Minute + 1 == System.DateTime.Today.Minute)
            {
                Debug.Log($"checked time as {dateTimeToCheck.Minute} minutes - passed check");
            }
            else
                return false;
            return true;
        }
        private void OnApplicationQuit()
        {
            if (signOutOnClose)
            {
                PlayerAccountService.Instance.SignOut();
                AuthenticationService.Instance.SignOut(true);
                AuthenticationService.Instance.ClearSessionToken();
            }
        }
    }
}