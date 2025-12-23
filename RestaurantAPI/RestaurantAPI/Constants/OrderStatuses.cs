namespace RestaurantAPI.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static bool IsValid(string status)
    {
        return status == Pending || 
               status == Processing || 
               status == Completed || 
               status == Cancelled;
    }

    public static string[] GetAll()
    {
        return new[] { Pending, Processing, Completed, Cancelled };
    }
}

