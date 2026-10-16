using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private CustomerController[] customerPrefabs = new CustomerController[0];
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform pathPoint1;
    [SerializeField] private Transform pathPoint2;
    [SerializeField] private SeatManager seatManager;
    [SerializeField] private CookingManager cookingManager;
    [SerializeField] private SushiShiftController shiftController;
    [SerializeField, Min(0.1f)] private float minSpawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float maxSpawnInterval = 7f;
    [SerializeField, Min(0.1f)] private float customerMoveSpeed = 2f;
    [SerializeField, Min(0f)] private float eatingDuration = 3f;

    private readonly List<CustomerController> activeCustomers = new();
    private Coroutine spawnRoutine;

    public void BeginSpawning()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    public void NotifyCustomerLeft(CustomerController customer)
    {
        activeCustomers.Remove(customer);
    }

    public bool TryCompleteSale(int foodPrice)
    {
        return shiftController != null && shiftController.TryRecordCompletedSale(foodPrice);
    }

    public void CloseStore()
    {
        StopSpawning();

        foreach (CustomerController customer in activeCustomers)
        {
            if (customer != null)
                Destroy(customer.gameObject);
        }

        activeCustomers.Clear();
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float delay = Random.Range(
                Mathf.Min(minSpawnInterval, maxSpawnInterval),
                Mathf.Max(minSpawnInterval, maxSpawnInterval)
            );
            yield return new WaitForSeconds(delay);

            CustomerController customerPrefab = GetRandomCustomerPrefab();
            if (customerPrefab == null || spawnPoint == null ||
                pathPoint1 == null || pathPoint2 == null ||
                seatManager == null || cookingManager == null ||
                !cookingManager.HasAvailableFish)
            {
                continue;
            }

            if (!seatManager.TryReserveRandomSeat(out SeatPoint seat))
                continue;

            CustomerController customer = Instantiate(
                customerPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            activeCustomers.Add(customer);
            customer.Initialize(
                seat,
                pathPoint1.position,
                pathPoint2.position,
                spawnPoint.position,
                cookingManager,
                this,
                customerMoveSpeed,
                eatingDuration
            );
        }
    }

    private CustomerController GetRandomCustomerPrefab()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0)
            return null;

        int startIndex = Random.Range(0, customerPrefabs.Length);
        for (int offset = 0; offset < customerPrefabs.Length; offset++)
        {
            CustomerController prefab = customerPrefabs[(startIndex + offset) % customerPrefabs.Length];
            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}
