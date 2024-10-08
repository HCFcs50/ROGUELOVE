using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera mainCamera;
    [Range(2, 100)] [SerializeField] private float cameraTargetDivider;
 
    private void Update()
    {
        if (GameStateManager.GetState() != GAMESTATE.MENU) {
            var mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var cameraTargetPosition = (mousePosition + (cameraTargetDivider - 1) * playerTransform.position) / cameraTargetDivider;
            transform.position = cameraTargetPosition;
        }
    }
}
