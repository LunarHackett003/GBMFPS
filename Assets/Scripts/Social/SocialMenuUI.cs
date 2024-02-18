using Starlight.GamingService;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Starlight.PlayerInteraction
{
    public class SocialMenuUI : MonoBehaviour
    {
        [SerializeField] internal RectTransform socialMenu;
        [SerializeField] Vector3 hiddenPosition, shownPosition;
        [SerializeField] float menuPopoutSpeed;
        [SerializeField] bool menuActive;
        internal static SocialMenuUI instance;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(this);
            }
        }
        [SerializeField] internal TextMeshProUGUI usernameText;
        [SerializeField] internal TextMeshProUGUI idText;
        public void ToggleSocialMenu()
        {
            //Just in case its been updated, we also want to check the player name
            usernameText.text = AuthManager.Instance.username;
            StartCoroutine(LerpSocialMenu());
        }
        IEnumerator LerpSocialMenu()
        {
            float time = 0;
            socialMenu.gameObject.SetActive(true);
            //Sets the start and end positions of the menu.
            Vector3 start = menuActive ? shownPosition : hiddenPosition,
                end = menuActive ? hiddenPosition : shownPosition;
            while (time < 1)
            {
                //Update the lerp time
                time += Time.smoothDeltaTime * menuPopoutSpeed;
                socialMenu.localPosition = Vector3.Lerp(start, end, time);
                yield return new WaitForEndOfFrame();
            }
            menuActive = !menuActive;
            if (!menuActive)
                socialMenu.gameObject.SetActive(false);
        }
        public void CopyPlayerToClipboard()
        {
            GUIUtility.systemCopyBuffer = idText.text;
        }
    }
}