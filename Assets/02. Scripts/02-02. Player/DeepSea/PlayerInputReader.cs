using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 MousePosition { get; private set; }
    public bool AimPressed { get; private set; }
    public bool AimHeld { get; private set; }
    public bool FirePressed { get; private set; }
    public bool SprintHeld { get; private set; }

    // 스크립트 실행 순서에 의존하지 않도록 컨트롤러가 직접 호출합니다.
    public void ReadInput()
    {
        Move = Vector2.zero;
        AimPressed = AimHeld = FirePressed = false;
        SprintHeld = false;
        if (!Application.isFocused) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            SprintHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            Move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        MousePosition = mouse.position.ReadValue();
        AimPressed = mouse.rightButton.wasPressedThisFrame;
        AimHeld = mouse.rightButton.isPressed;
        FirePressed = mouse.leftButton.wasPressedThisFrame;
    }
}
