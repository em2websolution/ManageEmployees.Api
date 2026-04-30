# Test & Validation Plan — ManageEmployees

This document maps each interview requirement to its implementation and provides a verification checklist.

---

## 1. Requirements Traceability Matrix

### REQ-01: No EF Core / Dapper / Mediator — Direct Database Access

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| No EF Core packages in `.csproj` | PASS | No `Microsoft.EntityFrameworkCore.*` in any project |
| No Dapper packages | PASS | Not present in any project |
| No Mediator packages | PASS | Not present in any project |
| No `DbContext` usage | PASS | No DbContext class exists in the solution |
| No `DbSet<T>` usage | PASS | No references in the codebase |
| No `AddEntityFrameworkStores` | PASS | Identity uses `AddUserStore<>().AddRoleStore<>()` with custom ADO.NET stores |
| No EF Migrations | PASS | Schema managed by `DatabaseInitializer` with idempotent SQL scripts (tables + indexes) |
| Database access via ADO.NET | PASS | `SqlConnection`/`SqlCommand` with parameterized queries in all repositories |
| Proper indexing | PASS | 9 non-clustered indexes designed from actual query patterns (see `001_CreateTables.sql`) |

**Verification command:**
```bash
cd ManageEmployees.Api
grep -r "EntityFramework\|DbContext\|DbSet\|AddEntityFrameworkStores\|Dapper\|MediatR" --include="*.cs" --include="*.csproj"
# Expected: zero results
```

---

### REQ-02: Task CRUD (title, description, status, due_date)

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| TaskItem entity with all fields | PASS | `Domain/Entities/TaskItem.cs` — Id, Title, Description, Status, DueDate, UserId, CreatedAt |
| Create Task endpoint | PASS | `POST /Tasks` → `TasksController.CreateAsync()` |
| Read Tasks (list all) | PASS | `GET /Tasks` → `TasksController.GetAllAsync()` |
| Read Task (by ID) | PASS | `GET /Tasks/{id}` → `TasksController.GetByIdAsync()` |
| Update Task endpoint | PASS | `PUT /Tasks/{id}` → `TasksController.UpdateAsync()` |
| Delete Task endpoint | PASS | `DELETE /Tasks/{id}` → `TasksController.DeleteAsync()` |
| Status validation | PASS | Only Pending/InProgress/Completed accepted |
| Data persistence (SQL Server) | PASS | `TaskRepository` with parameterized ADO.NET queries |
| Frontend Task page | PASS | `/tasks` route with table + drawer |

**Verification steps:**
1. Start backend: `dotnet run --project ManageEmployees.Api`
2. Open Swagger: `https://localhost:64715`
3. Authenticate via `/Login/SignIn` with `admin@company.com` / `Admin123!`
4. Test each Task endpoint
5. Start frontend: `cd ManageEmployees.Front && npm run dev`
6. Navigate to `/tasks` — verify table, create, edit, delete

---

### REQ-03: User Authentication — Authorized & Non-Authorized Endpoints

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| SignIn without auth | PASS | `[AllowAnonymous]` on `LoginController.SignInAsync()` |
| SignUp without auth | PASS | `[AllowAnonymous]` on `LoginController.SignUpAsync()` |
| ListAll requires auth | PASS | `[Authorize]` on controller class |
| Update requires auth | PASS | `[Authorize]` on controller class |
| Delete requires auth | PASS | `[Authorize]` on controller class |
| SignOut requires auth | PASS | `[Authorize]` on controller class |
| All Task endpoints require auth | PASS | `[Authorize]` on `TasksController` class |
| 401 for unauthenticated requests | PASS | JWT Bearer middleware rejects missing/invalid tokens |
| JWT token in response | PASS | `Token` model with AccessToken, RefreshToken, Role, FirstName, UserId |

**Verification steps:**
1. `curl -X GET https://localhost:64715/Tasks` → expect 401
2. `curl -X POST https://localhost:64715/Login/SignIn -d '{"userName":"admin@company.com","password":"Admin123!"}' -H "Content-Type: application/json"` → expect 200 with token
3. `curl -X GET https://localhost:64715/Tasks -H "Authorization: Bearer {token}"` → expect 200

---

