using UnityEngine;

/// <summary>물고기의 배회, 회피, 떼 이동, 추격과 공격 행동을 제어합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Fish))]
public sealed class FishAgent : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private FishArea area;
    [SerializeField] private FishSchool school;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Canvas alertCanvas;
    [Tooltip("플레이어를 추격하는 동안 표시할 느낌표 말풍선입니다.")]
    [SerializeField] private GameObject chaseAlert;
    [Tooltip("플레이어 추격을 포기한 뒤 잠시 표시할 물음표 말풍선입니다.")]
    [SerializeField] private GameObject lostAlert;
    [Tooltip("원본 스프라이트가 오른쪽을 바라보면 활성화합니다.")]
    [SerializeField] private bool spriteFacesRight;

    [Header("기본 이동")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 0.8f;
    [SerializeField, Min(0.01f)] private float turnSpeed = 3f;
    [Tooltip("한 번 이동할 때 사용할 기준 거리입니다.")]
    [SerializeField, Min(0.1f)] private float wanderDistance = 5f;
    [Tooltip("기준 이동 거리에 더하거나 뺄 무작위 오차입니다.")]
    [SerializeField, Min(0f)] private float wanderDistanceVariation = 3f;
    [SerializeField, Min(0.01f)] private float wanderArrivalDistance = 0.2f;
    [Tooltip("목적지 도착 후 사용할 기준 대기시간입니다.")]
    [SerializeField, Min(0f)] private float restDuration = 3f;
    [Tooltip("기준 대기시간에 더하거나 뺄 무작위 오차입니다.")]
    [SerializeField, Min(0f)] private float restDurationVariation = 2f;
    [SerializeField, Min(0f)] private float boundaryWeight = 2.5f;
    [SerializeField, Min(0f)] private float facingChangeDelay = 0.15f;

    [Header("비공격형 회피")]
    [SerializeField, Min(0f)] private float playerAvoidDistance = 1.8f;
    [Tooltip("회피를 시작한 물고기가 배회로 돌아가기 위해 확보해야 하는 거리입니다.")]
    [SerializeField, Min(0f)] private float playerAvoidReleaseDistance = 3f;
    [Tooltip("회피가 끝난 뒤 다시 회피할 수 있을 때까지의 시간입니다.")]
    [SerializeField, Min(0f)] private float fleeCooldownDuration = 2f;
    [SerializeField, Min(0.1f)] private float fleeDestinationDistance = 2.5f;
    [SerializeField, Min(0.01f)] private float fleeArrivalDistance = 0.15f;

    [Header("공격형 행동")]
    [SerializeField, Min(0.1f)] private float detectionDistance = 3f;
    [SerializeField, Min(1f)] private float chaseLoseDistanceMultiplier = 1.4f;
    [SerializeField, Min(1f)] private float outsideAreaDetectionMultiplier = 2f;
    [SerializeField, Min(0.05f)] private float attackDistance = 0.6f;
    [SerializeField, Min(0f)] private float attackAirDamage = 8f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.2f;
    [Tooltip("추격을 포기한 뒤 물음표 말풍선을 유지하는 시간입니다.")]
    [SerializeField, Min(0f)] private float alertAfterChaseDuration = 1f;

    public Vector2 Velocity { get; private set; }

    private Fish fish;
    private PlayerDiveStatus player;
    private Vector2 wanderDirection;
    private Vector2 wanderDestination;
    private bool hasWanderDestination;
    private bool resting;
    private float restTimer;
    private float attackTimer;
    private bool chasing;
    private bool fleeing;
    private Vector2 fleeDestination;
    private float fleeCooldownTimer;
    private bool visualFacingRight;
    private bool pendingFacingRight;
    private float facingChangeTimer;
    private float alertHideTimer;
    private Vector3 baseLocalScale;
    private Vector3 baseAlertCanvasLocalScale;
    private float initialRootScaleSign;

    private void Awake()
    {
        fish = GetComponent<Fish>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (alertCanvas == null) alertCanvas = GetComponentInChildren<Canvas>(true);
        HideAlerts();
        baseLocalScale = transform.localScale;
        initialRootScaleSign = baseLocalScale.x < 0f ? -1f : 1f;
        if (alertCanvas != null)
            baseAlertCanvasLocalScale = alertCanvas.transform.localScale;
        wanderDirection = Random.insideUnitCircle.normalized;
        if (wanderDirection.sqrMagnitude < 0.01f) wanderDirection = Vector2.right;
        Velocity = wanderDirection * moveSpeed;
        if (spriteRenderer != null)
        {
            bool isMirrored = spriteRenderer.flipX ^ baseLocalScale.x < 0f;
            visualFacingRight = isMirrored ? !spriteFacesRight : spriteFacesRight;
            spriteRenderer.flipX = false;
            ApplyFacing(visualFacingRight);
        }
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerDiveStatus>();
    }

    public void AssignArea(FishArea assignedArea) => area = assignedArea;

    public void AssignSchool(FishSchool assignedSchool)
    {
        school = assignedSchool;
        if (area == null && school != null) area = school.Area;
    }

    private void Update()
    {
        if (fish == null || fish.IsHooked) return;
        float deltaTime = Time.deltaTime;
        attackTimer -= deltaTime;
        fleeCooldownTimer -= deltaTime;
        UpdateAlert(deltaTime);

        Vector2 desired = ChooseBehavior();
        if (school != null && !chasing && !fleeing)
            desired += school.CalculateSteering(this);
        if (!fleeing) desired += CalculateBoundarySteering();

        if (desired.sqrMagnitude < 0.001f && !resting) desired = wanderDirection;
        Vector2 targetVelocity = resting
            ? Vector2.zero
            : desired.normalized * moveSpeed;
        Velocity = Vector2.Lerp(Velocity, targetVelocity, 1f - Mathf.Exp(-turnSpeed * deltaTime));

        Vector3 position = transform.position;
        position += (Vector3)(Velocity * deltaTime);
        KeepInsideArea(ref position);
        transform.position = position;
        UpdateFacing(deltaTime);
    }

    private Vector2 ChooseBehavior()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerDiveStatus>();
            return UpdateWander();
        }

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float playerDistance = toPlayer.magnitude;
        bool aggressive = fish.Data != null && fish.Data.Aggressive;

        if (aggressive)
        {
            UpdateChaseState(playerDistance);
            if (chasing)
            {
                StopResting();
                if (playerDistance <= attackDistance && attackTimer <= 0f)
                {
                    player.TryTakeDamage(attackAirDamage);
                    attackTimer = attackCooldown;
                }
                return toPlayer.normalized;
            }
        }
        else if (ShouldFlee(playerDistance) && playerDistance > 0.01f)
        {
            StopResting();
            UpdateFleeDestination(toPlayer);
            Vector2 toDestination = fleeDestination - (Vector2)transform.position;
            if (toDestination.sqrMagnitude > fleeArrivalDistance * fleeArrivalDistance)
                return toDestination.normalized;
        }

        else if (fleeing)
        {
            // 회피가 끝났을 때 갑자기 이전 배회 방향으로 되돌아가지 않도록 현재 진행 방향을 유지합니다.
            fleeing = false;
            fleeCooldownTimer = fleeCooldownDuration;
            if (Velocity.sqrMagnitude > 0.001f) wanderDirection = Velocity.normalized;
            hasWanderDestination = false;
        }

        return UpdateWander();
    }

    private bool ShouldFlee(float playerDistance)
    {
        float releaseDistance = Mathf.Max(playerAvoidDistance, playerAvoidReleaseDistance);
        return fleeing
            ? playerDistance <= releaseDistance
            : fleeCooldownTimer <= 0f && playerDistance <= playerAvoidDistance;
    }

    private void UpdateFleeDestination(Vector2 toPlayer)
    {
        Vector2 awayFromPlayer = -toPlayer.normalized;
        Vector2 desiredDestination = (Vector2)transform.position
            + awayFromPlayer * fleeDestinationDistance;
        if (area != null)
            desiredDestination = area.GetFleeDestination(
                transform.position,
                player.transform.position,
                fleeDestinationDistance
            );

        bool reachedDestination = !fleeing
            || ((Vector2)transform.position - fleeDestination).sqrMagnitude
            <= fleeArrivalDistance * fleeArrivalDistance;
        bool destinationPointsTowardPlayer = Vector2.Dot(
            fleeDestination - (Vector2)transform.position,
            toPlayer
        ) > 0f;

        if (reachedDestination || destinationPointsTowardPlayer)
            fleeDestination = desiredDestination;
        fleeing = true;
    }

    private void UpdateChaseState(float playerDistance)
    {
        if (!chasing)
        {
            if (playerDistance <= detectionDistance)
            {
                chasing = true;
                alertHideTimer = 0f;
                ShowChaseAlert();
            }
            return;
        }

        bool outsideArea = area != null && !area.Contains(transform.position);
        float multiplier = outsideArea
            ? outsideAreaDetectionMultiplier
            : chaseLoseDistanceMultiplier;
        if (playerDistance > detectionDistance * multiplier)
        {
            chasing = false;
            alertHideTimer = alertAfterChaseDuration;
            if (alertHideTimer > 0f) ShowLostAlert();
            else HideAlerts();
        }
    }

    private void UpdateAlert(float deltaTime)
    {
        if (chasing) return;
        if (alertHideTimer <= 0f)
        {
            HideAlerts();
            return;
        }
        alertHideTimer -= deltaTime;
        if (alertHideTimer <= 0f) HideAlerts();
    }

    private void ShowChaseAlert()
    {
        if (alertCanvas != null) alertCanvas.enabled = true;
        if (chaseAlert != null) chaseAlert.SetActive(true);
        if (lostAlert != null) lostAlert.SetActive(false);
    }

    private void ShowLostAlert()
    {
        if (alertCanvas != null) alertCanvas.enabled = true;
        if (chaseAlert != null) chaseAlert.SetActive(false);
        if (lostAlert != null) lostAlert.SetActive(true);
    }

    private void HideAlerts()
    {
        if (chaseAlert != null) chaseAlert.SetActive(false);
        if (lostAlert != null) lostAlert.SetActive(false);
        if (alertCanvas != null) alertCanvas.enabled = false;
    }

    private Vector2 UpdateWander()
    {
        if (area == null)
        {
            wanderDirection = (wanderDirection + Random.insideUnitCircle * Time.deltaTime).normalized;
            return wanderDirection;
        }

        Vector2 position = transform.position;
        bool arrived = hasWanderDestination
            && (wanderDestination - position).sqrMagnitude
            <= wanderArrivalDistance * wanderArrivalDistance;
        if (arrived)
        {
            hasWanderDestination = false;
            if (!resting)
            {
                float minRest = Mathf.Max(0f, restDuration - restDurationVariation);
                float maxRest = Mathf.Max(minRest, restDuration + restDurationVariation);
                restTimer = Random.Range(minRest, maxRest);
                resting = restTimer > 0f;
            }
        }

        if (resting)
        {
            restTimer -= Time.deltaTime;
            if (restTimer > 0f) return Vector2.zero;
            resting = false;
        }

        if (!hasWanderDestination)
            SelectWanderDestination(position);

        Vector2 toDestination = wanderDestination - position;
        return toDestination.sqrMagnitude > 0.001f
            ? toDestination.normalized
            : wanderDirection;
    }

    private void SelectWanderDestination(Vector2 position)
    {
        Vector2 currentDirection = Velocity.sqrMagnitude > 0.001f
            ? Velocity.normalized
            : wanderDirection;
        float minimumDistance = Mathf.Max(0.1f, wanderDistance - wanderDistanceVariation);
        float maximumDistance = Mathf.Max(minimumDistance, wanderDistance + wanderDistanceVariation);
        float selectedDistance = Random.Range(minimumDistance, maximumDistance);
        Vector2 bestPoint = area.ClampPoint(position + currentDirection * selectedDistance);
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < 16; i++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection.sqrMagnitude < 0.001f) continue;

            Vector2 candidate = area.ClampPoint(position + randomDirection * selectedDistance);
            Vector2 offset = candidate - position;
            float actualDistance = offset.magnitude;
            if (actualDistance <= wanderArrivalDistance) continue;

            float directionScore = Vector2.Dot(currentDirection, offset.normalized);
            float distanceError = Mathf.Abs(selectedDistance - actualDistance);
            float score = -distanceError + directionScore * 0.25f;
            if (score <= bestScore) continue;
            bestScore = score;
            bestPoint = candidate;
        }

        wanderDestination = bestPoint;
        Vector2 newDirection = wanderDestination - position;
        if (newDirection.sqrMagnitude > 0.001f) wanderDirection = newDirection.normalized;
        hasWanderDestination = true;
    }

    private void StopResting()
    {
        resting = false;
        restTimer = 0f;
    }

    private Vector2 CalculateBoundarySteering()
    {
        if (area == null || (chasing && fish.Data != null && fish.Data.Aggressive))
            return Vector2.zero;
        if (area.Contains(transform.position)) return Vector2.zero;

        Vector2 closest = area.ClosestPoint(transform.position);
        Vector2 returnDirection = closest - (Vector2)transform.position;
        if (returnDirection.sqrMagnitude < 0.001f)
            returnDirection = area.Center - (Vector2)transform.position;
        return returnDirection.normalized * boundaryWeight;
    }

    private void KeepInsideArea(ref Vector3 position)
    {
        bool aggressiveChase = chasing && fish.Data != null && fish.Data.Aggressive;
        if (area == null || aggressiveChase || area.Contains(position)) return;

        Vector2 clamped = area.ClampPoint(position);
        position.x = clamped.x;
        position.y = clamped.y;

        Vector2 inward = area.Center - clamped;
        if (inward.sqrMagnitude > 0.001f)
        {
            wanderDirection = inward.normalized;
            Velocity = wanderDirection * moveSpeed;
            hasWanderDestination = false;
            StopResting();
        }
    }

    private void UpdateFacing(float deltaTime)
    {
        if (spriteRenderer == null || Mathf.Abs(Velocity.x) < 0.08f) return;
        bool movingRight = Velocity.x > 0f;
        if (movingRight == visualFacingRight)
        {
            facingChangeTimer = 0f;
            return;
        }

        if (pendingFacingRight != movingRight)
        {
            pendingFacingRight = movingRight;
            facingChangeTimer = 0f;
        }

        facingChangeTimer += deltaTime;
        if (facingChangeTimer < facingChangeDelay) return;

        visualFacingRight = movingRight;
        ApplyFacing(movingRight);
        facingChangeTimer = 0f;
    }

    private void ApplyFacing(bool faceRight)
    {
        Vector3 scale = baseLocalScale;
        float horizontalScale = Mathf.Abs(baseLocalScale.x);
        scale.x = faceRight == spriteFacesRight ? horizontalScale : -horizontalScale;
        transform.localScale = scale;

        if (alertCanvas != null)
        {
            Vector3 canvasScale = baseAlertCanvasLocalScale;
            float currentRootScaleSign = scale.x < 0f ? -1f : 1f;
            canvasScale.x *= initialRootScaleSign / currentRootScaleSign;
            alertCanvas.transform.localScale = canvasScale;
        }

        // 루트 Transform을 반전하므로 SpriteRenderer 자체의 반전은 사용하지 않습니다.
        if (spriteRenderer != null) spriteRenderer.flipX = false;
    }

    /// <summary>물고기가 지정된 위치의 반대 방향을 바라보도록 즉시 반전합니다.</summary>
    public void FaceAwayFrom(Vector3 sourcePosition)
    {
        if (spriteRenderer == null) return;

        float horizontalDirection = transform.position.x - sourcePosition.x;
        if (Mathf.Abs(horizontalDirection) < 0.001f) return;

        bool faceRight = horizontalDirection > 0f;
        visualFacingRight = faceRight;
        pendingFacingRight = faceRight;
        facingChangeTimer = 0f;
        ApplyFacing(faceRight);
    }

    private void OnDisable()
    {
        chasing = false;
        alertHideTimer = 0f;
        HideAlerts();
    }
}
