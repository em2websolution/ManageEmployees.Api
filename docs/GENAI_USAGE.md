# GenAI Usage Report — ManageEmployees

This document describes how Generative AI (GitHub Copilot) was used during the development of the ManageEmployees system. **The AI was used specifically for the Tasks feature** — the area requested by the interview exercise. All other parts of the application (user management, authentication, data layer, Identity stores, frontend employees page) were implemented manually without AI assistance.

---

## 1. Scope of AI Usage

### What Was Built Without AI

The following components were implemented entirely by hand, prior to engaging AI:

- **Data Layer** — `IDbConnectionFactory`, `SqlConnectionFactory`, `UserStore` (4 Identity interfaces), `RoleStore`, `DatabaseInitializer`, SQL schema (5 tables, 9 indexes), `RefreshTokenRepository`, `UserRepository`
- **Domain Layer** — `User` entity, DTOs (`CreateUser`, `UpdateUser`, `SignInRequest`), interfaces (`IUserService`, `IAuthService`), models (`Token`, `Error`, `PagedResult<T>`), constants, exceptions
- **Services Layer** — `UserService`, `AuthService`, `EncryptionService`
- **API Layer** — `LoginController`, `UsersController`, `Program.cs`, `GlobalExceptionHandlerMiddleware`, DI configuration, Identity configuration
- **Frontend** — Login page, Employees page, `AuthContext`, `UsersContext`, Gateway pattern for Users, Axios singleton, JWT interceptor, layout system
- **Infrastructure** — Docker Compose, Dockerfiles, SQL Server setup
- **Unit & Integration Tests** — User/Auth/Login tests (written by hand)

### What Was Built With AI (Tasks Feature)

AI (GitHub Copilot — Agent mode, Claude model) was used for the **Tasks feature** end-to-end:

| Component | What AI Generated |
|-----------|-------------------|
| `TaskItem` entity | Entity with proper defaults (`Guid.NewGuid`, `DateTime.UtcNow`) |
| `CreateTaskRequest` / `UpdateTaskRequest` | DTOs with DataAnnotations validation |
| `ITaskService`, `ITaskQueryService`, `ITaskCommandService` | Service interfaces (CQRS-Lite split) |
| `ITaskRepository` | Repository interface |
| `TaskService` | Business logic — status validation, CRUD operations |
| `TaskRepository` | ADO.NET repository with parameterized SQL queries |
| `TasksController` | Thin controller — JWT claim extraction, proper HTTP status codes |
| `TaskServiceTests` | Unit tests for business logic |
| `TasksControllerTests` | Unit tests for HTTP behavior |
| `TaskRepositoryTests` | Integration tests with real SQL Server |
| `TaskGateway` (frontend) | Gateway interface + Axios implementation |
| `TasksContext` | React Context with full CRUD state management |
| Tasks page | TanStack React Table + `TaskDrawer` with React Hook Form |
| `getApiErrorMessage` | Unified error handler for API responses |

### Other AI-Assisted Activities

Beyond the Tasks feature, AI was also used for:

| Activity | How AI Was Used |
|----------|----------------|
| **`.github/` setup** | Prepared workspace-level `copilot-instructions.md` with architecture conventions, naming patterns, and coding standards to provide context for AI interactions |
| **Plan mode validation** | Used Copilot's Plan mode to validate feature implementations — reviewing architecture alignment and identifying potential issues before execution |
| **Code coverage improvement** | AI helped increase test coverage by generating additional test cases for edge cases and uncovered paths |
| **SonarQube analysis & fixes** | AI analyzed SonarQube reports, identified code smells (S3604, S3928), and applied fixes — reducing code smells from 5 to 0 |
| **Documentation** | Generated README files, implementation report, test validation plan, and this GenAI usage report |

---

## 2. AI Tool & Configuration

- **Tool**: GitHub Copilot (Agent mode + Plan mode in VS Code)
- **Model**: Claude
- **Context engineering**: Workspace-level `.github/copilot-instructions.md` file providing architecture conventions, naming patterns, project structure, and coding standards — ensuring every AI interaction inherits the project context
- **Custom instructions**: Language-specific `.github/instructions/csharp.instructions.md` and `nextjs.instructions.md` files applied automatically via `applyTo` patterns

### Modes Used

| Mode | Purpose |
|------|---------|
| **Agent mode** | Multi-file code generation for the Tasks feature, test generation, SonarQube fixes |
| **Plan mode** | Feature validation — reviewing proposed changes before execution, ensuring alignment with existing architecture |

---

## 3. Tasks Feature — AI Development Process

### 3.1 Backend Implementation

