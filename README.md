# ManageEmployees — Backend (.NET 8 Web API)

## Overview

RESTful API for employee and task management built with **.NET 8**, **Clean Architecture**, **ASP.NET Identity** with custom ADO.NET stores, and **JWT Bearer** authentication.

---

## Architecture

```
ManageEmployees.Api/                    → Controllers, Program.cs, Middleware
ManageEmployees.Domain/                 → Entities, DTOs, Interfaces, Models, Constants
ManageEmployees.Services/               → Business logic (AuthService, UserService, TaskService)
ManageEmployees.Infra.Data/             → ADO.NET repositories, Identity stores, SQL scripts
ManageEmployees.Infra.CrossCutting.IoC/ → DI registration, Identity config
ManageEmployees.UnitTests/              → NUnit unit tests (62 tests)
ManageEmployees.IntegrationTests/       → NUnit integration tests (22 tests)
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| ADO.NET (no EF Core/Dapper) | Direct SQL access with parameterized queries |
| Custom Identity stores | `IUserStore`, `IRoleStore` implemented with raw SQL |
| SQL Server | Relational storage via `Microsoft.Data.SqlClient` |
| JWT Bearer | Stateless auth with access + refresh tokens |
| Clean Architecture | Separation of concerns across layers |
| CQRS-Lite | Query/Command service interfaces per domain |
| Global Exception Handler | RFC 7807 error responses via middleware |

---

## Prerequisites

- **.NET 8 SDK**
- **SQL Server** (LocalDB, Express, or Docker)
- Connection string in `appsettings.json`

## Getting Started

### Local Development

```bash
cd ManageEmployees.Api

# Build
dotnet build

# Run (database auto-initialized on startup)
dotnet run --project ManageEmployees.Api
```

The API starts at `https://localhost:64715` with Swagger UI at the root (`/`).

### Docker

The backend includes a multi-stage Dockerfile (`aspnet:8.0` base → `sdk:8.0` build → publish → runtime).

```bash
# Run the full stack from the repository root
docker compose up --build
```

| Setting | Local | Docker |
|---------|-------|--------|
| URL | `https://localhost:64715` | `http://localhost:64715` |
| SQL Server | `localhost,1433` | `sqlserver,1433` (container name) |
| Protocol | HTTPS | HTTP (`ASPNETCORE_URLS=http://+:8080`) |

In Docker, the connection string is overridden via environment variable `ConnectionStrings__DBConnection` to point to the `sqlserver` container. Test projects are excluded from the Docker image.

### Database Initialization

On startup, `DatabaseInitializer` automatically:
1. Creates the database `ManageEmployees` if it doesn't exist
2. Executes schema scripts (`001_CreateTables.sql`) — tables + indexes
3. Seeds roles (Administrator, Employee)
4. Seeds admin user
5. Seeds sample tasks

No manual migrations required.

### Database Indexing

All non-clustered indexes are designed based on actual repository query patterns:

| Index | Table | Purpose |
|-------|-------|---------|
| `IX_Users_NormalizedUserName` | Users | Login lookups (UNIQUE, filtered) |
| `IX_Users_NormalizedEmail` | Users | Email lookups (UNIQUE, filtered) |
| `IX_Users_FirstName` | Users | User listing ORDER BY (covering) |
| `IX_Roles_NormalizedName` | Roles | Role lookups — 4 code paths (UNIQUE, filtered) |
| `IX_UserRoles_RoleId` | UserRoles | Reverse FK — GetUsersInRole, RemoveFromRole |
| `IX_RefreshTokens_UserId` | RefreshTokens | Token lookup + 1-per-user (UNIQUE) |
| `IX_Tasks_UserId_CreatedAt` | Tasks | User tasks ordered by date (composite, covering) |
| `IX_Tasks_CreatedAt` | Tasks | All tasks ordered by date (covering) |

Additional: `NEWSEQUENTIALID()` for GUID PKs, `CHECK` constraint on Task.Status, named constraints (`PK_`, `FK_`, `CK_`), filtered indexes on nullable Identity columns.

---

## Default Credentials

| Email | Password | Role |
|-------|----------|------|
| `admin@company.com` | `Admin123!` | Administrator |

---

## API Endpoints

### Authentication (LoginController)

| Method | Route | Auth Required | Description |
|--------|-------|:---:|-------------|
| POST | `/Login/SignIn` | No | Authenticate user, returns JWT |
| POST | `/Login/SignUp` | No | Register new user |
| PUT | `/Login/{userId}` | Yes | Update user |
| DELETE | `/Login/{userId}` | Yes | Delete user |
| GET | `/Login/ListAll` | Yes | List all users (paginated, filterable) |
| POST | `/Login/SignOut` | Yes | Sign out (invalidate refresh token) |

#### GET `/Login/ListAll` — Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |
| `search` | string? | null | Search by FirstName, LastName, Email, DocNumber, PhoneNumber (LIKE) |
| `role` | string? | null | Filter by exact role name (e.g. `Administrator`, `Employee`) |

### Tasks (TasksController)

| Method | Route | Auth Required | Description |
|--------|-------|:---:|-------------|
| GET | `/Tasks` | Yes | List all tasks (paginated, filterable) |
| GET | `/Tasks/{id}` | Yes | Get task by ID |
| POST | `/Tasks` | Yes | Create task (UserId extracted from JWT) |
| PUT | `/Tasks/{id}` | Yes | Update task |
| DELETE | `/Tasks/{id}` | Yes | Delete task |

