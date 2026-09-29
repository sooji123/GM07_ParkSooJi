using UnityEngine;

/// <summary>게임 상태를 애니메이션에 반영</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAnimation : MonoBehaviour
{
    [Header("참조 (비워두면 자동 검색)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("애니메이터")]
    [SerializeField] private string stateParameter = "State";

    [Header("수직 수영")]
    [SerializeField, Range(0.71f, 1f)] private float verticalThreshold = 0.9f;
    private static readonly int SwimBlendId = Animator.StringToHash("Blend");
    private bool hasSwimBlend;

    [Header("바라보는 방향")]
    [SerializeField] private bool updateSpriteFacing = true;
    [Tooltip("좌우 반전하지 않은 원본 스프라이트가 바라보는 방향입니다.")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("대각선 수영 기울기")]
    [Tooltip("회전시킬 캐릭터 외형입니다. 비워두면 SpriteRenderer의 Transform을 사용합니다.")]
    [SerializeField] private Transform bodyVisual;
    [SerializeField, Range(0f, 80f)] private float maxDiagonalTilt = 35f;
    [SerializeField, Min(0.01f)] private float tiltSmoothTime = 0.12f;
    [SerializeField, Range(0.01f, 0.5f)] private float diagonalInputThreshold = 0.1f;

    private PlayerController player;
    private PlayerInputReader input;
    private PlayerAim aim;
    private int stateParameterId;
    private bool hasStateParameter;
    private RuntimeAnimatorController cachedController;
    private int lastAnimationState = -1;
    private Quaternion initialBodyRotation;
    private float currentTilt;
    private float tiltVelocity;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        input = GetComponent<PlayerInputReader>();
        aim = GetComponent<PlayerAim>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (spriteRenderer == null)
            spriteRenderer = animator != null
                ? animator.GetComponentInChildren<SpriteRenderer>(true)
                : GetComponentInChildren<SpriteRenderer>(true);
        if (bodyVisual == null && spriteRenderer != null)
            bodyVisual = spriteRenderer.transform;
        if (bodyVisual != null)
            initialBodyRotation = bodyVisual.localRotation;
        if (animator == null)
            Debug.LogWarning("PlayerAnimation: Assign the character Animator in the Inspector.", this);
    }

    private void OnEnable()
    {
        player.StateChanged += OnStateChanged;
        ValidateAnimator();
        OnStateChanged(player.State);
    }

    private void ValidateAnimator()
    {
        hasStateParameter = false;
        hasSwimBlend = false;
        lastAnimationState = -1;
        cachedController = animator != null ? animator.runtimeAnimatorController : null;
        if (cachedController == null) return;

        stateParameterId = Animator.StringToHash(stateParameter);
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == stateParameterId && parameter.type == AnimatorControllerParameterType.Int)
            {
                hasStateParameter = true;
            }
            if (parameter.nameHash == SwimBlendId && parameter.type == AnimatorControllerParameterType.Float)
                hasSwimBlend = true;
        }
        if (!hasStateParameter)
            Debug.LogWarning($"PlayerAnimation: Animator needs an Int parameter named '{stateParameter}'.", this);
    }

    private void OnStateChanged(PlayerState state)
    {
        if (!hasStateParameter || animator == null || !animator.isActiveAndEnabled) return;
        // 회수 완료까지 게임 상태는 Attack을 유지하고 애니메이터는 Aim을 유지합니다.
        int animationState = state == PlayerState.Attack
            ? (int)PlayerState.Aim : (int)state;
        if (lastAnimationState == animationState) return;
        animator.SetInteger(stateParameterId, animationState);
        lastAnimationState = animationState;
    }

    private void LateUpdate()
    {
        // 이번 프레임에서 PlayerController가 조준과 입력을 갱신한 뒤 방향을 읽습니다.
        if (animator != null && animator.runtimeAnimatorController != cachedController)
            ValidateAnimator();
        if (animator != null && !animator.isActiveAndEnabled) lastAnimationState = -1;
        OnStateChanged(player.State);
        UpdateSwimAnimation();
        UpdateDiagonalTilt();

        if (!updateSpriteFacing || spriteRenderer == null) return;
        float horizontal;
        switch (player.State)
        {
            case PlayerState.Move:
                horizontal = input.Move.x;
                break;
            case PlayerState.Aim:
            case PlayerState.Attack:
                horizontal = aim.Direction.x;
                break;
            default:
                return; // 대기 중에는 마지막으로 바라보던 방향을 유지합니다.
        }
        if (Mathf.Abs(horizontal) > 0.001f)
            spriteRenderer.flipX = (horizontal < 0f) == spriteFacesRight;
    }

    public static float GetDiagonalTilt(Vector2 movement, float maximumTilt, float inputThreshold)
    {
        float threshold = Mathf.Max(0.01f, inputThreshold);
        if (Mathf.Abs(movement.x) < threshold || Mathf.Abs(movement.y) < threshold)
            return 0f;

        float angle = Mathf.Atan2(movement.y, Mathf.Abs(movement.x)) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -Mathf.Abs(maximumTilt), Mathf.Abs(maximumTilt));
        return movement.x < 0f ? -angle : angle;
    }

    private void UpdateDiagonalTilt()
    {
        if (bodyVisual == null) return;

        float targetTilt = player.State == PlayerState.Move
            ? GetDiagonalTilt(input.Move, maxDiagonalTilt, diagonalInputThreshold)
            : 0f;
        currentTilt = Mathf.SmoothDampAngle(
            currentTilt, targetTilt, ref tiltVelocity, Mathf.Max(0.01f, tiltSmoothTime));
        bodyVisual.localRotation = initialBodyRotation * Quaternion.Euler(0f, 0f, currentTilt);
    }

    // 블렌드 트리의 정확한 임계값을 사용하여 서로 다른 스프라이트 프레임이 섞이지 않도록 합니다.
    public static float GetSwimBlend(Vector2 movement, float threshold)
    {
        if (movement.sqrMagnitude < 0.0001f) return 0f;
        float vertical = movement.normalized.y;
        float limit = Mathf.Clamp(threshold, 0.71f, 1f);
        if (vertical >= limit) return 1f;
        if (vertical <= -limit) return -1f;
        return 0f;
    }

    private void UpdateSwimAnimation()
    {
        if (!hasSwimBlend || animator == null || !animator.isActiveAndEnabled) return;
        float blend = player.State == PlayerState.Move
            ? GetSwimBlend(input.Move, verticalThreshold) : 0f;
        animator.SetFloat(SwimBlendId, blend);
    }

    private void OnDisable()
    {
        player.StateChanged -= OnStateChanged;
        currentTilt = 0f;
        tiltVelocity = 0f;
        if (bodyVisual != null) bodyVisual.localRotation = initialBodyRotation;
    }
}