**Prompt approach**: "Implement the Tasks feature following the same patterns already established for Users — Clean Architecture, ADO.NET, parameterized queries, CQRS-Lite interfaces."

**AI contribution**:
- Created `TaskItem` entity following the same conventions as `User`
- Designed DTOs with DataAnnotations validation
- Implemented `TaskRepository` with raw parameterized ADO.NET (matching the pattern of existing repositories)
- Implemented `TaskService` with status validation (Pending/InProgress/Completed)
- Created `TasksController` with `[Authorize]` at class level
- Registered DI dependencies following the existing IoC pattern

**Corrections required** (AI output needed human intervention):
- `CreatedAtAction("GetByIdAsync", ...)` caused routing errors → Corrected to `Created($"/Tasks/{task.Id}", task)`
- UserId extraction from JWT required aligning the claim type (`ClaimTypes.UserData`) between `AuthService` and `TasksController`

### 3.2 Frontend Implementation

**Prompt approach**: "Implement the Tasks frontend page following the Gateway pattern already established for Users."

**AI contribution**:
- Designed Gateway pattern for Tasks (interface + Axios implementation + singleton)
- Created `TasksContext` with full CRUD state management
- Built task management page with TanStack React Table
- Created `TaskDrawer` for create/edit forms with React Hook Form

**Bugs found and fixed iteratively**:

| Bug | Root Cause | Fix |
|-----|-----------|-----|
| API calls firing on login page | Context `useEffect` not guarded by auth state | Added token check |
| Table crash on undefined data | TanStack table receiving `undefined` | Used `tableData ?? []` |
| Task creation missing userId | Frontend was sending userId | Controller extracts from JWT |
| Error messages not displaying | Backend returns errors in different shapes | Created unified error handler |

### 3.3 Tests

**AI contribution**:
- Generated unit tests for `TaskService` (business logic) and `TasksController` (HTTP behavior)
- Generated integration tests for `TaskRepository` (real SQL Server database)
- Covered happy paths, error paths, and edge cases (invalid status, task not found, null description)

---

## 4. Code Coverage & SonarQube — AI-Assisted

### Coverage Improvement

AI helped increase test coverage by:
- Identifying uncovered code paths via coverage reports
- Generating additional test cases targeting specific branches
- Final result: **186 tests** (111 unit + 75 integration), **81.9% coverage**

### SonarQube Report Analysis & Fixes

AI analyzed SonarQube scan results and applied targeted fixes:

| Rule | File | Issue | Fix |
|------|------|-------|-----|
| S3604 | `LoginController.cs` | Redundant field initializer (primary constructor) | Removed field, used parameter directly |
| S3604 | `TasksController.cs` (×2) | Same — two redundant fields | Removed fields, used parameters directly |
| S3604 | `TaskRepository.cs` | Redundant field initializer | Removed field, used parameter directly |
| S3928 | `TaskRepository.cs` | `ArgumentNullException` parameter name mismatch | Removed unnecessary null guard |

**Result**: Code smells reduced from **5 to 0** — clean SonarQube dashboard.

---

## 5. Quality Assurance Process

Every AI-generated output went through verification:

```
┌─────────────────┐
│  AI generates    │
│  code/files      │
├────────┬────────┤
│        ▼        │
│  dotnet build   │──── Fix compilation errors
│  npm run build  │
├────────┬────────┤
│        ▼        │
│  dotnet test    │──── Fix failing tests
│  (186 tests)    │
├────────┬────────┤
│        ▼        │
│  Manual testing │──── Fix UI/UX bugs
│  in browser     │
├────────┬────────┤
│        ▼        │
│  SonarQube scan │──── Fix code smells
│  (0 issues)     │
├────────┬────────┤
│        ▼        │
│  Code review    │──── Verify security, patterns
│  by human       │
└─────────────────┘
```

---

## 6. Key Lessons Learned

1. **Context engineering matters**: The `.github/copilot-instructions.md` file ensured every AI interaction understood existing project conventions — the Tasks feature code is indistinguishable from the hand-written User code
2. **Plan mode catches issues early**: Using Copilot Plan mode to review before executing prevented architectural misalignment
3. **AI works best with established patterns**: By building Users first (manually), the AI had a clear reference for how Tasks should be structured — same patterns, same conventions
4. **SonarQube + AI is a strong feedback loop**: AI can read and fix static analysis reports faster than manual review, but human validation of each fix is still essential
5. **Frontend state management needs human testing**: React lifecycle issues (useEffect timing, state initialization) were not caught by AI alone — browser testing was required
6. **AI accelerates feature development, humans own architecture**: The architectural decisions were made manually; AI accelerated the implementation of a second feature following the same blueprint