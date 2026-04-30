---
applyTo: "**/*.cs"
---

# C# Instructions - ManageEmployees Backend

## Version and Target
- .NET 8.0
- C# 12 (nullable enabled, implicit usings)
- LangVersion: latest

## Code Style

### Constructor Injection (project standard)
```csharp
public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEncryptionService _encryptationService;

    public UserService(
        UserManager<User> userManager,
        IAuthService authService,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IEncryptionService encryptationService)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _encryptationService = encryptationService ?? throw new ArgumentNullException(nameof(encryptationService));
    }
}
```

### DTOs as Classes with DataAnnotations
```csharp
public class CreateUser
{
    [Required, MaxLength(256)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(256)]
    public string LastName { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8), MaxLength(64)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,64}$")]
    public string Password { get; set; } = null!;

    [Required, Compare("Password")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = null!;
}

// Do NOT use records for DTOs in this project
```

### Entities with ASP.NET Identity
```csharp
public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DocNumber { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
}
```

### Nullable Reference Types
```csharp
public string? ManagerId { get; set; }              // Optional
public string FirstName { get; set; } = null!;      // Required (set externally)
public string Email { get; set; } = string.Empty;   // Required with default
```

### Models
```csharp
public class Token
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string UserId { get; set; } = null!;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

### Controllers (thin — no try/catch)
```csharp
// Exceptions bubble to GlobalExceptionHandlerMiddleware
[ApiController]
[Route("[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskQueryService _taskQueryService;

    public TasksController(ITaskQueryService taskQueryService)
    {
        _taskQueryService = taskQueryService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _taskQueryService.GetAllTasksAsync(page, pageSize);
        return Ok(result);
    }
}
```

### Exception Handling
```csharp
// BusinessException for business rule violations
throw new BusinessException("User not found!");
throw new BusinessException("Couldn't create user!", errorsList);

// NotFoundException for 404 responses
throw new NotFoundException("Task not found.");
```

### Repositories (raw ADO.NET)
```csharp
// Use parameterized queries — NEVER string concatenation
using var connection = _connectionFactory.CreateConnection();
using var command = new SqlCommand(sql, connection);
command.Parameters.AddWithValue("@Id", id);
await connection.OpenAsync();
```

### Structured Logging
```csharp
// Correct — structured logging
_logger.LogInformation("Signing in user {UserName}", credentials.UserName);

// WRONG — string interpolation in logs
_logger.LogInformation($"Signing in {credentials.UserName}");
```

### Async Methods
```csharp
// All async methods must end with Async suffix
Task<Token> SignInAsync(NetworkCredential credentials);
Task<PagedResult<TaskItem>> GetAllTasksAsync(int page, int pageSize);
```
