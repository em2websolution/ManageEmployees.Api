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
ManageEmployees.UnitTests/              → NUnit unit tests (73 tests)
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

---

## Prerequisites

- **.NET 8 SDK**
- **SQL Server** (LocalDB, Express, or Docker)
- Connection string in `appsettings.json`

## Getting Started

```bash
cd ManageEmployees.Api

# Build
dotnet build

# Run (database auto-initialized on startup)
dotnet run --project ManageEmployees.Api
```

The API starts at `https://localhost:64715` with Swagger UI at the root (`/`).

### Database Initialization

On startup, `DatabaseInitializer` automatically:
1. Creates the database `ManageEmployees` if it doesn't exist
2. Executes schema scripts (`001_CreateTables.sql`)
3. Seeds roles (Administrator, Employee)
4. Seeds admin user
5. Seeds sample tasks

No manual migrations required.

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
| GET | `/Login/ListAll` | Yes | List all users with roles |
| POST | `/Login/SignOut` | Yes | Sign out (invalidate refresh token) |

### Tasks (TasksController)

| Method | Route | Auth Required | Description |
|--------|-------|:---:|-------------|
| GET | `/Tasks` | Yes | List all tasks |
| GET | `/Tasks/{id}` | Yes | Get task by ID |
| POST | `/Tasks` | Yes | Create task (UserId extracted from JWT) |
| PUT | `/Tasks/{id}` | Yes | Update task |
| DELETE | `/Tasks/{id}` | Yes | Delete task |

### Task Status Values
- `Pending` (default)
- `InProgress`
- `Completed`

---

## Project Structure Details

### Domain Layer
- **Entities**: `User` (extends IdentityUser + FirstName, LastName, DocNumber), `TaskItem`, `RefreshToken`
- **DTOs**: `CreateUser`, `UpdateUser`, `CreateTaskRequest`, `UpdateTaskRequest`, `UserDto`, `SignInRequest`
- **Interfaces**: Service and repository contracts
- **Models**: `Token` (AccessToken, RefreshToken, Role, FirstName, UserId), `Error`
- **Constants**: Role names (Administrator, Employee), task statuses, table names

### Services Layer
- **UserService**: Sign-in (password validation + JWT), sign-up, update, delete, list users
- **AuthService**: JWT generation with claims, refresh token swap, token removal
- **TaskService**: CRUD with status validation

### Infrastructure Layer
- **Identity**: `UserStore` (IUserStore, IUserPasswordStore, IUserRoleStore, IUserEmailStore, IUserSecurityStampStore), `RoleStore` (IRoleStore)
- **Repositories**: `RefreshTokenRepository`, `TaskRepository`, `UserRepository` — all raw ADO.NET
- **Connection**: `IDbConnectionFactory` / `SqlConnectionFactory` (Singleton)
- **DatabaseInitializer**: Idempotent schema creation and data seeding

---

## Tests

**Framework**: NUnit + Moq + FluentAssertions

```bash
dotnet test
```

**89 tests** (67 unit + 22 integration), all passing:

### Unit Tests (ManageEmployees.UnitTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| API | TasksControllerTests | 12 | HTTP responses, JWT claim extraction, error handling |
| API | LoginControllerTests | 16 | SignIn/SignUp/Update/Delete/SignOut/ListAll responses |
| Services | TaskServiceTests | 10 | CRUD, status validation, not-found errors |
| Services | UserServiceTests | 15 | Sign-in, sign-up, update, delete, list |
| Services | AuthServiceTests | 5 | Token generation, refresh swap, removal |
| Domain | ConstantsTests | 2 | Role names, task statuses |
| Domain | SignInRequestTests | 4 | DTO field validation |
| Domain | ExceptionsTests | 3 | BusinessException constructors, trace ID |
| | **Subtotal** | **67** | |

### Integration Tests (ManageEmployees.IntegrationTests)

| Layer | Test Class | Tests | Scope |
|-------|------------|:-----:|-------|
| Data Access | TaskRepositoryTests | 11 | CRUD, ordering, null description, user filtering |
| Data Access | RefreshTokenRepositoryTests | 5 | Create, get, delete, multiple tokens |
| Data Access | UserRepositoryTests | 6 | Roles join, ordering, nullable fields, empty list |
| | **Subtotal** | **22** | |

| | **Total** | **89** | |

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