### REQ-04: Clean Architecture + TDD

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| Layered project structure | PASS | Api, Domain, Services, Infra.Data, Infra.CrossCutting.IoC |
| Domain has no external dependencies | PASS | Only `Microsoft.Extensions.Identity.Stores` for interfaces |
| Services depend only on Domain | PASS | References only Domain project |
| Infrastructure implements Domain interfaces | PASS | Repositories implement `ITaskRepository`, `IRefreshTokenRepository` etc. |
| DI wiring in IoC layer | PASS | `DependencyInjection.cs` registers all services |
| Unit tests exist | PASS | 62 tests in `ManageEmployees.UnitTests` |
| Integration tests exist | PASS | 22 tests in `ManageEmployees.IntegrationTests` |
| All tests pass | PASS | `dotnet test` — 84/84 green |
| Tests use mocking | PASS | Moq for repositories, UserManager |
| Tests cover business logic | PASS | TaskService, UserService, AuthService tested |
| Tests cover API layer | PASS | TasksControllerTests, LoginControllerTests |
| Tests cover domain layer | PASS | ConstantsTests, SignInRequestTests, ExceptionsTests |
| Tests cover data access layer | PASS | TaskRepositoryTests, RefreshTokenRepositoryTests, UserRepositoryTests |
| Tests cover edge cases | PASS | Invalid status, not found, duplicate email, etc. |

**Verification command:**
```bash
cd ManageEmployees.Api
dotnet test --verbosity normal
# Expected: 84 passed, 0 failed
```

---

### REQ-05: Frontend Integration

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| Login page | PASS | `/login` route with form |
| User management page | PASS | `/employees` with UserListTable + UserDrawer |
| Task management page | PASS | `/tasks` with TaskListTable + TaskDrawer |
| API communication via Axios | PASS | `AxiosHttpClient` singleton with interceptor |
| JWT token handling | PASS | Stored in localStorage, injected in Authorization header |
| Gateway pattern | PASS | `UserGateway`, `TaskGateway` interfaces + implementations |
| Context API state management | PASS | `AuthContext`, `UsersContext`, `TasksContext` |
| Build succeeds | PASS | `npm run build` completes without errors |

**Verification command:**
```bash
cd ManageEmployees.Front
npm run build
# Expected: build succeeds
```

---

### REQ-06: GenAI Documentation

| Criterion | Status | Evidence |
|-----------|:------:|----------|
| AI usage documented | PASS | See `docs/GENAI_USAGE.md` |
| Prompts documented | PASS | Key prompts and interactions recorded |
| Corrections documented | PASS | Bug fixes and AI output corrections listed |

---

## 2. Automated Test Inventory

### Layer Coverage Summary

| Layer | Test Classes | Tests |
|-------|-------------|:-----:|
| **API (Controllers)** | TasksControllerTests, LoginControllerTests | 23 |
| **Services (Business Logic)** | TaskServiceTests, UserServiceTests, AuthServiceTests | 30 |
| **Domain (Entities/DTOs/Models)** | ConstantsTests, SignInRequestTests, ExceptionsTests | 9 |
| **Data Access (Repositories)** | TaskRepositoryTests, RefreshTokenRepositoryTests, UserRepositoryTests | 22 |
| **Total** | **8 unit + 3 integration test classes** | **84** |

---

### TasksControllerTests (11 tests)
| Test | Validates |
|------|---------|
| `GetAllAsync_ShouldReturnOk_WithTaskList` | 200 OK with task list |
| `GetAllAsync_ShouldReturnOk_WithEmptyList_WhenNoTasks` | 200 OK with empty list |
| `GetByIdAsync_ShouldReturnOk_WhenTaskExists` | 200 OK found |
| `GetByIdAsync_ShouldReturnNotFound_WhenTaskDoesNotExist` | 404 Not Found |
| `CreateAsync_ShouldReturnCreated_WhenDataIsValid` | 201 Created |
| `CreateAsync_ShouldSetUserIdFromJwtClaims` | UserId extracted from JWT |
| `CreateAsync_ShouldThrow_WhenServiceThrows` | Exception bubbles to middleware |
| `UpdateAsync_ShouldReturnOk_WhenUpdateSucceeds` | 200 OK |
| `UpdateAsync_ShouldThrow_WhenServiceThrows` | Exception bubbles to middleware |
| `DeleteAsync_ShouldReturnNoContent_WhenDeleteSucceeds` | 204 NoContent |
| `DeleteAsync_ShouldThrow_WhenServiceThrows` | Exception bubbles to middleware |

