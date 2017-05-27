using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon;

public class Launcher : PunBehaviour {

    [Tooltip("UI InputField for player username")]
    public InputField field;
	[Tooltip("UI Text informing player the connection is in progress")]
	public Text progressLabel;
	[Tooltip("UI Button for submitting the player username to the server")]
	public Button playButton;


	public PhotonLogLevel Loglevel = PhotonLogLevel.Informational;

    MyNetworkManager networkManager;
    string _gameVersion = "0.0.2";
    bool isConnecting;

    void Awake() {
        // #Critical, auto-set in PhotonServerSettings. We don't join a lobby
        PhotonNetwork.autoJoinLobby = false;

        // #Critical, we can use PhotonNetwork.LoadLevel() on the master client and all clients in the room sync their level automatically
        PhotonNetwork.automaticallySyncScene = true;

        // #NotImportant, force LogLevel
        PhotonNetwork.logLevel = Loglevel;
    }

    void Start() {
        networkManager = FindObjectOfType<MyNetworkManager>();

		var prefixes = new string[] {
			"Kappa",
			"PogChamp",
			"Keepo",
			"FeelsGoodMan",
			"FeelsBadMan",
			"SwiftRage",
			"teamTALIMA",
			"Beastie",
			"MrDestructoid",
			"OhMyDog",
			"ResidentSleeper",
			"CoolStoryBob",
			"CoolCat",
			"\u0295\u2022\u1d25\u2022\u0294",
			"VoHiYo",
			"RaccAttack"
		};
		field.text = string.Format("{0}{1}", prefixes[Random.Range(0, prefixes.Length)], Random.Range(0, 1000000).ToString().PadLeft(6, '0'));

		progressLabel.gameObject.SetActive(false);
		field.onValueChanged.AddListener((str) => {
			var text = field.text.Trim();
			var submittable = text.Length > 0 && text.Length <= 32;
			playButton.interactable = field.interactable && submittable;
		});
		field.interactable = true;

		playButton.onClick.AddListener(() => {
			if (playButton.interactable) {
				field.interactable = false;
				playButton.interactable = false;
				PassUsername();
			}
		});
		playButton.interactable = false;

		field.onEndEdit.AddListener((result) => {
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
				field.onValueChanged.Invoke(result);
				playButton.onClick.Invoke();
				FocusField();
			}
		});

		FocusField();
		field.onValueChanged.Invoke(field.text);
	}

	void FocusField() {
		EventSystem.current.SetSelectedGameObject(field.gameObject, null);
		field.OnPointerClick(new PointerEventData(EventSystem.current));
	}

	// Pass inputed username to NetworkManager to instantiate new player - Button.onClick()
	void PassUsername() {
		var username = field.text.Trim();
		username = username.Substring(0, Mathf.Min(username.Length, 32));
		networkManager.ReceiveUsername(username);
        Connect();
        Debug.Log("<Color=Blue>PassUsername()</Color> -- We call Connect()");
    }

    ///<summary>
    /// Start the connection process. If connected load 'Level 1', else connect to Photon Cloud Network.
    /// </summary>
    public void Connect() {
        isConnecting = true;
        progressLabel.gameObject.SetActive(true);
        Debug.Log("<Color=Blue>Connect()</Color> -- isConnecting was just set to: " + isConnecting);

        // are we connected
        if (PhotonNetwork.connected) {
            // join/create room 'Level 1'
            PhotonNetwork.JoinOrCreateRoom("Level 1", new RoomOptions { MaxPlayers = 14 }, null);
            Debug.Log("<Color=Blue>Connect()</Color> -- called JoinRoom('Level 1')");
        } else {
            // connect to Photon Online Server
            PhotonNetwork.ConnectUsingSettings(_gameVersion);
        }
    }

    public override void OnConnectedToMaster() {
        Debug.Log("<Color=Blue>OnConnectedToMaster()</Color>");

        // isConnecting is false typically when you lost or quit the game
        if (isConnecting) {
            // join/create room 'Level 1'
            PhotonNetwork.JoinOrCreateRoom("Level 1", new RoomOptions { MaxPlayers = 14 }, null);
            Debug.Log("<Color=Blue>OnConnectedToMaster()</Color> -- called JoinRoom('Level 1')");
        }
    }

    public override void OnPhotonJoinRoomFailed(object[] codeAndMsg) {
        // PhotonNetwork.CreateRoom("Level 1", new RoomOptions() { MaxPlayers = 14 }, null);
        Debug.Log("<Color=Blue>OnPhotonJoinRoomFailed()</Color> -- we failed to join room 'Level 1'");
    }

    public override void OnJoinedRoom() {
        Debug.Log("<Color=Blue>OnJoinedRoom()</Color> -- now this client is in a room.");

        // #Critical, if we are the first player load level, else rely on PhotonNetwork.automaticallySyncScene to sync our instance scene
        if (PhotonNetwork.room.PlayerCount == 1) {
            // #Critical, load the level
            PhotonNetwork.LoadLevel("Level 1");
        } else {
            Debug.Log("<Color=Blue>OnJoinedRoom()</Color> -- no Level loaded because there is more than 1 player here");
        }
    }

    public override void OnDisconnectedFromPhoton() {
        Debug.LogWarning("<Color=Red>OnDisconnectedFromPhoton()</Color>");
		progressLabel.gameObject.SetActive(false);
    }
}
