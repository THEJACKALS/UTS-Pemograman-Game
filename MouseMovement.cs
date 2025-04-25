using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMovement : MonoBehaviour
{
    public float mouseSensivity = 500f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = -90f;
    public float bottomClamp = 90f;

    void Start()
    {
        //locking cursor to middle, not visible
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        //getting the mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensivity * Time.deltaTime;

        //rotating the camera up and down
        xRotation -= mouseY;
        
        //clamping the rotation
        xRotation = Mathf.Clamp(xRotation, topClamp, bottomClamp);
        
        //rotating the player
        yRotation += mouseX;
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
