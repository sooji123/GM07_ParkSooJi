using UnityEngine;

public static class BoatActionLock
{
    private static Object owner;

    public static bool IsLocked => owner != null;

    public static void Lock(Object requester)
    {
        if (requester != null)
            owner = requester;
    }

    public static void Unlock(Object requester)
    {
        if (owner == requester)
            owner = null;
    }
}
