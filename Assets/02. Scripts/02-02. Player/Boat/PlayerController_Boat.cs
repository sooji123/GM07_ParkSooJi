using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController_Boat : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] 
    private float moveSpeed = 3f;
    [SerializeField] 
    private float minX = -3.5f;
    [SerializeField] 
    private float maxX = 0.5f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float moveInput;
    private float footstepDistance;

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        moveInput = 0f;

        if (!CanMove || !Application.isFocused)
        {
            if (animator != null)
                animator.SetBool("IsWalking", false);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool moveLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool moveRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        if (moveLeft || moveRight)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }

        moveInput = (moveRight ? 1f : 0f) - (moveLeft ? 1f : 0f);

        if (moveInput == 0f) return;

        spriteRenderer.flipX = moveInput < 0f;
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition = body.position;
        nextPosition.x += moveInput * moveSpeed * Time.fixedDeltaTime;
        nextPosition.x = Mathf.Clamp( nextPosition.x, minX, maxX);
        float movedDistance = Mathf.Abs(nextPosition.x - body.position.x);
        if (movedDistance > 0.0001f)
        {
            footstepDistance += movedDistance;
            if (footstepDistance >= 1f)
            {
                footstepDistance %= 1f;
                if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.Footstep);
            }
        }
        else footstepDistance = 0f;
        body.MovePosition(nextPosition);
    }
}
