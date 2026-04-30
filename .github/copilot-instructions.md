# Copilot Instructions - ManageEmployees Backend

## Architecture

**.NET 8 Web API** with **Clean Architecture**, **ASP.NET Identity** (custom ADO.NET stores), and **JWT Bearer** authentication.

### Project Structure

| Project | Responsibility |
|---------|----------------|
| `ManageEmployees.Api` | Controllers, Program.cs (DI, middlewares, Swagger, Serilog), GlobalExceptionHandlerMiddleware |
| `ManageEmployees.Domain` | Entities, DTOs, Interfaces (Services + Repositories), Exceptions, Models, Constants |
| `ManageEmployees.Services` | Application services (Auth, User, Task, Encryption), Settings (JWT, Log) |
| `ManageEmployees.Infra.Data` | ADO.NET Repositories, Identity stores (UserStore, RoleStore), DatabaseInitializer, SQL Scripts, Connection factory |
| `ManageEmployees.Infra.CrossCutting.IoC` | DI registration, Identity config, JWT security events |
| `ManageEmployees.UnitTests` | Unit tests (NUnit, Moq, FluentAssertions) — 62 tests |
| `ManageEmployees.IntegrationTests` | Integration tests (NUnit, SQL Server) — 22 tests |

### Naming Conventions

```
Controller:          LoginController, TasksController          -> Api/Controllers/
Interface Service:   IUserService, IUserQueryService           -> Domain/Interfaces/Services/
Implementation:      UserService, TaskService                  -> Services/Services/
DTO (class):         CreateUser, UpdateUser, CreateTaskRequest -> Domain/DTO/
Entity:              User (extends IdentityUser), TaskItem     -> Domain/Entities/
Model:               Token, Error, PagedResult<T>              -> Domain/Models/
Exception:           BusinessException, NotFoundException      -> Domain/Exceptions/
Interface Repo:      ITaskRepository, IUserRepository          -> Domain/Interfaces/Repositories/
Impl Repo:           TaskRepository, UserRepository            -> Infra.Data/Repositories/
Identity Store:      UserStore, RoleStore                      -> Infra.Data/Identity/
Connection:          IDbConnectionFactory, SqlConnectionFactory -> Infra.Data/Connection/
DI:                  DependencyInjection.cs                    -> Infra.CrossCutting.IoC/Configuration/
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| ADO.NET (no EF Core/Dapper) | Direct SQL access with parameterized queries |
| Custom Identity stores | `IUserStore`, `IRoleStore` implemented with raw SQL |
| SQL Server | Relational storage via `Microsoft.Data.SqlClient` |
| JWT Bearer | Stateless auth with access + refresh tokens |
| Clean Architecture | Separation of concerns across layers |
| CQRS-Lite | Query/Command service interfaces per domain (`IUserQueryService`, `IUserCommandService`) |
| Global Exception Handler | Standardized error responses via middleware (no try/catch in controllers) |
| DatabaseInitializer | Idempotent schema creation + index provisioning + data seeding on startup |

---

## Creating a New Endpoint — Checklist

1. **Controller** in `Api/Controllers/` (thin — no try/catch, exceptions go to middleware):
```csharp
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _taskQueryService.GetAllTasksAsync(page, pageSize, search, status);
        return Ok(result);
    }
}
```

2. **DTO** (class with DataAnnotations):
```csharp
public class CreateUser
{
    [Required, MaxLength(256)]
    public string FirstName { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8), MaxLength(64)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])...")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = null!;
}
```

3. **Service interfaces** in `Domain/Interfaces/Services/` (CQRS split):
```csharp
public interface ITaskQueryService
{
    Task<PagedResult<TaskItem>> GetAllTasksAsync(int page, int pageSize, string? search, string? status, DateTime? startDate = null, DateTime? endDate = null);
    Task<TaskItem> GetTaskByIdAsync(Guid id);
}

