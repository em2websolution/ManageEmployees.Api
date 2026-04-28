namespace ManageEmployees.Domain;

public static class RoleName
{
    public static string Administrator = "Administrator";
    public static string Employee = "Employee";
}

public static class TaskItemStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";

    public static readonly string[] All = { Pending, InProgress, Completed };
}
