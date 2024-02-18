using Starlight.GamingService;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Starlight.PlayerInteraction
{
    public class SocialMenuUI : MonoBehaviour
    {
        [SerializeField] internal RectTransform socialMenu;
        [SerializeField] Vector3 hiddenPosition, shownPosition;
        [SerializeField] float menuPopoutSpeed;
        [SerializeField] bool menuActive;
        internal static SocialMenuUI instance;
        [SerializeField] internal TMP_InputField friendInputField;
        [SerializeField] CanvasGroup backgroundFade;
        internal bool activated;

        [SerializeField, Header("Friends Display")] internal RectTransform friendsRoot;
        [SerializeField] GameObject friendsDisplayPrefab;
        bool subscribedToUpdates;
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
        IEnumerator UpdateFriends()
        {
            while (true)
            {
                yield return new WaitForSeconds(30);
                FriendsService.Instance.ForceRelationshipsRefreshAsync();
                DisplayFriends();
            }
        }
        [SerializeField] internal TextMeshProUGUI usernameText;
        [SerializeField] internal TextMeshProUGUI activityField;
        private void Update()
        {
            if(!subscribedToUpdates && activated)
            {
                FriendsService.Instance.RelationshipAdded += Instance_RelationshipAdded;
                FriendsService.Instance.RelationshipDeleted += Instance_RelationshipDeleted;
                StartCoroutine(UpdateFriends());
                subscribedToUpdates = true;
            }
        }

        private async void Instance_RelationshipDeleted(Unity.Services.Friends.Notifications.IRelationshipDeletedEvent obj)
        {
            await FriendsService.Instance.ForceRelationshipsRefreshAsync();
            DisplayFriends();
        }

        private async void Instance_RelationshipAdded(Unity.Services.Friends.Notifications.IRelationshipAddedEvent obj)
        {
            await FriendsService.Instance.ForceRelationshipsRefreshAsync();
            DisplayFriends();
        }

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
            backgroundFade.gameObject.SetActive(true);
            //Sets the start and end positions of the menu.
            Vector3 start = menuActive ? shownPosition : hiddenPosition,
                end = menuActive ? hiddenPosition : shownPosition;
            while (time < 1)
            {
                //Update the lerp time
                backgroundFade.alpha = menuActive ? 1 - time : time;
                time += Time.smoothDeltaTime * menuPopoutSpeed;
                socialMenu.localPosition = Vector3.Lerp(start, end, time);
                yield return new WaitForEndOfFrame();
            }
            menuActive = !menuActive;
            if (!menuActive)
            {
                socialMenu.gameObject.SetActive(false);
                backgroundFade.gameObject.SetActive(false);
            }
        }
        internal void DisplayFriends()
        {
            if (friendsRoot && friendsDisplayPrefab)
            {
                var relationships = FriendsService.Instance.Relationships;
                for (int i = 0; i < relationships.Count; i++)
                {
                    var fdp = Instantiate(friendsDisplayPrefab, friendsRoot);
                    if(fdp.TryGetComponent(out FriendDisplay fd))
                    {
                        fd.UpdatePlayer(relationships[i].Member);
                    }
                } 
            }
        }
        public void CopyPlayerToClipboard()
        {
            GUIUtility.systemCopyBuffer = activityField.text;
        }
        public async void SendFriendRequest()
        {
            string friendCode = friendInputField.text;
            try
            {
                await FriendsService.Instance.AddFriendByNameAsync(friendCode);
            }
            catch (System.Exception)
            {
                await FriendsService.Instance.AddFriendAsync(friendCode);
            }
        }
        internal async void SetAvailabilty(Availability availability)
        {
            await FriendsService.Instance.SetPresenceAvailabilityAsync(availability);
        }
    }
}