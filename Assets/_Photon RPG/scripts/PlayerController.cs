using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		Debug.LogFormat("User '{0}' appeared!", photonView.owner.NickName);


		gameObject.name = string.Format("Player \"{0}\"", photonView.owner.NickName.Trim());
		if (photonView.isMine) gameObject.name += " <- You";

        if (photonView.isMine) {
			GetComponent<MeshRenderer>().material.color = new Color(8/255f, 168/255f, 241/255f, 1);
			//GetComponent<MeshRenderer>().material.color = new Color(0x08/ 255f, 0xA8/255f, 0xF1/255f, 1);
			//GetComponent<MeshRenderer>().material.color = new Color32(8, 168, 241, 255);

			var rollingToggle = GameObject.FindWithTag("Rolling Toggle").GetComponent<Toggle>();
			rollingToggle.isOn = rollingMovement = false;
			rollingToggle.onValueChanged.AddListener((enabled) => {
				rollingMovement = rollingToggle.isOn;
			});

			qForward = Quaternion.Euler(0, transform.rotation.y, 0);


			var aiToggle = GameObject.FindWithTag("AI Control Toggle").GetComponent<Toggle>();
			aiToggle.isOn = aiMovement = false;
			aiToggle.onValueChanged.AddListener((enabled) => {
				aiMovement = aiToggle.isOn;
			});

			StartCoroutine(AIController());
		}
	}

	IEnumerator AIController() {
		while (true) {
			yield return null;
			if (!aiMovement) continue;

			var players = new List<PlayerController>();
			players.AddRange(
				GameObject.FindObjectsOfType<PlayerController>().Where(
					player => !player.photonView.isMine
				)
			);
			var action = Random.Range(0, 4);

			//if (action == 0 && players.Count == 0)
			//	action = 1;

			if (players.Count == 0) continue;

			switch (action) {
				case 0: { // flee
						var player = players[Random.Range(0, players.Count)];
						var start = Time.time;
						var time = Random.Range(10f, 15f);
						while (true) {
							yield return null;
							var duration = Time.time - start;
							if (duration > time
							|| player == null || player.transform == null) {
								yield return new WaitForSeconds(0.5f);
								break;
							}
							if (!aiMovement) break;
							transform.rotation = Quaternion.Lerp(
								transform.rotation,
								Quaternion.LookRotation(
									Vector3.Scale((transform.position - player.transform.position).normalized, new Vector3(1f, 0f, 1f)),
									Vector3.up
								),
								180f * Time.deltaTime
							);
							var distance = (transform.position - player.transform.position).magnitude;
							if (distance < 3f) {
								//time = Mathf.Max(time, duration + 5f);
								var speed = 1.5f + ((3f - distance)/3f)*2f;
								transform.Translate(-Vector3.back * Time.deltaTime * speed);
							}
						}
					}
					break;
				case 1: { // follow
						var player = players[Random.Range(0, players.Count)];
						var start = Time.time;
						var time = Random.Range(10f, 15f);
						while (true) {
							yield return null;
							var duration = Time.time - start;
							if (duration > time
							|| player == null || player.transform == null) {
								yield return new WaitForSeconds(0.5f);
								break;
							}
							if (!aiMovement) break;
							transform.rotation = Quaternion.Lerp(
								transform.rotation,
								Quaternion.LookRotation(
									Vector3.Scale((transform.position - player.transform.position).normalized, new Vector3(1f, 0f, 1f)),
									Vector3.up
								),
								180f * Time.deltaTime
							);
							var distance = (transform.position - player.transform.position).magnitude;
							if (distance > 2f) {
								//time = Mathf.Max(time, duration + 5f);
								transform.Translate(-Vector3.forward * Time.deltaTime * distance);
							} else {
								//time = Mathf.Max(time, duration + 5f);
								transform.Translate(Vector3.forward * Time.deltaTime * distance);
							}
						}
					}
					break;
				case 2: { // wander
						var target2d = new Vector2(Random.Range(-4, 4), Random.Range(-4, 4));
						while (true) {
							yield return null;
							if (!aiMovement) break;
							var target3d = new Vector3(
								target2d.x,
								transform.position.y > -50 ? transform.position.y : 50,
								target2d.y
							);
							//transform.rotation = Quaternion.Lerp(
							//	transform.rotation,
							//	Quaternion.Euler(0, Vector2.Angle(new Vector2(transform.position.x, transform.position.z), target2d), 0),
							//	180f * Time.deltaTime
							//);

							var targetQ = Quaternion.LookRotation(
								transform.position - target3d,
								Vector3.up
							);

							float movespeed = Mathf.Abs(Mathf.DeltaAngle(transform.rotation.eulerAngles.y, targetQ.eulerAngles.y));
							movespeed = Mathf.Max(0f, ((180f - movespeed) / 180f) - 0.5f) * 2f;

							transform.rotation = Quaternion.Lerp(
								transform.rotation,
								targetQ,
								3.5f * Time.deltaTime
							);

							transform.position = Vector3.MoveTowards(
								transform.position,
								target3d,
								movespeed * Time.deltaTime
							);

							if ((transform.position - target3d).magnitude < 0.01) {
								yield return new WaitForSeconds(Random.Range(.2f, 0.5f));
								break;
							}
						}
					}
					break;
				case 3: { // head upside down
						var duration = 5f;
						while (true) {
							yield return null;
							if (!aiMovement) break;
							transform.rotation = Quaternion.Lerp(
								transform.rotation,
								Quaternion.Euler(
									transform.rotation.eulerAngles.x,
									transform.rotation.eulerAngles.y,
									180f
								),
								180f * Time.deltaTime
							);
							duration -= Time.deltaTime;
							if (duration < 0f) break;
						}

						}
						break;

			}
		}
	}

	Quaternion qForward;
	public bool rollingMovement = false;
	public bool aiMovement = false;

	void Update() {
		if (!photonView.isMine) return;

		if (aiMovement) {
			return;
		}

		var ctrl = Input.GetKey(KeyCode.LeftControl);
		var shift = Input.GetKey(KeyCode.LeftShift);
		var alt = Input.GetKey(KeyCode.LeftAlt);
		var lmb = Input.GetMouseButton(0);
		var rmb = Input.GetMouseButton(1);
		var w = Input.GetKey(KeyCode.W);

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

		if (ctrl && alt && w) {
			transform.Translate(-Vector3.forward * 20f * Time.deltaTime);
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
				
				if(transform.position.y < -15) {
					GetComponent<Rigidbody>().velocity = Vector3.zero;
					GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
				}
				//var src = transform.position;

				var dest = new Vector3(
					hit.point.x,
					transform.position.y >= -15 ? transform.position.y : 2,
					hit.point.z
				);
				transform.position = dest;

				//var y = Quaternion.LookRotation(Vector3.Scale(src - dest, new Vector3(1, 0, 1)), Vector3.up).eulerAngles.y;

				//transform.rotation = Quaternion.Lerp(
				//	transform.rotation,
				//	Quaternion.Euler(
				//		transform.rotation.eulerAngles.x,
				//		y,
				//		transform.rotation.eulerAngles.z
				//	),
				//	180f*Time.deltaTime
				//);
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
