namespace ManageEmployees.Domain;

public static class TableName
{
    public static string Roles = "Roles";
}
public static class TableColumn
{
    public static string Id = "Id";
    public static string Name = "Name";
    public static string Segmento = "Segmento";
    public static string NormalizedName = "NormalizedName";
}
public static class RoleName
{
    public static string Administrator = "Administrator";
    public static string Director = "Director";
    public static string Leader = "Leader";
    public static string Employee = "Employee";
}

public static class Auth
{
    public const string DecryptKey = "@my-secret-key-@";
    public const string DecriptStringError = "Formato de senha criptografada inválido.";
    public const string DecriptKeyError = "The decryption key must be exactly 16 bytes long.";
}
