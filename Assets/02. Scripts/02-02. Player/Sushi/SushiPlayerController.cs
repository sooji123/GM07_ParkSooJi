using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public sealed class SushiPlayerController : MonoBehaviour
{
    private static readonly int StateParameter = Animator.StringToHash("State");

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 3f;
    [SerializeField, Min(0f)] private float sprintSpeed = 5.5f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;

    [Header("Energy")]
    [SerializeField, Min(0.01f)] private float maxEnergy = 100f;
    [SerializeField, Min(0f)] private float drainPerSecond = 25f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 15f;
    [SerializeField] private Image energyGauge;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float moveInput;
    private float currentEnergy;
    private bool controlEnabled;
    private bool isSprinting;
    private bool hasStateParameter;

    public bool ControlEnabled => controlEnabled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        hasStateParameter = HasIntParameter("State");
        currentEnergy = maxEnergy;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        RefreshGauge();
    }

    private void Update()
    {
        ReadMovement();
        UpdateEnergy();
        UpdateAnimationState();
        RefreshGauge();
    }

    private void FixedUpdate()
    {
        if (!controlEnabled)
            return;

        float speed = isSprinting ? sprintSpeed : walkSpeed;
        Vector2 next = body.position;
        next.x += moveInput * speed * Time.fixedDeltaTime;
        next.x = Mathf.Clamp(next.x, minX, maxX);
        body.MovePosition(next);
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;
        if (enabled)
            return;

        moveInput = 0f;
        isSprinting = false;
        UpdateAnimationState();
    }

    private void ReadMovement()
    {
        moveInput = 0f;
        isSprinting = false;

        if (!controlEnabled || !Application.isFocused)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
        moveInput = (right ? 1f : 0f) - (left ? 1f : 0f);

        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        isSprinting = moveInput != 0f && shiftHeld && currentEnergy > 0f;

        if (moveInput != 0f)
            spriteRenderer.flipX = moveInput < 0f;
    }

    private void UpdateAnimationState()
    {
        if (animator == null || !hasStateParameter)
            return;

        int state = moveInput == 0f ? 0 : isSprinting ? 2 : 1;
        animator.SetInteger(StateParameter, state);
    }

    private void UpdateEnergy()
    {
        if (isSprinting)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - drainPerSecond * Time.deltaTime);
            if (currentEnergy <= 0f)
                isSprinting = false;
        }
        else
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + recoveryPerSecond * Time.deltaTime);
    }

    private void RefreshGauge()
    {
        if (energyGauge != null)
            energyGauge.fillAmount = currentEnergy / Mathf.Max(0.01f, maxEnergy);
    }

    private bool HasIntParameter(string parameterName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Int &&
                parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }
}
