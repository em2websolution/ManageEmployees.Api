namespace ManageEmployees.Domain;

public static class RoleName
{
    public const string Administrator = "Administrator";
    public const string Employee = "Employee";
}

public static class TaskItemStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";

    public static readonly string[] All = { Pending, InProgress, Completed };
}
