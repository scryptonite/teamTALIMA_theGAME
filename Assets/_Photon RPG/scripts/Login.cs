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

public class Login : Photon.PunBehaviour {

    [Tooltip("UI InputField - Accepts player username")]
    public InputField field;
    [Tooltip("UI Text informing player the connection is in progress")]
    public Text progressLabel;
    [Tooltip("UI Text informing player the connection is in progress")]
    public Button playButton;

    [Tooltip("UI Text informing player the connection is in progress")]
    public PhotonLogLevel Loglevel = PhotonLogLevel.Informational;

    NetworkManager networkManager;

    void Awake() {
        networkManager = NetworkManager.instance;
    }

    void Start() {
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
		//networkManager.ReceiveUsername(username);
		// Connect();
		PhotonNetwork.playerName = username;
		Debug.Log("<Color=Blue>PlayerLogin()</Color> -- We call Connect()");

        // PhotonNetwork.playerName = (field.text + " ");
        // progressLabel.SetActive(true);
        // Debug.Log("<Color=Blue>PlayerLogin()</Color> -- We call Connect()");
        networkManager.Connect();
    }

    public override void OnDisconnectedFromPhoton() {
		progressLabel.gameObject.SetActive(false);
        Debug.LogWarning("<Color=Red>OnDisconnectedFromPhoton()</Color>");
    }
}
