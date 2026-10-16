using System.Collections.Generic;
using UnityEngine;

public sealed class SeatManager : MonoBehaviour
{
    private SeatPoint[] seats;
    private readonly List<SeatPoint> freeSeats = new();

    private void Awake()
    {
        seats = GetComponentsInChildren<SeatPoint>(true);
    }

    public bool TryReserveRandomSeat(out SeatPoint seat)
    {
        seat = null;
        freeSeats.Clear();

        if (seats == null || seats.Length == 0)
            seats = GetComponentsInChildren<SeatPoint>(true);

        foreach (SeatPoint candidate in seats)
        {
            if (candidate != null && !candidate.IsOccupied)
                freeSeats.Add(candidate);
        }

        if (freeSeats.Count == 0)
            return false;

        SeatPoint selected = freeSeats[Random.Range(0, freeSeats.Count)];
        if (!selected.TryReserve())
            return false;

        seat = selected;
        return true;
    }
}
