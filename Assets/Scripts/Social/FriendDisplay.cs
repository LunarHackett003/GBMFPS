using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using UnityEngine;

namespace Starlight.PlayerInteraction {
    public class FriendDisplay : MonoBehaviour
    {
        internal string playerName, playerID;
        internal string availability, activity;
        [SerializeField] internal TextMeshProUGUI tmp;

        internal void UpdatePlayer(Member member)
        {
            playerName = member.Profile.Name;
            availability = GetStringFromAvailability(member.Presence.Availability);
            tmp.text = $"{playerName}\n" +
                $"{availability}";
        }
        internal string GetStringFromAvailability(Availability av)
        {
            switch (av)
            {
                case Availability.Unknown:
                    return "Unknown";
                case Availability.Online:
                    return "Online";
                case Availability.Busy:
                    return "Busy";
                case Availability.Away:
                    return "Away";
                case Availability.Invisible:
                    return "Offline";
                case Availability.Offline:
                    return "Offline";
                default:
                    return "Unknown";
            }
        }
    }
}