using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Michsky.UI.Dark;
using TMPro;
using UnityEngine.UI;   
using Photon.Realtime;
using System.Linq;

public class Launcher : MonoBehaviourPunCallbacks
{

    public static Launcher Instance;

    [SerializeField] TMP_InputField lobbyNameInputField;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text errorMessage;
    [SerializeField] TMP_Text LobbyPanelLobbyName;
    [SerializeField] TMP_Text LobbyPanelMapName;

    [SerializeField] Transform roomListContent;
    [SerializeField] Transform roomListContent1;
    [SerializeField] GameObject roomListItemPrefab;

    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;
    [SerializeField] TMP_InputField playerUserName;

    public ModalWindowManager CreateServerError;
    public ModalWindowManager CreateServerErrorMessage;

    public CustomDropdown dropdown;

    public GameObject serverButton;

    public string gm;
    public string un;

    private LoadBalancingClient loadBalancingClient;

    void Start()
    {
        Debug.Log("Connecting to the Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    void Awake()
    {
        Instance = this;
        
    }

    public void OnValueChanged(string input)
    {
        PhotonNetwork.NickName = input;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to the Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        //MainPanelManager.Instance.OpenFirstTab();
        Debug.Log("Joined the Lobby");
        if (!string.IsNullOrEmpty(playerUserName.text))
        {
            OnValueChanged(playerUserName.text);
        }
        else
        {
            PhotonNetwork.NickName = "Player " + Random.Range(0, 1000).ToString("0000");
        }
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(lobbyNameInputField.text))
        {
            CreateServerError.ModalWindowIn();
            Debug.Log("Empty Name");
            return;
        }
        Debug.Log("Room Created with the name of " + lobbyNameInputField.text + " for the Game: " + dropdown.selectedText.text);
        RoomOptions newRoomOptions = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = 20 };
        //newRoomOptions.CustomRoomPropertiesForLobby = { GMKey };
        //newRoomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { GMKey, 1 } };
        gm = dropdown.selectedText.text;
        newRoomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
        newRoomOptions.CustomRoomProperties.Add("GM", gm);
        newRoomOptions.CustomRoomPropertiesForLobby = new string[] { "GM" };
        PhotonNetwork.CreateRoom(lobbyNameInputField.text, newRoomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        MainPanelManager.Instance.PanelAnim(5);
        LobbyPanelLobbyName.text = PhotonNetwork.CurrentRoom.Name;
        LobbyPanelMapName.text = (string)PhotonNetwork.CurrentRoom.CustomProperties["GM"];

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Count(); i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<UserNameList>().SetUp(players[i]);
        }

        Debug.Log((string)PhotonNetwork.CurrentRoom.CustomProperties["GM"]);
        Debug.Log("Joined User Room");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CreateServerErrorMessage.ModalWindowIn();
        errorMessage.text = message + "\nError Code: " + returnCode;
        Debug.LogError("Lobby Creation Failed" + message + returnCode);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        Debug.Log("Leaving Room");
    }

    public void JoinRoom(RoomInfo info)
    {
        //gm = dropdown.selectedText.text;
        //info.CustomProperties.Add("GM", gm);
        //un = playerUserName.text;
        //info.CustomProperties.Add("UN", un);
        PhotonNetwork.JoinRoom(info.Name);
        // TODO: Implement a waiting menu
    }

    public override void OnLeftRoom()
    {
        MainPanelManager.Instance.PanelAnim(0);
        Debug.Log("Left Room");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }

        RoomInfo _information;

        for(int i = 0; i < roomList.Count; i++)
        {
            _information = roomList[i];
            //Debug.Log(_information);
            if((string)_information.CustomProperties["GM"] == "LOST IN THE DARK")
            {
                Instantiate(roomListItemPrefab, roomListContent).GetComponent<ServerNameButton>().Setup(roomList[i]);
            }
            else if ((string)_information.CustomProperties["GM"] == "PARKOUR FPS")
            {
                Instantiate(roomListItemPrefab, roomListContent1).GetComponent<ServerNameButton>().Setup(roomList[i]);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<UserNameList>().SetUp(newPlayer);
    }
}