### LoginControllerTests (16 tests)
| Test | Validates |
|------|----------|
| `SignInAsync_ShouldReturnOk_WhenCredentialsAreValid` | 200 OK with token |
| `SignInAsync_ShouldReturnBadRequest_WhenTokenIsNull` | 400 on null token |
| `SignInAsync_ShouldReturnBadRequest_WhenServiceThrows` | 400 on error |
| `SignUpAsync_ShouldReturnOk_WhenUserCreated` | 200 OK |
| `SignUpAsync_ShouldReturnBadRequest_WhenResultIsEmpty` | 400 on empty result |
| `SignUpAsync_ShouldReturnBadRequest_WhenServiceThrows` | 400 on error |
| `UpdateUserAsync_ShouldReturnOk_WhenUpdateSucceeds` | 200 OK |
| `UpdateUserAsync_ShouldReturnBadRequest_WhenUpdateFails` | 400 on failure |
| `UpdateUserAsync_ShouldReturnBadRequest_WhenServiceThrows` | 400 on error |
| `DeleteUserAsync_ShouldReturnOk_WhenDeleteSucceeds` | 200 OK |
| `DeleteUserAsync_ShouldReturnBadRequest_WhenDeleteFails` | 400 on failure |
| `DeleteUserAsync_ShouldReturnBadRequest_WhenServiceThrows` | 400 on error |
| `SignOutAsync_ShouldReturnOk_WhenSignOutSucceeds` | 200 OK |
| `SignOutAsync_ShouldReturnBadRequest_WhenSignOutFails` | 400 on failure |
| `GetAllUsersAsync_ShouldReturnOk_WithUserList` | 200 OK with users |
| `GetAllUsersAsync_ShouldReturnBadRequest_WhenServiceThrows` | 400 on error |

### TaskServiceTests (10 tests)
| Test | Validates |
|------|-----------|
| `CreateAsync_ShouldCreateTask_WhenDataIsValid` | Happy path creation |
| `CreateAsync_ShouldThrowBusinessException_WhenStatusIsInvalid` | Status validation |
| `GetAllAsync_ShouldReturnAllTasks` | List all tasks |
| `GetByIdAsync_ShouldReturnTask_WhenExists` | Get by ID found |
| `GetByIdAsync_ShouldReturnNull_WhenNotFound` | Get by ID not found |
| `UpdateAsync_ShouldUpdateTask_WhenExists` | Happy path update |
| `UpdateAsync_ShouldThrowBusinessException_WhenTaskNotFound` | Update not found |
| `UpdateAsync_ShouldThrowBusinessException_WhenStatusIsInvalid` | Update status validation |
| `DeleteAsync_ShouldDeleteTask_WhenExists` | Happy path delete |
| `DeleteAsync_ShouldThrowBusinessException_WhenTaskNotFound` | Delete not found |

### UserServiceTests (11 tests)
| Test | Validates |
|------|-----------|
| `SignInAsync_ShouldReturnToken_WhenLoginIsSuccessful` | Login happy path |
| `SignInAsync_ShouldThrowException_WhenUserNotFound` | Invalid credentials |
| `SignUpAsync_ShouldCreateUser_WhenDataIsValid` | Registration happy path |
| `SignUpAsync_ShouldThrowBusinessException_WhenEmailAlreadyExists` | Duplicate email |
| `SignUpAsync_ShouldThrowBusinessException_WhenRoleIsInvalid` | Invalid role |
| `SignUpAsync_ShouldThrowBusinessException_WhenUnhandledExceptionOccurs` | Error handling |
| `UpdateUserAsync_ShouldUpdateUser_WhenDataIsValid` | Update happy path |
| `UpdateUserAsync_ShouldThrowException_WhenUserNotFound` | Update not found |
| `UpdateUserAsync_ShouldThrowException_WhenUpdateFails` | Update failure |
| `DeleteUserAsync_ShouldDeleteUser_WhenUserExists` | Delete happy path |
| `DeleteUserAsync_ShouldThrowException_WhenUserNotFound` | Delete not found |

### AuthServiceTests (5 tests)
| Test | Validates |
|------|-----------|
| `GenerateTokenAsync_ShouldReturnToken_WhenUserExists` | JWT generation |
| `RefreshTokenSwapAsync_ShouldReturnNewToken_WhenRefreshTokenIsValid` | Token refresh |
| `RefreshTokenSwapAsync_ShouldThrowException_WhenRefreshTokenIsInvalid` | Invalid refresh |
| `RefreshTokenSwapAsync_ShouldThrowException_WhenValidationFails` | Validation failure |
| `RemoveRefreshTokenAsync_ShouldReturnFalse_WhenTokenNotFound` | Token removal |

### ConstantsTests (2 tests)
| Test | Validates |
|------|---------|
| `RoleName_ShouldContainExpectedValues` | Roles: Administrator, Employee |
| `TaskItemStatus_ShouldContainExpectedValues` | Statuses: Pending, InProgress, Completed |

### SignInRequestTests (4 tests)
| Test | Validates |
|------|-----------|
| `ShouldBeValid_WhenAllFieldsAreProvided` | Valid DTO |
| `ShouldBeInvalid_WhenUserNameIsMissing` | Required username |
| `ShouldBeInvalid_WhenPasswordIsMissing` | Required password |
| `ShouldBeInvalid_WhenPasswordIsTooShort` | MinLength validation |

