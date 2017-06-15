using System.Linq;
using UnityEngine;
using UI = UnityEngine.UI;

class XPlayerController : Photon.PunBehaviour {
    public Canvas canvas;
    public UI.Text indicator;
    RectTransform indicatorTransform;
    CanvasRenderer indicatorRenderer;

    static XPlayerController instance;
    public bool isMaster {
        get { return photonView.isMine && instance == this; }
    }

    UI.InputField chatField;

    public float playerReach = 3f;
    [Range(0f, 1f)]
    public float interactionDirectionWeight = 0.8f;

    void OnValidation() {
        interactionDirectionWeight = Mathf.Clamp01(interactionDirectionWeight);
    }

    public string _name = " ";
    static GameObject _go;

    void Awake() {
        _name = PhotonNetwork.playerName;
        if (!photonView.isMine) {
            Destroy(this);
            return;
        }

        if (instance == null) {
            instance = this;
        } else return;

        if (canvas == null) canvas = GameObject.FindGameObjectWithTag("Main Canvas").GetComponent<Canvas>();
        if (chatField == null) chatField = GameObject.FindGameObjectWithTag("Username Input").GetComponent<UI.InputField>();

        if (indicator == null) indicator = GameObject.FindGameObjectWithTag("Interaction Indicator").GetComponent<UI.Text>();
        indicatorTransform = indicator.GetComponent<RectTransform>();
        indicatorRenderer = indicator.GetComponent<CanvasRenderer>();

        indicatorRenderer.SetAlpha(0f);
    }

    bool GetInteractables(out Collider[] interactables) {
        float sqrReach = playerReach * playerReach;
        interactables = Physics.OverlapSphere(transform.position, playerReach, 1 << LayerMask.NameToLayer("Interact")).Where(collider => {
            float sqrMagnitude = (collider.transform.position - transform.position).sqrMagnitude;
            return sqrMagnitude <= sqrReach;
        }).OrderBy(collider => {
            var sqrMagnitude = (collider.transform.position - transform.position).sqrMagnitude;
            sqrMagnitude *= (1f - interactionDirectionWeight) + interactionDirectionWeight * (Vector3.Angle(transform.forward, collider.transform.position - transform.position) / 180f);
            return sqrMagnitude;
        }).ToArray();
        return interactables.Length > 0;
    }

    bool GetClosestInteractable(out Collider closest) {
        Collider closestFound = null;
        float sqrReach = playerReach * playerReach;
        float distance = sqrReach;
        Collider[] colliders = Physics.OverlapSphere(transform.position, playerReach, 1<<LayerMask.NameToLayer("Interact"));
        foreach (Collider collider in colliders) {
            float sqrMagnitude = (collider.transform.position - transform.position).sqrMagnitude;
            if (sqrMagnitude > sqrReach) continue;
            sqrMagnitude *= (1f - interactionDirectionWeight) + interactionDirectionWeight * (Vector3.Angle(transform.forward, collider.transform.position - transform.position) / 180f);
            if (sqrMagnitude < distance) {
                closestFound = collider;
                distance = sqrMagnitude;
            }
        };
        closest = closestFound;
        return closestFound != null;
    }

    Vector3 closestPosition = Vector3.zero;

    void Update() {
        if (!isMaster) return;

        if(Input.GetKeyDown(KeyCode.Return)) {
            //if(PhotonNetwork.isMasterClient)
            //    PhotonNetwork.InstantiateSceneObject("teamTALIMA Player", Vector3.up, Quaternion.identity, 0, null);
            //else
            if (_go != null) PhotonNetwork.Destroy(_go);
            PhotonNetwork.playerName = chatField.text;
            _go = PhotonNetwork.Instantiate("teamTALIMA Player", transform.position, transform.rotation, 0);
        }

        float indicatorAlpha = indicatorRenderer.GetAlpha();
        Collider closest;
        if (GetClosestInteractable(out closest)) {
            if (indicator.enabled == false) {
                indicatorAlpha = 0f;
                closestPosition = closest.transform.position;
            }
            indicatorAlpha = Mathf.Lerp(indicatorAlpha, 1f, Time.deltaTime * 16f);
            if(indicatorAlpha > 0) indicator.enabled = true;
            closestPosition = Vector3.Lerp(closestPosition, closest.transform.position + (Vector3.up*.5f), Time.deltaTime * 8f);

            if (Input.GetButtonDown("Interact"))
                closest.SendMessage("Interact");
        } else {
            if(indicator.enabled) 
                closestPosition = Vector3.Lerp(closestPosition, closestPosition + Vector3.down, Time.deltaTime * 2f);
            indicatorAlpha = Mathf.Lerp(indicatorAlpha, 0f, Time.deltaTime * 16f);
            if(indicatorAlpha <= 0.001) indicator.enabled = false;
        }
        if (indicator.enabled) {
            indicatorTransform.anchoredPosition = canvas.WorldToCanvasPosition(closestPosition);
            indicatorRenderer.SetAlpha(indicatorAlpha);
        }
    }

