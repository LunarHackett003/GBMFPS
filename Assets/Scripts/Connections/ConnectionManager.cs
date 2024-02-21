using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Starlight.Connection
{
    public class ConnectionManager : MonoBehaviour
    {
        internal static ConnectionManager Instance;

        internal Allocation hostAllocation;
        internal JoinAllocation joinAllocation;

        internal Lobby friendParty;
        internal Lobby gameLobby;

        internal bool inLobby, inGame;
        [SerializeField] internal bool localTesting;

        [SerializeField] CanvasGroup gameLobbyCG;
        private void Awake()
        {
            if (Instance)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            ToggleLobbyCG(false);
        }
        internal void ToggleLobbyCG(bool activated)
        {
            gameLobbyCG.gameObject.SetActive(activated);
        }
    }
}