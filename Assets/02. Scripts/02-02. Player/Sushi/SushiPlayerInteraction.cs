using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SushiPlayerInteraction : MonoBehaviour
{
    [SerializeField] private SushiPlayerController playerController;
    [SerializeField] private GameObject carriedFoodObject;
    [SerializeField] private Image carriedFoodImage;

    private readonly List<CustomerController> nearbyCustomers = new();
    private SushiChefInteraction nearbyChef;
    private FoodOrder carriedOrder;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<SushiPlayerController>();
        HideCarriedFood();
    }

    private void Update()
    {
        if (playerController == null || !playerController.ControlEnabled)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            return;

        if (carriedOrder == null)
            TryPickUpFood();
        else
            TryServeFood();
    }

    private void TryPickUpFood()
    {
        if (nearbyChef == null || !nearbyChef.TryTakeReadyOrder(out FoodOrder order))
            return;

        carriedOrder = order;
        if (carriedFoodImage != null && order.Fish.DishSprite != null)
            carriedFoodImage.sprite = order.Fish.DishSprite;
        if (carriedFoodObject != null)
            carriedFoodObject.SetActive(true);
    }

    private void TryServeFood()
    {
        for (int i = nearbyCustomers.Count - 1; i >= 0; i--)
        {
            CustomerController customer = nearbyCustomers[i];
            if (customer == null)
            {
                nearbyCustomers.RemoveAt(i);
                continue;
            }

            if (!customer.TryReceive(carriedOrder))
                continue;

            carriedOrder = null;
            HideCarriedFood();
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SushiChefInteraction chef = other.GetComponentInParent<SushiChefInteraction>();
        if (chef != null)
            nearbyChef = chef;

        CustomerController customer = other.GetComponentInParent<CustomerController>();
        if (customer != null && !nearbyCustomers.Contains(customer))
            nearbyCustomers.Add(customer);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SushiChefInteraction chef = other.GetComponentInParent<SushiChefInteraction>();
        if (chef != null && chef == nearbyChef)
            nearbyChef = null;

        CustomerController customer = other.GetComponentInParent<CustomerController>();
        if (customer != null)
            nearbyCustomers.Remove(customer);
    }

    private void HideCarriedFood()
    {
        if (carriedFoodObject != null)
            carriedFoodObject.SetActive(false);
    }
}
