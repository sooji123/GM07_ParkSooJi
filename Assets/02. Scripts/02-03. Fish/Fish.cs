using System;
using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [SerializeField] private FishData data;

    [Tooltip("작살에 물고기가 매달릴 기준 위치입니다.")]
    [SerializeField] private Transform hookPoint;

    [Tooltip("평상시 물고기 이동을 담당하는 스크립트입니다.")]
    [SerializeField] private MonoBehaviour movementBehaviour;

    [Header("공용 사망 연출")]
    [Tooltip("모든 물고기가 함께 사용하는 사망 색상과 사라지는 시간 설정입니다.")]
    [SerializeField] private FishDeathSettings deathSettings;

    public event Action<Fish> Caught;

    public FishData Data => data;
    public int CurrentHealth { get; private set; }
    public bool IsHooked { get; private set; }

    private Collider2D[] colliders;
    private Rigidbody2D body;
    private SpriteRenderer[] spriteRenderers;
    private Animator[] animators;
    private Vector3 capturedPosition;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"{name}: FishData가 지정되지 않았습니다.", this);
            enabled = false;
            return;
        }

        CurrentHealth = data.MaxHealth;
        colliders = GetComponentsInChildren<Collider2D>();
        body = GetComponent<Rigidbody2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        animators = GetComponentsInChildren<Animator>(true);
    }

    public HarpoonHitResult HitByHarpoon(int damage, int struggleHealthThreshold)
    {
        int appliedDamage = Mathf.Max(0, damage);
        bool canCatchDirectly = CurrentHealth <= appliedDamage;
        CurrentHealth = Mathf.Max(0, CurrentHealth - appliedDamage);

        if (canCatchDirectly)
            return HarpoonHitResult.DirectCatch;

        return CurrentHealth <= Mathf.Max(0, struggleHealthThreshold)
            ? HarpoonHitResult.Struggle
            : HarpoonHitResult.Escaped;
    }

    public void BeginHook(Vector3 playerPosition)
    {
        capturedPosition = transform.position;
        IsHooked = true;

        if (movementBehaviour is FishAgent fishAgent)
            fishAgent.FaceAwayFrom(playerPosition);

        if (movementBehaviour != null)
            movementBehaviour.enabled = false;

        foreach (Collider2D fishCollider in colliders)
            fishCollider.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }

    public void MoveToHook(Vector3 hookPosition)
    {
        if (!IsHooked) return;

        if (hookPoint != null)
            transform.position += hookPosition - hookPoint.position;
        else
            transform.position = hookPosition;
    }

    public void CompleteCatch()
    {
        IsHooked = false;
        Caught?.Invoke(this);
        gameObject.SetActive(false);
    }

    /// <summary>보관할 수 없는 물고기를 처음 포획된 위치에서 사망 처리합니다.</summary>
    public void DieAtCapturedPosition()
    {
        IsHooked = false;
        transform.position = capturedPosition;

        if (movementBehaviour != null)
            movementBehaviour.enabled = false;

        foreach (Collider2D fishCollider in colliders)
            fishCollider.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        foreach (Animator fishAnimator in animators)
            fishAnimator.enabled = false;

        foreach (SpriteRenderer fishRenderer in spriteRenderers)
        {
            Color color = deathSettings != null
                ? deathSettings.DeadColor
                : new Color(0.3f, 0.3f, 0.3f, 1f);
            color.a = fishRenderer.color.a;
            fishRenderer.color = color;
        }

        StartCoroutine(DespawnDeadFish());
    }

    private IEnumerator DespawnDeadFish()
    {
        float despawnDelay = deathSettings != null ? deathSettings.DespawnDelay : 10f;
        if (despawnDelay > 0f)
            yield return new WaitForSeconds(despawnDelay);
        gameObject.SetActive(false);
    }

    public void Release()
    {
        IsHooked = false;

        if (movementBehaviour != null)
            movementBehaviour.enabled = true;

        foreach (Collider2D fishCollider in colliders)
            fishCollider.enabled = true;

        if (body != null)
            body.simulated = true;
    }
}
