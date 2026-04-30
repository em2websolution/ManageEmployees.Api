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
ManageEmployees.UnitTests/              → NUnit unit tests (111 tests)
ManageEmployees.IntegrationTests/       → NUnit integration tests (75 tests)
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
| Global Exception Handler | Standardized error responses via middleware |

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
# Run the full stack from the backend directory (ManageEmployees.Api/)
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
| POST | `/Users` | No | Register new user |
| PUT | `/Users/{userId}` | Yes | Update user |
| DELETE | `/Users/{userId}` | Yes | Delete user |
| GET | `/Users` | Yes | List all users (paginated, filterable) |
| POST | `/Login/SignOut` | Yes | Sign out (invalidate refresh token) |

#### GET `/Users` — Query Parameters

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
- **GlobalExceptionHandlerMiddleware**: Maps `NotFoundException` → 404, `BusinessException` → 400, `UnauthorizedAccessException` → 403, others → 500

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

**186 tests** (111 unit + 75 integration), all passing:

### Unit Tests (ManageEmployees.UnitTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| API | TasksControllerTests | 11 | HTTP responses, JWT claim extraction, error delegation |
| API | LoginControllerTests | 12 | SignIn/SignUp/Update/Delete/SignOut/ListAll responses |
| Services | TaskServiceTests | 10 | CRUD, status validation, not-found errors |
| Services | UserServiceTests | 15 | Sign-in, sign-up, update, delete, list |
| Services | AuthServiceTests | 5 | Token generation, refresh swap, removal |
| Services | UserServiceAdditionalTests | 6 | Edge cases, duplicate email, roles |
| IoC | DependencyInjectionTests | 10 | All DI registrations verified |
| IoC | IdentityConfigTests | 7 | JWT options, password config, cookie handler |
| IoC | JwtSecurityExtensionEventsTests | 2 | Token validation events |
| API Middleware | GlobalExceptionHandlerMiddlewareTests | 8 | Exception → HTTP status mapping |
| Domain | ConstantsTests | 2 | Role names, task statuses |
| Domain | SignInRequestTests | 4 | DTO field validation |
| Domain | ExceptionsTests | 4 | BusinessException, NotFoundException constructors |
| Domain | PagedResultTests | 6 | Pagination model |
| Domain | ApiErrorResponseTests | 4 | Error response model |
| Data | SqlConnectionFactoryTests | 3 | Connection creation |
| Settings | LogSettingsTests | 2 | Log middleware |
| | **Subtotal** | **111** | |

### Integration Tests (ManageEmployees.IntegrationTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| Identity | UserStoreTests | 24 | IUserStore, IUserPasswordStore, IUserRoleStore, IUserSecurityStampStore |
| Identity | RoleStoreTests | 12 | IRoleStore full CRUD + accessors |
| Configuration | IdentityConfigTests | 5 | DI registration with real config |
| Data Access | TaskRepositoryTests | 18 | CRUD, filters, pagination, date ranges |
| Data Access | RefreshTokenRepositoryTests | 5 | Create, get, delete, multiple tokens |
| Data Access | UserRepositoryTests | 11 | CRUD, roles join, search, pagination |
| | **Subtotal** | **75** | |

| | **Total** | **186** | |

---

## Code Quality — SonarQube

The project is analyzed with **SonarQube 9.9 LTS Community Edition**.

### Quality Report

| Metric | Result |
|--------|--------|
| **Coverage** | **82.0%** |
| **Bugs** | 0 |
| **Vulnerabilities** | 0 |
| **Code Smells** | 0 |
| **Duplication** | 0.0% |
| **Security Hotspots Reviewed** | 100% |

### Coverage by Assembly

| Assembly | Coverage |
|----------|----------|
| ManageEmployees.Domain | 100% |
| ManageEmployees.Services | 98.9% |
| ManageEmployees.Infra.CrossCutting.IoC | 98.9% |
| ManageEmployees.Infra.Data | 79.3% |
| ManageEmployees.Api | 57.6% |

> `Program.cs` (startup bootstrap) and `DatabaseInitializer` (one-time schema setup) account for the untested lines. All business logic is at 98%+ coverage.

### Running Analysis

```bash
# Prerequisites: dotnet-sonarscanner, Java 17+
dotnet sonarscanner begin \
  /k:"manage-employees-api" \
  /d:sonar.host.url="http://localhost:9000" \
  /d:sonar.token="<YOUR_TOKEN>" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
  /d:sonar.exclusions="**/Migrations/**,**/obj/**,**/bin/**"

dotnet build ManageEmployees.sln

dotnet test ManageEmployees.UnitTests/ManageEmployees.UnitTests.csproj \
  --no-build --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

dotnet test ManageEmployees.IntegrationTests/ManageEmployees.IntegrationTests.csproj \
  --no-build --settings ManageEmployees.IntegrationTests/coverage.runsettings \
  --collect:"XPlat Code Coverage"

dotnet sonarscanner end /d:sonar.token="<YOUR_TOKEN>"
```

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

### Project Documentation
The files are in the “docs” folder

| Document | Description |
|----------|-------------|
| [PRESENTATION.md](PRESENTATION.md) | This file — thought process and exercise summary |
| [GENAI_USAGE.md](GENAI_USAGE.md) | AI usage methodology, contributions, and corrections |
| [TEST_VALIDATION_PLAN.md](TEST_VALIDATION_PLAN.md) | Requirement traceability matrix and test inventory |