    void OnDrawGizmosSelected() {
        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        int m = 32;
        float[] scale = new float[m];
        for(int i = 0; i < m; i++) {
            float p = i / (float)m;
            scale[i] = (1f - interactionDirectionWeight) + interactionDirectionWeight*(Vector3.Angle(Vector3.forward, Quaternion.Euler(0, p * 360f, 0) * Vector3.forward) / 180f);
            //scale[i] = 2f;
        }
        
        var white = new Color(1f, 1f, 1f, 0.5f);
        var orange_1_3 = new Color(1f, 0.6f, 0f, 0.25f);
        var orange_2_3 = new Color(1f, 0.6f, 0f, 0.75f);
        var orange_3_3 = new Color(1f, 0.6f, 0f, 1.00f);

        for (int i = 0; i < m; i++) {
            int k = (i + 1) % m;
            float p = i / (float)m, q = k / (float)m;
            Vector3 forward = Vector3.forward;

            Gizmos.color = orange_1_3;
            Gizmos.DrawLine(
                Quaternion.AngleAxis(p * 360f, Vector2.up) * forward * scale[i] * playerReach * .5f,
                Quaternion.AngleAxis(q * 360f, Vector2.up) * forward * scale[k] * playerReach * .5f
            );

            Gizmos.color = orange_2_3;
            Gizmos.DrawLine(
                Quaternion.AngleAxis(p * 360f, Vector2.up) * forward * scale[i] * playerReach * .75f,
                Quaternion.AngleAxis(q * 360f, Vector2.up) * forward * scale[k] * playerReach * .75f
            );

            Gizmos.color = orange_3_3;
            Gizmos.DrawLine(
                Quaternion.AngleAxis(p * 360f, Vector2.up) * forward * scale[i] * playerReach,
                Quaternion.AngleAxis(q * 360f, Vector2.up) * forward * scale[k] * playerReach
            );

            Gizmos.color = white;
            Gizmos.DrawLine(
                Quaternion.AngleAxis(p * 360f, Vector2.up) * forward * playerReach,
                Quaternion.AngleAxis(q * 360f, Vector2.up) * forward * playerReach
            );
        }

        Collider[] interactables;
        if(GetInteractables(out interactables)) {
            int k = 0;
            foreach(var interactable in interactables) {
                var delta = interactable.transform.position - transform.position;
                delta = Quaternion.Inverse(transform.rotation) * delta;
                var distanceScale = (1f - interactionDirectionWeight) + interactionDirectionWeight * (Vector3.Angle(transform.forward, delta) / 180f);
                //distanceScale *= playerReach;

                Gizmos.color = white;
                Gizmos.DrawWireSphere(delta, 0.05f + (.1f - (k / (float)interactables.Length) * .1f));

                int n = 3;
                for(int i = 0; i < n; i++) {
                    float p = i / (float)n;
                    float q = (i+1) / (float)n;
                    Gizmos.color = Color.Lerp(white, orange_2_3, Mathf.Lerp(p, q, .5f));
                    Gizmos.DrawLine(
                        Vector3.Lerp(delta, delta * distanceScale, p),
                        Vector3.Lerp(delta, delta * distanceScale, q)
                    );
                }

                Gizmos.color = orange_2_3;
                Gizmos.DrawWireSphere(delta * distanceScale, 0.1f);
                k++;

            }
        }
        //Gizmos.DrawWireSphere(Vector3.zero, playerReach);

        Gizmos.matrix = matrix;
    }

    [ContextMenu("DESTROY")]
    public void DESTROY() {
        PhotonNetwork.Destroy(photonView);
    }
}

/// <summary>
/// Gives all Canvas instances a new method: WorldToCanvasPosition(position, camera);
/// </summary>
public static class CanvasExtensions {
    public static Vector2 WorldToCanvasPosition(this Canvas canvas, Vector3 position, Camera camera = null) {
        Vector2 screenPosition = (camera == null ? Camera.main : camera).WorldToScreenPoint(position);
        RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
        Vector2 canvasSizeDelta = canvasRectTransform.sizeDelta;
        Vector2 canvasPivot = canvasRectTransform.pivot;

        screenPosition.x /= canvas.scaleFactor;
        screenPosition.y /= canvas.scaleFactor;

        screenPosition.x -= canvasSizeDelta.x * .5f;
        screenPosition.y -= canvasSizeDelta.y * .5f;

        return screenPosition;
    }
}
