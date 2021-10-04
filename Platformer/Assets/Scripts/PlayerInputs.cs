using UnityEngine;

public class PlayerInputs : MonoBehaviour
{
    [SerializeField] float _inputBufferTime = 0.1f;

    public float HorizontalInput { get; private set; }
    public bool JumpInput { get => _timeJumpWasPressed + _inputBufferTime > Time.time; }
    public bool DashInput { get => _timeDashWasPressed + _inputBufferTime > Time.time; }
    public bool JumpPressed { get; private set; }
    public bool DashPressed { get; private set; }
    public bool WallGrabPressed { get; private set; }
    public bool SlidePressed { get; private set; }

    float _timeJumpWasPressed = -1.0f;
    float _timeDashWasPressed = -1.0f;

    void Update()
    {
        if (!GameManager.Instance.IsGamePaused)
        {
            HorizontalInput = Input.GetAxisRaw("Horizontal");

            JumpPressed = Input.GetButton("Jump");
            DashPressed = Input.GetButton("Dash");
            WallGrabPressed = Input.GetButton("WallGrab");
            SlidePressed = Input.GetButton("Slide");

            if (Input.GetButtonDown("Jump"))
            {
                _timeJumpWasPressed = Time.time;
            }

            if (Input.GetButtonDown("Dash"))
            {
                _timeDashWasPressed = Time.time;
            }
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

    /// <summary>
    /// Consumes the stored input, reseting its value
    /// </summary>
    /// <returns></returns>
    public bool ConsumeDashInput()
    {
        bool dashInput = DashInput;
        _timeDashWasPressed = -1.0f;

        return dashInput;
    }
}
