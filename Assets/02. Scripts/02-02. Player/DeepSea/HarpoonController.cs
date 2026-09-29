using System;
using UnityEngine;

public sealed class HarpoonController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] 
    private float range = 5f;
    [SerializeField, Min(0.1f)] 
    private float launchSpeed = 12f;
    [SerializeField, Min(0.1f)] 
    private float returnSpeed = 15f;
    [Tooltip("2D 콜라이더를 사용하며 플레이어 자식의 콜라이더는 무시합니다.")]
    [SerializeField] 
    private LayerMask hitLayers = ~0;
    [SerializeField] 
    private Transform tipVisual;

    public event Action AttackFinished;
    public event Action<RaycastHit2D> Hit;
    public bool IsAttacking { get; private set; }
    private bool returning;
    private Vector3 origin;
    private Vector3 tipPosition;
    private Vector3 direction;
    private float travelled;
    private LineRenderer rope;
    private Material ropeMaterial;

    private void Awake()
    {
        var visual = new GameObject("HarpoonRope");
        visual.transform.SetParent(transform, false);
        rope = visual.AddComponent<LineRenderer>();
        rope.useWorldSpace = true;
        rope.positionCount = 2;
        rope.startWidth = rope.endWidth = 0.02f;
        rope.startColor = rope.endColor = Color.black;
        rope.sortingOrder = 11;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            ropeMaterial = new Material(shader);
            rope.sharedMaterial = ropeMaterial;
        }
        rope.enabled = false;
        if (tipVisual != null)
        {
            tipVisual.gameObject.SetActive(false);
        }
    }

    public bool Fire(Vector3 start, Vector2 aimDirection)
    {
        if (!isActiveAndEnabled || IsAttacking || aimDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }
        origin = tipPosition = start;
        direction = aimDirection.normalized;
        travelled = 0f;
        returning = false;
        IsAttacking = true;
        if (tipVisual != null)
        {
            tipVisual.position = start;
            tipVisual.gameObject.SetActive(true);
            UpdateTipRotation();
        }
        rope.enabled = true;
        UpdateVisual();
        return true;
    }

    private void Update()
    {
        if (!IsAttacking) return;
        if (returning)
        {
            tipPosition = Vector3.MoveTowards(tipPosition, origin,
                Mathf.Max(0.1f, returnSpeed) * Time.deltaTime);
            if ((tipPosition - origin).sqrMagnitude < 0.0001f)
            {
                Cancel();
                AttackFinished?.Invoke();
                return;
            }
        }
        else
        {
            float step = Mathf.Min(Mathf.Max(0.1f, launchSpeed) * Time.deltaTime,
                Mathf.Max(0f, range - travelled));
            // 이동 구간 전체의 충돌을 검사하여 빠르게 발사해도 얇은 콜라이더를 통과하지 않도록 합니다.
            RaycastHit2D[] hits = Physics2D.RaycastAll(tipPosition, direction, step, hitLayers);
            bool didHit = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger || hit.transform.IsChildOf(transform)) continue;
                tipPosition = new Vector3(hit.point.x, hit.point.y, origin.z);
                returning = didHit = true;
                Hit?.Invoke(hit);
                break;
            }
            if (!IsAttacking) return;
            if (!didHit)
            {
                tipPosition += direction * step;
                travelled += step;
                if (travelled >= range) returning = true;
            }
        }
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        rope.SetPosition(0, origin);
        rope.SetPosition(1, tipPosition);
        if (tipVisual != null) tipVisual.position = tipPosition;
    }

    private void UpdateTipRotation()
    {
        if (tipVisual == null) return;

        bool facingRight = direction.x >= 0f;

        float angle = Mathf.Atan2(
            direction.y,
            Mathf.Abs(direction.x)
        ) * Mathf.Rad2Deg;

        tipVisual.rotation = Quaternion.Euler(
            0f,
            0f,
            facingRight ? angle : -angle
        );

        Vector3 scale = tipVisual.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1f : -1f);

        tipVisual.localScale = scale;
    }

    public void Cancel()
    {
        IsAttacking = false;
        if (rope != null) rope.enabled = false;
        if (tipVisual != null) tipVisual.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        bool wasAttacking = IsAttacking;
        Cancel();
        if (wasAttacking) AttackFinished?.Invoke();
    }

    private void OnDestroy()
    {
        if (ropeMaterial != null) Destroy(ropeMaterial);
    }
}
