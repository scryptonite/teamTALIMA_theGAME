/*
 * Login script
 * ---
 * Receives login
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Login : Photon.PunBehaviour {


	[Tooltip("UI Dropdown that chooses which server to connect to")]
	public Dropdown serverChoice;
	[Tooltip("UI InputField - Accepts player username")]
    public InputField field;
    [Tooltip("UI Text informing player the connection is in progress")]
    public Text progressLabel;
    [Tooltip("UI Button that triggers the connection")]
    public Button playButton;

    [Tooltip("UI Text informing player the connection is in progress")]
    public PhotonLogLevel Loglevel = PhotonLogLevel.Informational;

    NetworkManager networkManager;

	public struct ServerAddress {
		public string Name;
		public string IPAddress;
		public int Port;
	}
	public List<ServerAddress> serverAddresses = new List<ServerAddress> {
		new ServerAddress { Name = "Amazon EC2 Server", IPAddress = "54.193.101.8", Port = 5055 },
		new ServerAddress { Name = "Local Server", IPAddress = "127.0.0.1", Port = 5055 },
	};

	void Awake() {
		networkManager = NetworkManager.instance;

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
		if (PlayerPrefs.HasKey("username") && PlayerPrefs.GetString("username").Trim().Length > 0) {
			field.text = PlayerPrefs.GetString("username").Trim();
		} else {
			field.text = string.Format("{0}{1}", prefixes[Random.Range(0, prefixes.Length)], Random.Range(0, 1000000).ToString().PadLeft(6, '0'));
		}

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
				PlayerLogin();
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

		var settings = (ServerSettings)Resources.Load(PhotonNetwork.serverSettingsAssetFile, typeof(ServerSettings));
		serverChoice.options.Clear();
		serverChoice.options.AddRange(serverAddresses.Select(address =>
			new Dropdown.OptionData(address.Name != null ? address.Name : address.IPAddress)
		));
		serverChoice.value = serverAddresses.FindIndex(address =>
			address.IPAddress == settings.ServerAddress
			&& address.Port == settings.ServerPort);
		serverChoice.onValueChanged.Invoke(serverChoice.value);
		serverChoice.onValueChanged.AddListener(choice => {
			if (settings != null) {
				settings.ServerAddress = serverAddresses[choice].IPAddress;
				settings.ServerPort = serverAddresses[choice].Port;
			} else {
				serverChoice.options.Clear();
				serverChoice.options.Add(new Dropdown.OptionData("Unable to change server"));
				serverChoice.interactable = false;
			}
		});
	}
	void Start() {
        FocusField();
        field.onValueChanged.Invoke(field.text);
    }

    void FocusField() {
        EventSystem.current.SetSelectedGameObject(field.gameObject, null);
        field.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    // Pass inputed username to NetworkManager to instantiate new player - Button.onClick()
    // void PassUsername() {
    // }

    // void Update() {
    //     // fire Submit event when InputField is not focused
    //     if (!field.isFocused && field.text != "" && Input.GetButtonDown("Submit")) {
    //         PlayerLogin();
    //     }
    // }

    // // fire Submit event when InputField is focused
    // void ISubmitHandler.OnSubmit(BaseEventData eventData) {
    //     PlayerLogin();
    // }

    // Pass inputed username to NetworkManager to instantiate new player - Button.onClick()
    public void PlayerLogin() {
        var username = field.text.Trim();
        username = username.Substring(0, Mathf.Min(username.Length, 32));
		field.text = username;
		//networkManager.ReceiveUsername(username);
		// Connect();
		PhotonNetwork.playerName = username;
		Debug.Log("<Color=Blue>PlayerLogin()</Color> -- We call Connect()");

        // PhotonNetwork.playerName = (field.text + " ");
        // progressLabel.SetActive(true);
        // Debug.Log("<Color=Blue>PlayerLogin()</Color> -- We call Connect()");
        networkManager.Connect();
    }

	public override void OnConnectedToPhoton() {
		PlayerPrefs.SetString("username", PhotonNetwork.playerName);
		PlayerPrefs.Save();
	}

	//IEnumerator SetActiveLater(GameObject go, float delay, bool active = true) {
	//	yield return new WaitForSecondsRealtime(delay);
	//	go.SetActive(active);
	//}
	public override void OnDisconnectedFromPhoton() {
		progressLabel.gameObject.SetActive(true);
		progressLabel.text = "<color=red>Disconnected</color>";
		//StartCoroutine(SetActiveLater(progressLabel.gameObject, 1f, false));
		
		field.interactable = true;
		playButton.interactable = true;

		Debug.LogWarning("<Color=Red>OnDisconnectedFromPhoton()</Color>");
    }
}
