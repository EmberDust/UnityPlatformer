using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputs : MonoBehaviour
{
    [SerializeField] float _inputBufferTime = 0.1f;

    float _timeJumpWasPressed = -1.0f;  // to buffer the jump input - resets to -1 after consumption

    public float HorizontalInput { get; private set; }
    public bool JumpInput { get => _timeJumpWasPressed + _inputBufferTime > Time.time; }
    public bool JumpPressed { get; private set; }
    public bool WallGrabPressed { get; private set; }

    void Update()
    {
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        JumpPressed = Input.GetButton("Jump");
        WallGrabPressed = Input.GetButton("WallGrab");

        if (Input.GetButtonDown("Jump"))
        {
            _timeJumpWasPressed = Time.time;
        }
    }

    /// <summary>
    /// Consumes the stored input, reseting its value
    /// </summary>
    /// <returns></returns>
    public bool ConsumeJumpInput()
    {
        bool jumpInput = JumpInput;
        _timeJumpWasPressed = -1.0f;

        return jumpInput;
    }
}