public interface ITaskCommandService
{
    Task<TaskItem> CreateTaskAsync(CreateTaskRequest request, string userId);
    Task<TaskItem> UpdateTaskAsync(Guid id, UpdateTaskRequest request);
    Task DeleteTaskAsync(Guid id);
}
```

4. **Repository interface** in `Domain/Interfaces/Repositories/`:
```csharp
public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> GetAllAsync(int page, int pageSize, string? search, string? status, DateTime? startDate, DateTime? endDate);
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<TaskItem> UpdateAsync(TaskItem task);
    Task DeleteAsync(Guid id);
}
```

5. **Register DI** in `Infra.CrossCutting.IoC/Configuration/DependencyInjection.cs`:
```csharp
services.AddScoped<ITaskQueryService, TaskService>();
services.AddScoped<ITaskCommandService, TaskService>();
services.AddScoped<ITaskRepository, TaskRepository>();
```

---

## Authentication

- **ASP.NET Identity** with custom `User` entity (extends `IdentityUser` + FirstName, LastName, DocNumber, ManagerId)
- **JWT Bearer** with refresh token
- Token read from `Authorization` header
- JWT configuration in `appsettings.json` → `JwtBearerTokenSettings`
- Passwords encrypted on frontend (AES-128 CBC, CryptoJS) and decrypted on backend (`EncryptionService`)

### Roles and Hierarchy

| Role | Level |
|------|-------|
| Administrator | Highest (seed) |
| Director | High |
| Leader | Medium |
| Employee | Base |

Rule: Users can only create/edit/delete others at same or lower level.

---

## Database (SQL Server)

### Configuration

- **DBMS:** SQL Server (via Docker or SQL Express)
- **Connection string:** `appsettings.json` → `ConnectionStrings.DBConnection`
- **Data access:** Raw ADO.NET (`Microsoft.Data.SqlClient`) — no ORM
- **Identity stores:** `UserStore` and `RoleStore` with ADO.NET
- **Initialization:** `DatabaseInitializer` — creates DB, executes `001_CreateTables.sql`, seeds data

### Tables

| Table | Description |
|-------|-------------|
| `Users` | Users (Identity + FirstName, LastName, DocNumber, ManagerId) |
| `Roles` | Roles (Administrator, Employee) |
| `UserRoles` | User-role mapping (composite PK) |
| `RefreshTokens` | Refresh tokens (unique per user) |
| `Tasks` | Tasks (Title, Description, Status, DueDate, UserId, CreatedAt) |

### Server-Side Filtering & Pagination

All list endpoints use `OFFSET/FETCH NEXT` with dynamic `WHERE` clause building:

| Repository | Searchable Fields | Filters | Default Sort |
|------------|-------------------|---------|--------------|
| `TaskRepository` | Title, Description (LIKE) | Status (exact), StartDate/EndDate (range) | CreatedAt DESC (DueDate ASC with date filters) |
| `UserRepository` | FirstName, LastName, Email, DocNumber, PhoneNumber (LIKE) | Role (exact) | FirstName ASC |

SQL injection prevented via parameterized queries (`@Search`, `@Status`, `@StartDate`, `@EndDate`, `@Role`).

---

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|:----:|-------------|
| POST | `/Login/SignIn` | No | Authenticate user, returns JWT |
| POST | `/Users` | No | Register new user |
| PUT | `/Users/{userId}` | Yes | Update user |
| DELETE | `/Users/{userId}` | Yes | Delete user |
| GET | `/Users` | Yes | List users (paginated, filterable) |
| POST | `/Login/SignOut` | Yes | Sign out (invalidate refresh token) |
| GET | `/Tasks` | Yes | List tasks (paginated, filterable) |
| GET | `/Tasks/{id}` | Yes | Get task by ID |
| POST | `/Tasks` | Yes | Create task |
| PUT | `/Tasks/{id}` | Yes | Update task |
| DELETE | `/Tasks/{id}` | Yes | Delete task |

### Pagination Response (`PagedResult<T>`)

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

---

## Commands

```bash
cd ManageEmployees.Api
dotnet build
dotnet run --project ManageEmployees.Api    # https://localhost:64715 (Swagger at root)
dotnet test                                 # 84 tests (62 unit + 22 integration)
```

### Default Credentials (seed)
- **Email:** `admin@company.com`
- **Password:** `Admin123!`

---

## Language

- **Technical code:** English
- **UI and messages:** English
- **Comments:** English
