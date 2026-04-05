# Job Application Tracker API

> A production-grade REST API for tracking job applications, documents, and interview notes — built with Clean Architecture, CQRS, and Azure cloud services.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?logo=entity-framework)](https://learn.microsoft.com/en-us/ef/core/)
[![MediatR](https://img.shields.io/badge/MediatR-12.2-512BD4)](https://github.com/jbogard/MediatR)
[![Auth](https://img.shields.io/badge/Auth-JWT%20Bearer-000000?logo=json-web-tokens)](https://jwt.io/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2B%20CQRS-2EA44F)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![License](https://img.shields.io/badge/License-MIT-blue)](LICENSE)

---

## Project Overview

This is a backend API I built to solve a real problem: job seekers need a structured way to track their applications, store CVs and cover letters, and keep notes from interviews — all in one place. Rather than relying on spreadsheets or scattered browser bookmarks, this API provides a single source of truth with proper authentication, file storage, and audit trails.

It's designed as a portfolio project that demonstrates production-level .NET 8 development. Every architectural decision was intentional — from the Clean Architecture layering to the CQRS pattern with MediatR — because I wanted this codebase to reflect how I'd build software for a real team.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                            │
│  Controllers │ Middleware │ Swagger │ Health Checks          │
├─────────────────────────────────────────────────────────────┤
│                     Application Layer                       │
│  Commands │ Queries │ Handlers │ Validators │ DTOs           │
│  ───────────────── CQRS via MediatR ──────────────────      │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                     │
│  EF Core │ Repositories │ JWT │ Blob Storage │ Service Bus  │
├─────────────────────────────────────────────────────────────┤
│                       Domain Layer                          │
│  Entities │ Value Objects │ Enums │ Domain Interfaces        │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rule:** Each layer only depends on the layer directly inside it. The Domain layer has zero external dependencies.

---

## Key Technical Highlights

| Decision | Why |
|----------|-----|
| **Clean Architecture** | Keeps business logic isolated from frameworks, making the codebase testable and adaptable to change without rewriting core logic. |
| **CQRS with MediatR** | Separates read and write concerns so each handler has a single responsibility, and adding new features means adding one file — not modifying existing ones. |
| **Repository + Unit of Work** | Abstracts data access behind interfaces so the application layer never talks to EF Core directly, enabling easy mocking in unit tests. |
| **JWT Bearer Authentication** | Stateless auth that scales horizontally — no session storage needed, and refresh tokens provide secure token rotation. |
| **FluentValidation Pipeline** | Validation runs automatically through MediatR's pipeline behavior, so handlers never need to check input validity — they can assume clean data. |
| **Global Exception Middleware** | Catches all unhandled exceptions in one place and returns consistent RFC 7807 ProblemDetails responses, so clients always know what went wrong. |
| **Soft Deletes with Query Filters** | Entities have an `IsDeleted` flag with EF Core global query filters, so data is never truly lost and can be recovered if needed. |
| **Value Objects (Email)** | Encapsulates email validation at the domain level, so invalid emails can't exist in the system — not even temporarily. |

---

## Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Framework** | .NET 8.0 (ASP.NET Core) | Runtime and web framework |
| **Database** | SQL Server + EF Core 8.0 | Data persistence and ORM |
| **CQRS** | MediatR 12.2 | Command/Query pattern with pipeline behaviors |
| **Validation** | FluentValidation 11.9 | Declarative input validation |
| **Auth** | JWT Bearer + BCrypt | Token-based authentication with secure password hashing |
| **Storage** | Azure Blob Storage | Document and file uploads |
| **Messaging** | Azure Service Bus | Event-driven notifications |
| **Caching** | Redis (StackExchange.Redis) | Query result caching for performance |
| **Logging** | Serilog | Structured logging with console and file sinks |
| **Testing** | xUnit + Moq + FluentAssertions | Unit and integration testing |
| **CI/CD** | GitHub Actions + Docker | Automated build, test, and deployment |
| **API Docs** | Swagger/OpenAPI | Interactive API documentation |

---

## Project Structure

```
JobApplicationTrackerApi/
├── src/
│   ├── JobApplicationTrackerAPI.Api/          # Entry point, controllers, middleware
│   ├── JobApplicationTrackerAPI.Application/  # Commands, queries, handlers, validators
│   ├── JobApplicationTrackerAPI.Domain/       # Entities, value objects, enums
│   └── JobApplicationTrackerAPI.Infrastructure/ # EF Core, repositories, external services
├── tests/
│   ├── JobApplicationTrackerAPI.UnitTests/    # Handler and validator tests
│   └── JobApplicationTrackerAPI.IntegrationTests/ # Endpoint integration tests
├── Dockerfile
├── docker-compose.yml
└── .github/workflows/ci-cd.yml
```

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker & Docker Compose](https://www.docker.com/) (optional, for containerised setup)
- SQL Server (LocalDB or Docker)
- Redis (optional, for caching)

### Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/MuhammadNomanDev/JobApplicationTrackerApi.git
cd JobApplicationTrackerApi

# 2. Configure your secrets (never commit secrets)
dotnet user-secrets init --project src/JobApplicationTrackerAPI.Api
dotnet user-secrets set "Jwt:Key" "your-secure-key-here" --project src/JobApplicationTrackerAPI.Api

# 3. Run with Docker (includes SQL Server + Redis)
docker-compose up -d

# Or run locally (requires SQL Server running)
dotnet run --project src/JobApplicationTrackerAPI.Api

# 4. Apply database migrations
dotnet ef database update --project src/JobApplicationTrackerAPI.Infrastructure

# 5. Open Swagger UI
# http://localhost:5000/swagger
```

### Configuration

Copy `appsettings.json` and override values in `appsettings.Development.json` or use `dotnet user-secrets` for sensitive values. Never commit real connection strings or keys.

---

## API Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/auth/register` | Register a new user account | No |
| `POST` | `/api/auth/login` | Authenticate and receive JWT tokens | No |
| `POST` | `/api/auth/refresh` | Refresh expired JWT tokens | No |
| `POST` | `/api/jobapplications` | Create a new job application | Yes |
| `GET` | `/api/jobapplications` | List all job applications (paginated) | Yes |
| `GET` | `/api/jobapplications/{id}` | Get a specific job application | Yes |
| `PUT` | `/api/jobapplications/{id}` | Update a job application | Yes |
| `PATCH` | `/api/jobapplications/{id}/status` | Update application status | Yes |
| `DELETE` | `/api/jobapplications/{id}` | Delete a job application | Yes |
| `POST` | `/api/notes` | Add a note to a job application | Yes |
| `GET` | `/api/notes/getnotesbyjobapplication/{id}` | Get all notes for a job application | Yes |
| `PUT` | `/api/notes/{id}` | Update a note | Yes |
| `DELETE` | `/api/notes/{id}` | Delete a note | Yes |
| `POST` | `/api/documents` | Upload a document (CV, cover letter) | Yes |
| `GET` | `/api/documents/{id}` | Get document with SAS URL | Yes |
| `GET` | `/api/documents/getdocumentsbyjobapplication/{id}` | List all documents for a job application | Yes |
| `DELETE` | `/api/documents/{id}` | Delete a document | Yes |
| `GET` | `/health` | Health check (SQL Server status) | No |

---

## Design Decisions & Patterns

I chose Clean Architecture because I've seen too many projects where business logic gets tangled into controllers, and changing a database query means touching three different layers. Here, the Domain layer is the centre of gravity — it defines the rules, and everything else serves those rules. The Application layer orchestrates use cases through MediatR commands and queries, which means each feature is a self-contained vertical slice. I went with CQRS because reads and writes have fundamentally different concerns: a query shouldn't carry the risk of side effects, and a command shouldn't need to worry about projection optimisations. The Repository pattern sits behind interfaces so that every handler can be unit-tested without a database — I mock `IAppDbContext` and verify behaviour, not implementation. JWT auth with refresh tokens was chosen over session-based auth because it's stateless, scales horizontally, and is the industry standard for SPAs and mobile apps. Every decision here was made with one question in mind: "Will this make the codebase easier to maintain six months from now?"

---

## Author

**Muhammad Noman**  
🔗 [LinkedIn](https://linkedin.com/in/muhammad-noman-823225266)  
🐙 [GitHub](https://github.com/MuhammadNomanDev)  
📧 muhd.noman.dev@gmail.com

---

*Built with .NET 8.0 — because good architecture is a habit, not an accident.*
