using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class Doorway : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera cam;
    
    public Transform cameraLookAt;

    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        StartCoroutine(CameraSway());
    }

    private void OnTriggerEnter2D(Collider2D collider) {
        // If collided with the player, start next level
        if (collider.CompareTag("Player")) {
            GameStateManager.NextLevel();
        } 
    }

    private IEnumerator CameraSway() {
        cam.Follow = this.transform;
        yield return new WaitForSeconds(2);
        cam.Follow = cameraLookAt;
        WalkerGenerator.doneWithLevel = true;
    }

    public UnityEngine.Object Create(UnityEngine.Object original, Vector3 position, Quaternion rotation, GameObject player) {
        GameObject door = Instantiate(original, position, rotation) as GameObject;
        
        if (door.TryGetComponent<Doorway>(out var doorway)) {
            doorway.cameraLookAt = player.transform;
            Debug.Log("Doorway Spawned!");
            return door;
        } else {
            Debug.LogError("Could not find Doorway script or extension of such on this Object.");
            return null;
        }
    }
}
