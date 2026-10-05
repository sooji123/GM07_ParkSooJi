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
    [Header("공격")]
    [SerializeField, Min(0)]
    private int attackPower = 10;
    [Tooltip("피해 적용 후 남은 체력이 이 값 이하이면 연타 포획을 시작합니다.")]
    [SerializeField, Min(0)]
    private int struggleHealthThreshold = 10;
    [Tooltip("물고기에 적중한 뒤 결과를 보여주기 전까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float hitPauseDuration = 0.6f;
    [Tooltip("물고기에 적중했을 때 작살 끝과 물고기를 발사 방향으로 밀어내는 거리입니다.")]
    [SerializeField, Min(0f)]
    private float hitPushDistance = 0.35f;
    [Tooltip("적중 후 작살 끝과 물고기가 밀려나는 속도입니다.")]
    [SerializeField, Min(0.1f)]
    private float hitPushSpeed = 2.5f;
    [SerializeField]
    private HarpoonStruggleGauge struggleGauge;

    public event Action AttackFinished;
    public event Action<RaycastHit2D> Hit;
    public event Action<Fish> FishHit;
    public event Action<Fish> FishHooked;
    public bool IsAttacking { get; private set; }
    private Vector3 origin;
    private Vector3 tipPosition;
    private Vector3 direction;
    private float travelled;
    private LineRenderer rope;
    private Material ropeMaterial;
    private HarpoonState state = HarpoonState.Ready;
    private float catchHoldTimer;
    private float hitPushTargetDistance;
    private Fish hookedFish;
    private HarpoonHitResult pendingFishResult;

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
        state = HarpoonState.Flying;
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
        if (!IsAttacking)return;

        switch (state)
        {
            case HarpoonState.Flying:
                UpdateFlying();
                break;
            case HarpoonState.HoldingFish:
                UpdateHoldingFish();
                break;
            case HarpoonState.HitPause:
                UpdateHitPause();
                break;
            case HarpoonState.Struggling:
                UpdateStruggling();
                break;
            case HarpoonState.Returning:
                UpdateReturning();
                break;
        }
        UpdateVisual();
    }

    private void UpdateFlying()
    {
        float step = Mathf.Min(
            Mathf.Max(0.1f, launchSpeed) * Time.deltaTime,
            Mathf.Max(0f, range - travelled)
        );

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            tipPosition,
            direction,
            step,
            hitLayers
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.transform.IsChildOf(transform)) continue;
            HandleHit(hit);
            return;
        }

        tipPosition += direction * step;
        travelled += step;

        if (travelled >= range)
        {
            state = HarpoonState.Returning;
        }
    }

    private void UpdateHoldingFish()
    {
        if (hookedFish != null)
            hookedFish.MoveToHook(tipPosition);

        catchHoldTimer -= Time.deltaTime;

        if (catchHoldTimer <= 0f)
            state = HarpoonState.Returning;
    }

    private void UpdateHitPause()
    {
        float currentDistance = Vector3.Distance(origin, tipPosition);
        float pushedDistance = Mathf.MoveTowards(
            currentDistance,
            hitPushTargetDistance,
            Mathf.Max(0.1f, hitPushSpeed) * Time.deltaTime
        );
        tipPosition = origin + direction * pushedDistance;
        travelled = pushedDistance;

        if (hookedFish != null) hookedFish.MoveToHook(tipPosition);
        catchHoldTimer -= Time.deltaTime;
        if (catchHoldTimer > 0f) return;

        switch (pendingFishResult)
        {
            case HarpoonHitResult.DirectCatch:
                FishHooked?.Invoke(hookedFish);
                state = HarpoonState.Returning;
                break;
            case HarpoonHitResult.Struggle:
                if (struggleGauge == null)
                {
                    ReleaseHookedFish();
                    state = HarpoonState.Returning;
                    break;
                }
                state = HarpoonState.Struggling;
                struggleGauge.Begin(
                    direction,
                    hookedFish.CurrentHealth,
                    hookedFish.Data.MaxHealth,
                    OnStruggleFinished
                );
                break;
            default:
                ReleaseHookedFish();
                state = HarpoonState.Returning;
                break;
        }
    }

    private void UpdateStruggling()
    {
        if (hookedFish != null) hookedFish.MoveToHook(tipPosition);
    }

    private void OnStruggleFinished(bool succeeded)
    {
        if (!IsAttacking || state != HarpoonState.Struggling) return;
        if (succeeded) FishHooked?.Invoke(hookedFish);
        else ReleaseHookedFish();
        state = HarpoonState.Returning;
    }

    private void UpdateReturning()
    {
        tipPosition = Vector3.MoveTowards(
            tipPosition,
            origin,
            Mathf.Max(0.1f, returnSpeed) * Time.deltaTime
        );

        if (hookedFish != null)
        {
            hookedFish.MoveToHook(tipPosition);
        }

        if ((tipPosition - origin).sqrMagnitude >= 0.0001f) return;

        Fish caughtFish = hookedFish;
        hookedFish = null;

        if (caughtFish != null)
        {
            caughtFish.CompleteCatch();
        }

        Cancel();
        AttackFinished?.Invoke();
    }

    private bool HandleHit(RaycastHit2D hit)
    {
        tipPosition = new Vector3(
            hit.point.x,
            hit.point.y,
            origin.z
        );

        Fish fish = hit.collider.GetComponentInParent<Fish>();

        if (fish == null)
        {
            state = HarpoonState.Returning;
            Hit?.Invoke(hit);
            return true;
        }

        pendingFishResult = fish.HitByHarpoon(attackPower, struggleHealthThreshold);
        hookedFish = fish;
        travelled = Vector3.Distance(origin, tipPosition);
        hitPushTargetDistance = Mathf.Min(range, travelled + hitPushDistance);
        hookedFish.BeginHook(origin);
        hookedFish.MoveToHook(tipPosition);
        catchHoldTimer = hitPauseDuration;
        state = HarpoonState.HitPause;
        FishHit?.Invoke(hookedFish);

        Hit?.Invoke(hit);
        return true;
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
        if (struggleGauge != null) struggleGauge.Cancel();
        IsAttacking = false;
        if (rope != null) rope.enabled = false;
        if (tipVisual != null) tipVisual.gameObject.SetActive(false);
        if (hookedFish != null)
        {
            hookedFish.Release();
            hookedFish = null;
        }
    }

    private void ReleaseHookedFish()
    {
        if (hookedFish == null) return;
        hookedFish.Release();
        hookedFish = null;
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
