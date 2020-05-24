#region

using System;
using UnityEngine;

#endregion

/// <summary>
///     A simple free camera to be added to a Unity game object.
///     Keys:
///     wasd / arrows	- movement
///     q/e 			- up/down (local space)
///     r/f 			- up/down (world space)
///     pageup/pagedown	- up/down (world space)
///     hold shift		- enable fast movement mode
///     right mouse  	- enable free look
///     mouse			- free look / rotation
/// </summary>
public class FreeCam : MonoBehaviour
{
    public Camera Camera;

    /// <summary>
    ///     Set to true when free looking (on right mouse button).
    /// </summary>
    private bool _Looking;

    /// <summary>
    ///     Speed of camera movement when shift is held down,
    /// </summary>
    public float FastMovementSpeed = 100f;

    /// <summary>
    ///     Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float FastZoomSensitivity = 50f;

    /// <summary>
    ///     Sensitivity for free look.
    /// </summary>
    public float FreeLookSensitivity = 3f;

    /// <summary>
    ///     Normal speed of camera movement.
    /// </summary>
    public float MovementSpeed = 10f;

    /// <summary>
    ///     Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float ZoomSensitivity = 10f;

    private void Start()
    {
        Camera.depthTextureMode = DepthTextureMode.Depth;
    }

    private void Update()
    {
        bool fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float movementSpeed = fastMode ? FastMovementSpeed : MovementSpeed;

        Vector3 position = transform.position;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            position += -transform.right * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            position += transform.right * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            position += transform.forward * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            position += -transform.forward * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            position += transform.up * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.E))
        {
            position += -transform.up * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
        {
            position += Vector3.up * (movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
        {
            position += -Vector3.up * (movementSpeed * Time.deltaTime);
        }

        transform.position = position;

        if (_Looking)
        {
            Vector3 localEulerAngles = transform.localEulerAngles;
            float newRotationX = localEulerAngles.y + (Input.GetAxis("Mouse X") * FreeLookSensitivity);
            float newRotationY = localEulerAngles.x - (Input.GetAxis("Mouse Y") * FreeLookSensitivity);
            localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
            transform.localEulerAngles = localEulerAngles;
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            float zoomSensitivity = fastMode ? FastZoomSensitivity : ZoomSensitivity;
            transform.position += transform.forward * (axis * zoomSensitivity);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }
    }

    private void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    ///     Enable free looking.
    /// </summary>
    private void StartLooking()
    {
        _Looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    ///     Disable free looking.
    /// </summary>
    private void StopLooking()
    {
        _Looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
