
using UnityEngine;
using UnityEngine.InputSystem;

public class PCInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    private Vector2 lookInput;
    private bool jumpRequested;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null)
        {
            MoveInput = Vector2.zero;
            return;
        }

        Vector2 input = Vector2.zero;

        if (keyboard.wKey.isPressed)
            input.y += 1f;

        if (keyboard.sKey.isPressed)
            input.y -= 1f;

        if (keyboard.dKey.isPressed)
            input.x += 1f;

        if (keyboard.aKey.isPressed)
            input.x -= 1f;

        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        MoveInput = Vector2.ClampMagnitude(input, 1f);

        if (mouse != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                lookInput += mouse.delta.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public Vector2 ConsumeLookInput()
    {
        Vector2 result = lookInput;
        lookInput = Vector2.zero;
        return result;
    }

    public bool ConsumeJumpInput()
    {
        if (!jumpRequested)
            return false;

        jumpRequested = false;
        return true;
    }
}
