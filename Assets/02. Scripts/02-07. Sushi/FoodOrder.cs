public enum FoodOrderState
{
    Cooking,
    Ready,
    Carried,
    Served
}

public sealed class FoodOrder
{
    public FishData Fish { get; }
    public CustomerController Customer { get; }
    public FoodOrderState State { get; set; }
    public CookingOrderView View { get; set; }

    public FoodOrder(FishData fish, CustomerController customer)
    {
        Fish = fish;
        Customer = customer;
        State = FoodOrderState.Cooking;
    }
}
