using Eflatun.SceneReference;
using Starlight.InputHandling;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] internal int maxPlayers;

        [SerializeField] internal SceneReference menuScene;
        [SerializeField] internal SceneReference[] maps;


        [SerializeField] internal string remotePlayerLayer;
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
        public async void CreateGame()
        {
            print("Creating game");
            try
            {
                var alloc = await Relay.Instance.CreateAllocationAsync(maxPlayers);
                string joincode = await Relay.Instance.GetJoinCodeAsync(alloc.AllocationId);
                try
                {
                    int mapIndex = Random.Range(0, maps.Length);
                    CreateLobbyOptions clo = new()
                    {
                        Data = new()
                    {
                        {"rjc", new(DataObject.VisibilityOptions.Member, alloc.AllocationId.ToString()) },
                        {"map", new(DataObject.VisibilityOptions.Member, maps[mapIndex].Name) }
                    },
                        IsPrivate = false,
                        IsLocked = false
                    };
                    Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(AuthenticationService.Instance.PlayerId + "_s-Game", maxPlayers, clo);
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                        alloc.RelayServer.IpV4,
                        (ushort)alloc.RelayServer.Port,
                        alloc.AllocationIdBytes,
                        alloc.Key,
                        alloc.ConnectionData);
                    NetworkManager.Singleton.StartHost();
                    NetworkManager.Singleton.SceneManager.LoadScene(maps[mapIndex].Name, LoadSceneMode.Single);
                    hostAllocation = alloc;
                    gameLobby = lobby;
                    InputHandler.instance.joinCodeDisplay.text = lobby.LobbyCode;
                    inGame = true;
                }
                catch (System.Exception)
                {

                    throw;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            
                


        }
        public async void TryJoinRandomGame()
        {
            print("trying to join");
            try
            {
                print("finding lobby");
                gameLobby = await Lobbies.Instance.QuickJoinLobbyAsync();
                
                print("joining lobby from code");
                string jc = gameLobby.Data["rjc"].Value;
                string mapname = gameLobby.Data["map"].Value;
                string joincode = await Relay.Instance.GetJoinCodeAsync(System.Guid.Parse(jc));
                var jAlloc = await Relay.Instance.JoinAllocationAsync(joincode);
                joinAllocation = jAlloc;
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                    jAlloc.RelayServer.IpV4,
                    (ushort)jAlloc.RelayServer.Port,
                    jAlloc.AllocationIdBytes,
                    jAlloc.Key,
                    jAlloc.ConnectionData,
                    jAlloc.HostConnectionData);
                NetworkManager.Singleton.StartClient();
                inGame = true;
                InputHandler.instance.Unpause();
            }
            catch
            {
                throw;
            }
            finally
            {
                if(!inGame)
                    CreateGame();
            }

            InputHandler.instance.joinCodeDisplay.text = gameLobby.LobbyCode;
            //We could not find a game, so we'll make one instead.
        }
        public async void TryQuitGame()
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(menuScene.Name);
            await Lobbies.Instance.RemovePlayerAsync(gameLobby.Id, AuthenticationService.Instance.PlayerId);
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(menuScene.Name);
            hostAllocation = null;
            joinAllocation = null;
            gameLobby = null;
            inGame = false;
        }
    }
}