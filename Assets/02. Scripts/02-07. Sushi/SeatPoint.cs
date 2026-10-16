using UnityEngine;

public sealed class SeatPoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public Vector3 Position => transform.position;

    public bool TryReserve()
    {
        if (IsOccupied)
            return false;

        IsOccupied = true;
        return true;
    }

    public void Release()
    {
        IsOccupied = false;
    }
}
