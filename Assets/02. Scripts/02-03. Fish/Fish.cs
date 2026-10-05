using System;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [SerializeField] private FishData data;

    [Tooltip("작살에 물고기가 매달릴 기준 위치입니다.")]
    [SerializeField] private Transform hookPoint;

    [Tooltip("평상시 물고기 이동을 담당하는 스크립트입니다.")]
    [SerializeField] private MonoBehaviour movementBehaviour;

    public event Action<Fish> Caught;

    public FishData Data => data;
    public int CurrentHealth { get; private set; }
    public bool IsHooked { get; private set; }

    private Collider2D[] colliders;
    private Rigidbody2D body;

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
