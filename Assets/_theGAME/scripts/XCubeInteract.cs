using UnityEngine;

public class XCubeInteract : MonoBehaviour {

	void Interact() {
        Debug.Log("interacting!");
        GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
}