### ExceptionsTests (3 tests)
| Test | Validates |
|------|---------|
| `BusinessException_ShouldSetMessageAndInnerException_WhenInitializedWithInnerException` | Inner exception |
| `BusinessException_ShouldSetTraceIdToNull_WhenNoActivityIsPresent` | TraceId null handling |
| `BusinessException_ShouldSetMessageAndErrors_WhenInitializedWithErrors` | Constructor with errors |

---

### 2.4 Integration Tests — Data Access Layer (ManageEmployees.IntegrationTests)

#### TaskRepositoryTests (11 tests)

| Test | Validates |
|------|-----------|
| `CreateAsync_ShouldInsertTask` | INSERT into Tasks table |
| `GetAllAsync_ShouldReturnAllTasks` | SELECT all tasks |
| `GetAllAsync_WhenEmpty_ShouldReturnEmptyList` | Empty result handling |
| `GetByIdAsync_WhenExists_ShouldReturnTask` | SELECT by primary key |
| `GetByIdAsync_WhenNotExists_ShouldReturnNull` | Null on missing record |
| `GetByUserIdAsync_ShouldReturnOnlyUserTasks` | WHERE UserId filter |
| `UpdateAsync_ShouldModifyExistingTask` | UPDATE statement |
| `DeleteAsync_WhenExists_ShouldReturnTrueAndRemove` | DELETE + verify removal |
| `DeleteAsync_WhenNotExists_ShouldReturnFalse` | False on missing record |
| `CreateAsync_WithNullDescription_ShouldPersistNull` | DBNull.Value handling |
| `GetAllAsync_ShouldReturnOrderedByCreatedAtDesc` | ORDER BY CreatedAt DESC |

#### RefreshTokenRepositoryTests (5 tests)

| Test | Validates |
|------|-----------|
| `CreateAsync_ShouldInsertToken` | INSERT into RefreshTokens |
| `GetByUserIdAsync_WhenExists_ShouldReturnToken` | SELECT by UserId |
| `GetByUserIdAsync_WhenNotExists_ShouldReturnNull` | Null on missing record |
| `DeleteAsync_ShouldRemoveToken` | DELETE by Id |
| `CreateAsync_MultipleTimes_ShouldStoreMultipleTokens` | Multiple inserts for same user |

#### UserRepositoryTests (6 tests)

| Test | Validates |
|------|-----------|
| `GetAllWithRolesAsync_WhenEmpty_ShouldReturnEmptyList` | Empty result handling |
| `GetAllWithRolesAsync_ShouldReturnUsersWithRoles` | LEFT JOIN Users-UserRoles-Roles |
| `GetAllWithRolesAsync_UserWithNoRole_ShouldReturnEmptyRole` | ISNULL handling for missing role |
| `GetAllWithRolesAsync_ShouldReturnOrderedByFirstName` | ORDER BY FirstName |
| `GetAllWithRolesAsync_ShouldReturnDocNumberAndPhoneNumber` | All mapped fields |
| `GetAllWithRolesAsync_NullPhoneNumber_ShouldReturnNull` | IsDBNull handling |

---

## 3. Manual Validation Checklist

Run these checks after deploying to confirm full functionality:

| # | Check | How to Verify | Expected |
|---|-------|---------------|----------|
| 1 | Backend compiles | `dotnet build ManageEmployees.sln` | 0 errors |
| 2 | Tests pass | `dotnet test` | 84/84 passed |
| 3 | API starts | `dotnet run --project ManageEmployees.Api` | Swagger opens |
| 4 | DB auto-created | Check SQL Server | ManageEmployees database exists |
| 5 | No EF Core | `grep -r "EntityFramework" --include="*.cs"` | 0 results |
| 6 | Frontend builds | `npm run build` | Build succeeds |
| 7 | Login works | Navigate to `/login`, use admin credentials | Redirects to `/employees` |
| 8 | User list | `/employees` page | Shows admin user in table |
| 9 | Create user | Click add button, fill form | New user appears in table |
| 10 | Edit user | Click edit on a user row | Updates reflected |
| 11 | Delete user | Click delete on a user row | User removed from table |
| 12 | Tasks page | Navigate to `/tasks` | Shows task table |
| 13 | Create task | Click add, fill title/description/status/dueDate | Task appears in table |
| 14 | Edit task | Click edit on task row | Changes saved |
| 15 | Delete task | Click delete on task row | Task removed |
| 16 | 401 without token | `GET /Tasks` without auth header | 401 Unauthorized |
| 17 | SignIn without auth | `POST /Login/SignIn` | 200 OK with token |
| 18 | SignUp without auth | `POST /Users` | 200 OK |
| 19 | Console clean | Check browser dev tools | No critical errors |
