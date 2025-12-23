namespace RestaurantAPI.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Waiter = "Waiter";
    public const string User = "User";

    public static bool IsValid(string role)
    {
        return role == Admin || role == Waiter || role == User;
    }

    public static string[] GetAll()
    {
        return new[] { Admin, Waiter, User };
    }
}

