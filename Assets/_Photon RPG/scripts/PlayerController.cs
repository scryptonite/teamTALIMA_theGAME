using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;

public class PlayerController : PunBehaviour {

    //MyNetworkManager networkManager;
    GameManager gameManager;

    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject localPlayer;

    public Transform playerCanvas;
    public Vector3 ScreenOffset = new Vector3(0f, 30f, 0f);
    public float _characterControllerHeight = 0f;
    public Vector3 _targetPosition;

    [Tooltip("The Player's UI GameObject Prefab")]
    public Text txtPlayerUsername;

    void Awake() {
        //networkManager = FindObjectOfType<MyNetworkManager>();
        gameManager = FindObjectOfType<GameManager>();
        
        // keep track of the localPlayer to prevent instantiation when levels are synchronized
        if (photonView.isMine) {
            localPlayer = this.gameObject;
        }
    }

    // Use this for initialization
    void Start () {
        txtPlayerUsername = GetComponentInChildren<Text>();
        //txtPlayerUsername.text = networkManager.playerUsername;
        playerCanvas = transform.Find("Player Canvas");
        txtPlayerUsername.text = photonView.owner.NickName;


		gameObject.name = string.Format("Player \"{0}\"", photonView.owner.NickName.Trim());
		if (photonView.isMine) gameObject.name += " <- You";

        if (photonView.isMine) {
			GetComponent<MeshRenderer>().material.color = new Color(8/255f, 168/255f, 241/255f, 1);
			//GetComponent<MeshRenderer>().material.color = new Color(0x08/ 255f, 0xA8/255f, 0xF1/255f, 1);
			//GetComponent<MeshRenderer>().material.color = new Color32(8, 168, 241, 255);

			var toggle = GameObject.FindWithTag("Rolling Toggle").GetComponent<Toggle>();
			toggle.isOn = rollingMovement = false;
			toggle.onValueChanged.AddListener((enabled) => {
				rollingMovement = toggle.isOn;
			});

			qForward = Quaternion.Euler(0, transform.rotation.y, 0);
		}
	}

	Quaternion qForward;
	public bool rollingMovement = false;

	void Update() {
		if (!photonView.isMine) return;

		var ctrl = Input.GetKey(KeyCode.LeftControl);
		var shift = Input.GetKey(KeyCode.LeftShift);
		var alt = Input.GetKey(KeyCode.LeftAlt);
		var lmb = Input.GetMouseButton(0);
		var rmb = Input.GetMouseButton(1);

		var piv = Input.GetAxis("Pivot") * (ctrl ? 240f : 180f) * Time.deltaTime;
		var rot = Input.GetAxis("Horizontal") * (ctrl ? 360f : 180f) * Time.deltaTime;
		var fwd = -Input.GetAxis("Vertical") * (ctrl ? 5f : shift ? 1f : 3f) * Time.deltaTime;

		qForward *= Quaternion.Euler(0, rot, 0);

		transform.rotation = qForward * Quaternion.Euler(
			transform.rotation.eulerAngles.x,
			0,
			transform.rotation.eulerAngles.z + piv
		);
	

		if (Input.GetKey(KeyCode.H)) {
			transform.rotation = Quaternion.Lerp(
				transform.rotation,
				qForward * Quaternion.Euler(
					transform.rotation.eulerAngles.x,
					0f,
					0f
				),
				Time.deltaTime * 5f
			);
		}

		if (rollingMovement) {
			transform.rotation *= Quaternion.Euler(fwd * 30f, 0, 0);
			transform.Translate(Vector3.forward * fwd);
		} else {
			transform.position += qForward * Vector3.forward * fwd;
		}


		if (rmb || (ctrl && lmb)) {
			RaycastHit hit;
			var ray = Camera.main.ScreenPointToRay(
				Input.mousePosition
			);

			//Debug.DrawRay(ray.origin, ray.direction, Color.magenta);

			if(Physics.Raycast(ray, out hit, 30f, 1 << LayerMask.NameToLayer("World"))) {
				//Debug.DrawLine(ray.origin, hit.point, Color.green);
				//Debug.DrawRay(hit.point, hit.normal * 3f, Color.red);

				transform.position = new Vector3(
					hit.point.x,
					transform.position.y,
					hit.point.z
				);
			}
		}

		if (Input.GetKeyDown(KeyCode.Space)) {
			transform.position += Vector3.up;
		}
		//transform.position += Quaternion.Euler(0, rot, 0) * Vector3.forward * fwd;


		//transform.Rotate(0, y, 0);
		//transform.Translate(0, 0, -z);
	}

	void LateUpdate() {
        playerCanvas.rotation = Camera.main.transform.rotation;
    }

    // in an "observed" script:
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        /*
        if (stream.isWriting) {
            stream.SendNext(transform.position);
        } else {
            this.transform.position = (Vector3)stream.ReceiveNext();
        }
        */
    }

    private void OnTriggerEnter(Collider other) {
        if (!localPlayer) return;
        if (other.CompareTag("Respawn Shield")) Respawn();
    }
    
    public void Respawn() {
        if (!localPlayer) return;

        Debug.Log("Choosing a spawn point.");
        // Default spawn point
        Vector3 spawnPoint = new Vector3(0, 3, 0);

        // If array of spawn points exists, choose a random one
        SpawnPoint[] spawnPoints = gameManager.spawnPoints;
        if (spawnPoints != null && spawnPoints.Length > 0) {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        }

        Debug.Log("Spawn Point chosen: " + spawnPoint);
        transform.position = spawnPoint;
    }
}
