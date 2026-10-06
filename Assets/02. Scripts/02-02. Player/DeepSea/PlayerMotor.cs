using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(-100)]
public sealed class PlayerMotor : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 3f;
    [SerializeField, Min(1f)]
    private float sprintMultiplier = 1.6f;
    [SerializeField]
    private MovementBounds movementBounds;
    [Tooltip("이동 범위를 제한할 캐릭터 중심입니다. 비워두면 Rigidbody가 있는 루트를 사용합니다.")]
    [SerializeField]
    private Transform boundaryAnchor;

    private PlayerDiveStatus diveStatus;
    private Rigidbody2D body;
    private Vector2 anchorOffset;
    private float carryWeightSpeedMultiplier = 1f;
    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        diveStatus = GetComponent<PlayerDiveStatus>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        anchorOffset = boundaryAnchor != null
            ? (Vector2)(boundaryAnchor.position - transform.position) : Vector2.zero;
        ConstrainPosition();
    }

    public void Move(Vector2 input, bool sprint = false)
    {
        IsSprinting = sprint && input.sqrMagnitude > 0.0001f
            && diveStatus != null && diveStatus.CurrentAir > 0f;
        Vector2 velocity = Vector2.ClampMagnitude(input, 1f) * moveSpeed * carryWeightSpeedMultiplier
            * (IsSprinting ? Mathf.Max(1f, sprintMultiplier) : 1f);
        if (movementBounds != null)
        {
            ConstrainPosition();
            Vector2 center = body.position + anchorOffset;
            Vector2 destination = movementBounds.Clamp(center + velocity * Time.fixedDeltaTime);
            velocity = (destination - center) / Time.fixedDeltaTime;
        }
        body.linearVelocity = velocity;
        IsSprinting &= velocity.sqrMagnitude > 0.0001f;
    }

    // 충돌이나 외력으로 경계를 벗어난 위치를 카메라가 따라오기 전에 보정합니다.
    private void LateUpdate() => ConstrainPosition();

    private void ConstrainPosition()
    {
        if (body == null || movementBounds == null) return;
        Vector2 center = body.position + anchorOffset;
        Vector2 clamped = movementBounds.Clamp(center);
        if (clamped.x == center.x && clamped.y == center.y) return;
        body.position = clamped - anchorOffset;
        Vector2 velocity = body.linearVelocity;
        if (center.x != clamped.x) velocity.x = 0f;
        if (center.y != clamped.y) velocity.y = 0f;
        body.linearVelocity = velocity;
    }

    public void Stop()
    {
        IsSprinting = false;
        if (body == null) body = GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    /// <summary>보유 무게에 따른 이동속도 배율을 적용합니다.</summary>
    public void SetCarryWeightSpeedMultiplier(float multiplier)
    {
        carryWeightSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    private void OnDisable() => Stop();
}
