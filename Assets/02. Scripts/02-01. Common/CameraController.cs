using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private MovementBounds movementBounds;
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;
    private Vector2 followVelocity;
    private Vector2 followPosition;

    public Vector2 EffectOffset { get; set; }

    private void OnEnable()
    {
        followVelocity = Vector2.zero;
        followPosition = transform.position;
        EffectOffset = Vector2.zero;
    }

    private void LateUpdate()
    {
        Vector2 desired = target != null ? (Vector2)target.position : followPosition;
        if (movementBounds != null) desired = movementBounds.Clamp(desired);
        Vector2 next = smoothTime > 0f
            ? Vector2.SmoothDamp(followPosition, desired, ref followVelocity, smoothTime)
            : desired;
        if (movementBounds != null)
        {
            Vector2 clamped = movementBounds.Clamp(next);
            if (clamped.x != next.x) followVelocity.x = 0f;
            if (clamped.y != next.y) followVelocity.y = 0f;
            next = clamped;
        }
        followPosition = next;

        // 추적 좌표에 카메라 연출 오프셋을 더해도 다음 프레임의 추적 계산은 흔들리지 않습니다.
        Vector2 displayedPosition = followPosition + EffectOffset;
        transform.position = new Vector3(
            displayedPosition.x,
            displayedPosition.y,
            transform.position.z
        );
    }
}
