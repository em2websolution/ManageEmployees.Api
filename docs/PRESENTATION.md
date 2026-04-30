# ManageEmployees — Test Interview Presentation

## 1. The Challenge

Build a full-stack employee and task management system under these constraints:

| # | Requirement | Status |
|---|-------------|:------:|
| 1 | **No ORM** — no Entity Framework, Dapper, or Mediator | Done |
| 2 | **Task CRUD** — title, description, status, due date | Done |
| 3 | **Authentication** — authorized and non-authorized endpoints | Done |
| 4 | **Clean Architecture + TDD** | Done |
| 5 | **Frontend integration** with the backend API | Done |
| 6 | **GenAI documentation** showing AI-assisted development | Done |

---

## 2. My Thought Process

### 2.1 Planning Before Coding

Before writing a single line of code, I invested time in **architecture planning**. The "no ORM" constraint was the most impactful — it meant building the entire data layer from scratch, including custom ASP.NET Identity stores with raw ADO.NET.

My approach was **constraint-driven**: I listed what I _couldn't_ use, then designed around those gaps. This naturally led to:

- `IDbConnectionFactory` abstraction for testable connection management
- Custom `UserStore` and `RoleStore` implementing Identity interfaces with parameterized SQL
- A `DatabaseInitializer` that is fully idempotent — no migration tooling needed

### 2.2 Architecture Decision: Clean Architecture with CQRS-Lite

I chose Clean Architecture because it maps directly to the requirement and keeps each layer independently testable:

```
API Layer          → Controllers (thin, no try/catch — delegates to middleware)
Services Layer     → Business logic (TaskService, UserService, AuthService)
Domain Layer       → Entities, DTOs, Interfaces (the core — no external dependencies)
Infrastructure     → ADO.NET repositories, Identity stores, SQL scripts
Cross-Cutting IoC  → DI wiring, Identity configuration
```

Service interfaces follow a **CQRS-Lite split** — `IUserQueryService` (reads) and `IUserCommandService` (writes) — both implemented by the same class. This gives separation of concerns at the contract level without over-engineering.

### 2.3 Key Design Decisions

| Decision | Reasoning |
|----------|-----------|
| **Raw ADO.NET** with parameterized queries | Requirement says no ORM — parameterized commands prevent SQL injection |
| **JWT Bearer + Refresh Tokens** | Stateless auth, standard for SPAs |
| **GlobalExceptionHandlerMiddleware** | Controllers stay thin — all errors handled centrally with proper HTTP status codes |
| **Custom Identity stores** | ASP.NET Identity provides user/role management; custom stores let me use ADO.NET underneath |
| **Gateway Pattern (frontend)** | Clean separation between API calls and UI logic |
| **Context API** (not Redux) | Sufficient for this app's state complexity |
| **Server-side pagination** | `OFFSET/FETCH NEXT` in SQL → `PagedResult<T>` — scalable for large datasets |

### 2.4 The TDD Loop

For each feature, I followed this cycle:

1. **Define the interface** (Domain layer)
2. **Write the service** (Services layer)
3. **Write the tests** — unit tests with Moq for service isolation, integration tests with a real SQL Server database
4. **Build & verify** — green build before moving forward
5. **Create the controller** — thin, only routing and HTTP semantics

---

## 3. What Was Built

### Backend (.NET 8 / C# 12)

- **3 Controllers**: `LoginController` (auth), `UsersController` (user CRUD), `TasksController` (task CRUD)
- **6 Service interfaces** with CQRS-Lite split (Query/Command)
- **3 Repositories** with raw ADO.NET (no ORM)
- **Custom Identity stores** — `UserStore` (4 interfaces), `RoleStore`
- **Database schema** — 5 tables, 9 non-clustered indexes (designed from actual query patterns), named constraints, CHECK constraints
- **JWT authentication** with refresh token rotation

### Frontend (Next.js 15 / TypeScript / MUI v6)

