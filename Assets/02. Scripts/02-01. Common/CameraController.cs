using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private MovementBounds movementBounds;
    [SerializeField, Min(0f)] private float smoothTime = 0.2f;
    private Vector2 followVelocity;

    private void OnEnable() => followVelocity = Vector2.zero;

    private void LateUpdate()
    {
        Vector2 current = transform.position;
        Vector2 desired = target != null ? (Vector2)target.position : current;
        if (movementBounds != null) desired = movementBounds.Clamp(desired);
        Vector2 next = smoothTime > 0f
            ? Vector2.SmoothDamp(current, desired, ref followVelocity, smoothTime)
            : desired;
        if (movementBounds != null)
        {
            Vector2 clamped = movementBounds.Clamp(next);
            if (clamped.x != next.x) followVelocity.x = 0f;
            if (clamped.y != next.y) followVelocity.y = 0f;
            next = clamped;
        }
        // 카메라의 원근 거리와 회전을 유지합니다.
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }
}
