using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CustomerState
{
    Entering,
    WaitingForFood,
    Eating,
    Leaving
}

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class CustomerController : MonoBehaviour
{
    private static readonly int AnimationStateParameter = Animator.StringToHash("State");

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject orderImage;
    [SerializeField] private Image requestedFoodImage;
    [SerializeField] private GameObject coinImage;
    [SerializeField] private TMP_Text coinText;
    [SerializeField, Min(0f)] private float coinDisplayDuration = 1f;

    private Rigidbody2D body;
    private SeatPoint seat;
    private CookingManager cookingManager;
    private CustomerSpawner owner;
    private FoodOrder order;
    private Vector2[] entryPath;
    private int entryPathIndex;
    private Vector2 targetPosition;
    private Vector2 exitPosition;
    private float moveSpeed;
    private float eatDuration;
    private bool seatReleased;

    public CustomerState State { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    public void Initialize(
        SeatPoint assignedSeat,
        Vector3 pathPoint1,
        Vector3 pathPoint2,
        Vector3 exit,
        CookingManager manager,
        CustomerSpawner spawner,
        float speed,
        float eatingTime)
    {
        seat = assignedSeat;
        cookingManager = manager;
        owner = spawner;
        exitPosition = exit;
        entryPath = new[]
        {
            (Vector2)pathPoint1,
            (Vector2)pathPoint2,
            (Vector2)seat.Position
        };
        entryPathIndex = 0;
        targetPosition = entryPath[entryPathIndex];
        moveSpeed = Mathf.Max(0.1f, speed);
        eatDuration = Mathf.Max(0f, eatingTime);
        SetState(CustomerState.Entering);

        if (orderImage != null && requestedFoodImage != null)
        {
            orderImage.SetActive(false);
            requestedFoodImage.gameObject.SetActive(false);
        }

        if (coinImage != null)
            coinImage.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (State != CustomerState.Entering && State != CustomerState.Leaving)
            return;

        Vector2 next = Vector2.MoveTowards(
            body.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        if (spriteRenderer != null && Mathf.Abs(next.x - body.position.x) > 0.001f)
            spriteRenderer.flipX = next.x < body.position.x;

        body.MovePosition(next);

        if (Vector2.Distance(next, targetPosition) > 0.03f)
            return;

        if (State == CustomerState.Entering)
            AdvanceEntryPath();
        else
            FinishLeaving();
    }

    private void AdvanceEntryPath()
    {
        entryPathIndex++;
        if (entryPath != null && entryPathIndex < entryPath.Length)
        {
            targetPosition = entryPath[entryPathIndex];
            return;
        }

        PlaceOrder();
    }

    public bool CanReceive(FoodOrder deliveredOrder)
    {
        return State == CustomerState.WaitingForFood
            && order != null
            && deliveredOrder != null
            && deliveredOrder.Fish == order.Fish;
    }

    public bool TryReceive(FoodOrder deliveredOrder)
    {
        if (!CanReceive(deliveredOrder))
            return false;

        deliveredOrder.State = FoodOrderState.Served;
        SetState(CustomerState.Eating);

        if (SoundManager.HasInstance)
            SoundManager.Instance.PlaySfx(ESfx.CustomerEating);

        if (orderImage != null && requestedFoodImage != null)
        {
            orderImage.SetActive(false);
            requestedFoodImage.gameObject.SetActive(false);
        }

        int foodPrice = order.Fish.SellPrice;
        if (owner != null && owner.TryCompleteSale(foodPrice) && GameSession.HasInstance)
            GameSession.Instance.AddMoney(foodPrice);

        StartCoroutine(EatThenLeave());
        return true;
    }

    private void PlaceOrder()
    {
        if (cookingManager == null || !cookingManager.TryCreateOrder(this, out order))
        {
            BeginLeaving();
            return;
        }

        SetState(CustomerState.WaitingForFood);
        if (orderImage != null && requestedFoodImage != null)
        {
            if (order.Fish.DishSprite != null)
                requestedFoodImage.sprite = order.Fish.DishSprite;

            orderImage.SetActive(true);
            requestedFoodImage.gameObject.SetActive(true);
        }
    }

    private IEnumerator EatThenLeave()
    {
        if (eatDuration > 0f)
            yield return new WaitForSeconds(eatDuration);

        int foodPrice = order.Fish.SellPrice;
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.OrderComplete);

        if (coinText != null)
            coinText.text = foodPrice.ToString();

        if (coinImage != null)
        {
            coinImage.SetActive(true);

            if (coinDisplayDuration > 0f)
                yield return new WaitForSeconds(coinDisplayDuration);

            coinImage.SetActive(false);
        }

        BeginLeaving();
    }

    private void BeginLeaving()
    {
        SetState(CustomerState.Leaving);
        targetPosition = exitPosition;
    }

    private void SetState(CustomerState nextState)
    {
        State = nextState;
        if (animator == null)
            return;

        int animationState = nextState switch
        {
            CustomerState.Entering => 1,
            CustomerState.Leaving => 1,
            CustomerState.Eating => 2,
            _ => 0
        };
        animator.SetInteger(AnimationStateParameter, animationState);
    }

    private void FinishLeaving()
    {
        ReleaseSeat();
        if (owner != null)
            owner.NotifyCustomerLeft(this);
        Destroy(gameObject);
    }

    private void ReleaseSeat()
    {
        if (seatReleased)
            return;

        seatReleased = true;
        if (seat != null)
            seat.Release();
    }

    private void OnDestroy()
    {
        ReleaseSeat();
    }
}