- **Login page** with JWT-based auth flow
- **Employees page** — data table with search, role filter, pagination, CRUD drawer
- **Tasks page** — data table with search, status filter, date range filter, pagination, CRUD drawer
- **Gateway pattern** — typed API interfaces with Axios singleton
- **Context API** — `AuthContext`, `UsersContext`, `TasksContext`

### Infrastructure

- **Docker Compose** — orchestrates SQL Server + API + Frontend (one command: `docker compose up --build`)
- **SonarQube** — separate compose file for code quality analysis
- **Multi-stage Dockerfile** — optimized .NET build (SDK for build, ASP.NET runtime for final image)

---

## 4. API Design

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|:----:|---------|
| `POST` | `/Login/SignIn` | No | Authenticate and receive JWT |
| `POST` | `/Login/SignOut` | Yes | Invalidate session |
| `POST` | `/Users` | No | Register new user |
| `GET` | `/Users` | Yes | List users (paginated, filterable) |
| `PUT` | `/Users/{userId}` | Yes | Update user |
| `DELETE` | `/Users/{userId}` | Yes | Delete user |
| `GET` | `/Tasks` | Yes | List tasks (paginated, filterable) |
| `GET` | `/Tasks/{id}` | Yes | Get task by ID |
| `POST` | `/Tasks` | Yes | Create task (userId from JWT) |
| `PUT` | `/Tasks/{id}` | Yes | Update task |
| `DELETE` | `/Tasks/{id}` | Yes | Delete task |

### Controller Design Philosophy

Controllers are **thin** — they handle HTTP semantics only:
- Extract data from requests (body, route, query, JWT claims)
- Delegate to services
- Return appropriate HTTP status codes (`201 Created`, `204 NoContent`, `404 NotFound`)
- **No try/catch** — all exceptions bubble to `GlobalExceptionHandlerMiddleware`

---

## 5. Database Design

### Schema

```sql
Users           → ASP.NET Identity users + custom fields (FirstName, LastName, DocNumber)
Roles           → Administrator, Employee
UserRoles       → Many-to-many mapping
RefreshTokens   → One active token per user (UNIQUE constraint)
Tasks           → Title, Description, Status, DueDate, UserId, CreatedAt
```

### Indexing Strategy

Every index was designed from actual query patterns in the code — no speculative indexes:

| Index | Query Pattern |
|-------|--------------|
| `IX_Users_NormalizedUserName` (UNIQUE) | `FindByNameAsync` — every login |
| `IX_Users_FirstName` (COVERING) | `GetAllWithRolesAsync` ORDER BY |
| `IX_RefreshTokens_UserId` (UNIQUE) | One token per user enforcement |
| `IX_Tasks_CreatedAt` (COVERING) | Default sort: `CreatedAt DESC` |
| `IX_Tasks_UserId_CreatedAt` (COMPOSITE) | Filter by user + sort |

### DBA Practices Applied

- `NEWSEQUENTIALID()` for GUID PKs — prevents clustered index page splits
- `DATETIME2(7)` explicit precision
- Named constraints (`PK_`, `FK_`, `CK_`)
- CHECK constraint on `Tasks.Status`
- Idempotent creation (`IF NOT EXISTS` guards)

---

## 6. Testing

### Coverage: 186 Tests — All Passing

| Suite | Tests | Scope |
|-------|:-----:|-------|
| **Unit Tests** | 111 | Controllers, Services, Domain, DTOs, Exceptions |
| **Integration Tests** | 75 | Repositories with real SQL Server database |
| **Total** | **186** | |

### Test Architecture

- **Unit tests**: NUnit + Moq + FluentAssertions — services mocked, controllers tested for HTTP behavior
- **Integration tests**: Real database via `DatabaseFixture` — automatic setup/teardown per test run
- **Coverage**: Generated with Coverlet (OpenCover format), consumed by SonarQube

### What Was Tested

| Layer | What's Verified |
|-------|----------------|
| Controllers | HTTP status codes, JWT claim extraction, error delegation to middleware |
| Services | Business rules, validation, error handling, Identity interactions |
| Repositories | CRUD operations, ordering, null handling, FK filtering, pagination |
| Domain | Entity defaults, DTO validation, exception constructors, constants |