#### GET `/Tasks` — Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page |
| `search` | string? | null | Search by Title or Description (LIKE) |
| `status` | string? | null | Filter by exact status (`Pending`, `InProgress`, `Completed`) |
| `startDate` | DateTime? | null | Filter tasks with DueDate >= startDate |
| `endDate` | DateTime? | null | Filter tasks with DueDate <= endDate (inclusive, uses `< endDate + 1 day`) |

When date filters are active, results are sorted by `DueDate ASC`; otherwise by `CreatedAt DESC`.

### Pagination Response

All list endpoints return `PagedResult<T>`:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

### Task Status Values
- `Pending` (default)
- `InProgress`
- `Completed`

---

## Project Structure Details

### Domain Layer
- **Entities**: `User` (extends IdentityUser + FirstName, LastName, DocNumber), `TaskItem`, `RefreshToken`
- **DTOs**: `CreateUser`, `UpdateUser`, `CreateTaskRequest`, `UpdateTaskRequest`, `UserDto`, `SignInRequest`
- **Interfaces**: CQRS service contracts (`IUserQueryService`, `IUserCommandService`, `ITaskQueryService`, `ITaskCommandService`) and repository contracts
- **Models**: `Token` (AccessToken, RefreshToken, Role, FirstName, UserId), `Error`, `PagedResult<T>` (Items, Page, PageSize, TotalCount, TotalPages)
- **Exceptions**: `BusinessException`, `NotFoundException`
- **Constants**: Role names (Administrator, Employee), task statuses, table names

### Services Layer
- **UserService**: Sign-in (password validation + JWT), sign-up, update, delete, list users — implements `IUserQueryService` + `IUserCommandService`
- **AuthService**: JWT generation with claims, refresh token swap, token removal
- **TaskService**: CRUD with status validation — implements `ITaskQueryService` + `ITaskCommandService`

### API Layer
- **Controllers**: Thin controllers with no try/catch — exceptions bubble to middleware
- **GlobalExceptionHandlerMiddleware**: Maps `NotFoundException` → 404, `BusinessException` → 400, `UnauthorizedAccessException` → 403, others → 500 (RFC 7807 format)

### Infrastructure Layer
- **Identity**: `UserStore` (IUserStore, IUserPasswordStore, IUserRoleStore, IUserSecurityStampStore), `RoleStore` (IRoleStore)
- **Repositories**: `RefreshTokenRepository`, `TaskRepository`, `UserRepository` — all raw ADO.NET with parameterized queries
- **Connection**: `IDbConnectionFactory` / `SqlConnectionFactory` (Singleton)
- **DatabaseInitializer**: Idempotent schema creation, index provisioning, and data seeding

### Server-Side Filtering & Pagination

All list endpoints support **server-side pagination** (`OFFSET/FETCH NEXT`) with dynamic `WHERE` clause building:

| Repository | Searchable Fields | Filters | Default Sort |
|------------|-------------------|---------|--------------|
| `TaskRepository` | Title, Description (LIKE) | Status (exact), StartDate/EndDate (range) | CreatedAt DESC (DueDate ASC with date filters) |
| `UserRepository` | FirstName, LastName, Email, DocNumber, PhoneNumber (LIKE) | Role (exact) | FirstName ASC |

- SQL injection is prevented via parameterized queries (`@Search`, `@Status`, `@StartDate`, `@EndDate`, `@Role`)
- EndDate filter is inclusive: uses `DueDate < @EndDate` where `@EndDate = endDate.Date.AddDays(1)`

---

## Tests

**Framework**: NUnit + Moq + FluentAssertions

```bash
dotnet test
```

**84 tests** (62 unit + 22 integration), all passing:

### Unit Tests (ManageEmployees.UnitTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| API | TasksControllerTests | 11 | HTTP responses, JWT claim extraction, error delegation |
| API | LoginControllerTests | 12 | SignIn/SignUp/Update/Delete/SignOut/ListAll responses |
| Services | TaskServiceTests | 10 | CRUD, status validation, not-found errors |
| Services | UserServiceTests | 15 | Sign-in, sign-up, update, delete, list |
| Services | AuthServiceTests | 5 | Token generation, refresh swap, removal |
| Domain | ConstantsTests | 2 | Role names, task statuses |
| Domain | SignInRequestTests | 4 | DTO field validation |
| Domain | ExceptionsTests | 3 | BusinessException constructors, trace ID |
| | **Subtotal** | **62** | |

### Integration Tests (ManageEmployees.IntegrationTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| Data Access | TaskRepositoryTests | 11 | CRUD, ordering, null description, user filtering |
| Data Access | RefreshTokenRepositoryTests | 5 | Create, get, delete, multiple tokens |
| Data Access | UserRepositoryTests | 6 | Roles join, ordering, nullable fields, empty list |
| | **Subtotal** | **22** | |

| | **Total** | **84** | |

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DBConnection": "Server=localhost\\SQLExpress;Database=ManageEmployees;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtBearerTokenSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "https://localhost:62577",
    "Audience": "ManageEmployees.Api",
    "ExpiresAt": "7"
  }
}
```

### Logging
- **Serilog** with console sink only