---

## 7. Code Quality — SonarQube Results

Analyzed with **SonarQube 9.9 LTS Community Edition**:

### Backend — `manage-employees-api`

| Metric | Result |
|--------|--------|
| **Coverage** | **81.9%** |
| **Bugs** | 0 |
| **Vulnerabilities** | 0 |
| **Code Smells** | 0 |
| **Duplication** | 0.0% |
| **Security Hotspots Reviewed** | 100% |
| **Maintainability Rating** | A |
| **Reliability Rating** | A |
| **Security Rating** | A |

### Frontend — `manage-employees-front`

| Metric | Result |
|--------|--------|
| **Bugs** | 0 |
| **Vulnerabilities** | 0 |
| **Code Smells** | 0 |
| **Duplication** | 0.0% |
| **Security Hotspots** | 0 |

> The non-covered 18.1% is `Program.cs` (startup bootstrap) and `DatabaseInitializer` (runs only at app startup) — infrastructure code that runs outside the testable application lifecycle. All testable production code is at **98%+ coverage**.

---

## 8. GenAI Usage

GitHub Copilot (Agent mode + Plan mode, Claude model) was used **specifically for the Tasks feature** — the area requested by the interview exercise. All other parts of the application (user management, authentication, data layer, Identity stores, frontend employees page) were implemented manually without AI.

### What Was Built Without AI

- Full data layer (ADO.NET, custom Identity stores, SQL schema, repositories)
- User/Auth services and controllers
- Frontend login and employees pages
- Docker infrastructure

### What Was Built With AI (Tasks Feature)

The entire Tasks feature end-to-end: `TaskItem` entity, DTOs, service interfaces (CQRS-Lite), `TaskService`, `TaskRepository` (ADO.NET), `TasksController`, frontend Tasks page (Gateway + Context + TanStack Table + Drawer), and all related tests.

### Other AI-Assisted Activities

| Activity | How AI Was Used |
|----------|----------------|
| **`.github/` setup** | Prepared `copilot-instructions.md` with project conventions for context |
| **Plan mode validation** | Reviewed feature implementations before execution |
| **Code coverage** | Generated additional test cases for uncovered paths |
| **SonarQube fixes** | Analyzed reports and fixed 5 code smells (S3604, S3928) → 0 |
| **Documentation** | Generated README, implementation report, test plan, AI usage report |

### AI Corrections Required

| Issue | What Happened | Fix |
|-------|--------------|-----|
| `CreatedAtAction` routing | AI used wrong routing helper | Changed to `Created($"/Tasks/{id}", task)` |
| JWT claim mismatch | Different claim types between AuthService and Controller | Aligned on `ClaimTypes.UserData` |
| Frontend table crash | Context returned `undefined` instead of `[]` | Added `tableData ?? []` guard |
| Hydration mismatch | MUI components rendered during SSR outside cache provider | Added `mounted` guard with `useEffect` |

Full details in [GENAI_USAGE.md](GENAI_USAGE.md).

---

## 9. How to Run

### Docker (recommended — one command)

```bash
docker compose up --build

# Frontend:  http://localhost:3000
# Swagger:   http://localhost:64715
# SQL Server: localhost,1433
```

### Local Development

```bash
# Backend
cd ManageEmployees.Api
dotnet build
dotnet run --project ManageEmployees.Api

# Frontend
cd ManageEmployees.Front
npm install
npm run dev

# Tests
cd ManageEmployees.Api
dotnet test    # 186 tests
```

### Default Credentials

| Email | Password | Role |
|-------|----------|------|
| `admin@company.com` | `Admin123!` | Administrator |

---

## 10. Project Documentation
The files are in the “docs” folder

| Document | Description |
|----------|-------------|
| [PRESENTATION.md](PRESENTATION.md) | This file — thought process and exercise summary |
| [GENAI_USAGE.md](GENAI_USAGE.md) | AI usage methodology, contributions, and corrections |
| [TEST_VALIDATION_PLAN.md](TEST_VALIDATION_PLAN.md) | Requirement traceability matrix and test inventory |
