# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Favi-BE is a social media backend application built with .NET 9 and ASP.NET Core. The system supports a photo-sharing platform with features like posts, collections, comments, reactions, user profiles, follows, and content moderation. It includes an AI-powered vector search service for semantic image and text retrieval.

## Architecture

### Multi-Service Architecture

The project consists of two main services:

1. **Favi-BE (.NET 9 API)** - Main backend service handling business logic, authentication, and data persistence
2. **Vector-Index-API (FastAPI + PyTorch)** - AI service for semantic search using OpenCLIP embeddings and Qdrant vector database

### Core Architectural Patterns

**Repository Pattern with Unit of Work**
- All data access goes through repositories under `Data/Repositories/`
- `UnitOfWork` aggregates all repositories and manages transactions
- Use `IUnitOfWork` for database operations across multiple entities
- Always call `CompleteAsync()` after repository operations to persist changes

**Service Layer Pattern**
- Business logic resides in services under `Services/`
- Controllers are thin and delegate to services
- Services use `IUnitOfWork` to access repositories

**Privacy Guard System**
- `PrivacyGuard` service enforces access control across the application
- Check privacy before returning any user-generated content (posts, profiles, collections, comments)
- Privacy levels: `Public`, `Followers`, `Private`
- Always inject and use `IPrivacyGuard` when implementing new features that access user content

### Authentication & Authorization

- **Supabase Auth** for user authentication
- JWT bearer tokens with custom claims (`sub` for user ID, `account_role` for role)
- Two policies: `RequireUser` and `RequireAdmin`
- Use `ClaimsPrincipalExtensions.GetUserId()` to extract the current user ID
- Never bypass authentication for user-specific operations

### Database

- **PostgreSQL** with Entity Framework Core 9
- Migrations in `Migrations/` directory
- `AppDbContext` contains all entity configurations
- Automatic migration on startup in `Program.cs` with retry logic
- Connection string configured via environment variables

### Key Entities

- **Profile**: User profiles (linked to Supabase auth via `Id`)
- **Post**: User posts with media, caption, tags, and privacy settings
- **Comment**: Hierarchical comments with parent-child relationships
- **Collection**: User-organized post collections
- **Follow**: Social graph relationships
- **Reaction**: Post reactions (Like, Love, etc.)
- **Tag**: Content categorization
- **Report**: Content moderation reports

### External Services

- **Cloudinary**: Media storage for images/videos
- **Supabase**: Authentication provider
- **Qdrant**: Vector database for semantic search (via Vector-Index-API)

## Development Commands

### Running the Application

**Start all services with Docker Compose:**
```powershell
docker-compose up --build
```

This starts:
- `favi-api` on http://localhost:5000
- `vector-index-api` on http://localhost:18080
- `favi-postgres` on localhost:5432
- `qdrant` on localhost:6333
- `redis` on localhost:6379

**Run .NET API locally (without Docker):**
```powershell
cd Favi-BE\Favi-BE.API
dotnet run
```

### Database Operations

**Create a new migration:**
```powershell
cd Favi-BE\Favi-BE.API
dotnet ef migrations add <MigrationName>
```

**Apply migrations:**
```powershell
dotnet ef database update
```
Note: Migrations run automatically on application startup.

**Remove last migration:**
```powershell
dotnet ef migrations remove
```

### Building

**Build the solution:**
```powershell
cd Favi-BE
dotnet build
```

**Build with Docker:**
```powershell
docker-compose build favi-api
```

### API Documentation

When running in development mode, API documentation is available at:
- **Scalar UI**: http://localhost:5000/scalar/v1

## Vector Index API

The Vector-Index-API service provides semantic search capabilities using OpenCLIP embeddings.

### Key Features
- Multimodal embeddings (image + text)
- Multi-image support per post
- Privacy-aware search (filters by user's follow graph)
- Rate limiting per user

### Common Operations

**Seed data into vector index:**
```powershell
cd Vector-index-api
python seed.py
```

**Test vector search:**
```powershell
python test_multi_images.py
```

## Code Organization

```
Favi-BE/
├── Favi-BE.API/
│   ├── Common/              # Extensions and options classes
│   ├── Controllers/         # API endpoints
│   ├── Data/
│   │   ├── Repositories/    # Data access layer
│   │   ├── AppDbContext.cs  # EF Core context
│   │   └── UnitOfWork.cs    # Transaction management
│   ├── Interfaces/          # Interface definitions
│   │   ├── Repositories/
│   │   └── Services/
│   ├── Models/
│   │   ├── Dtos/           # Data transfer objects
│   │   ├── Entities/       # Database entities
│   │   └── Enums/          # Enumerations
│   ├── Services/           # Business logic
│   ├── Migrations/         # EF Core migrations
│   └── Program.cs          # Application entry point

Vector-index-api/
├── main.py                 # FastAPI application
├── seed.py                 # Database seeding
└── requirements.txt        # Python dependencies
```

## Development Guidelines

### Adding New Features

1. **Define entities** in `Models/Entities/` if needed
2. **Create DTOs** in `Models/Dtos/` for API contracts
3. **Create repository interface** in `Interfaces/Repositories/`
4. **Implement repository** in `Data/Repositories/`
5. **Register repository** in `Program.cs` and add to `UnitOfWork`
6. **Create service interface** in `Interfaces/Services/`
7. **Implement service** in `Services/` using `IUnitOfWork`
8. **Register service** in `Program.cs`
9. **Create controller** in `Controllers/`
10. **Add privacy checks** using `IPrivacyGuard` where applicable
11. **Create migration** if database schema changed

### Privacy Considerations

Always check privacy before returning user content:
```csharp
var post = await _uow.Posts.GetByIdAsync(postId);
if (!await _privacyGuard.CanViewPostAsync(post, userId))
    return Forbid();
```

### Working with Unit of Work

Always save changes:
```csharp
await _uow.Posts.AddAsync(post);
await _uow.CompleteAsync();  // Don't forget this!
```

Use transactions for multi-step operations:
```csharp
await _uow.BeginTransactionAsync();
try 
{
    // Multiple operations
    await _uow.CompleteAsync();
    await _uow.CommitTransactionAsync();
}
catch 
{
    await _uow.RollbackTransactionAsync();
    throw;
}
```

## Configuration

Key configuration sections in `appsettings.json` / environment variables:

- `ConnectionStrings__Default`: PostgreSQL connection string
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`: JWT settings
- `Supabase__Url`, `Supabase__ApiKey`: Supabase configuration
- `CloudinarySettings__*`: Cloudinary credentials
- `VectorIndex__BaseUrl`, `VectorIndex__Enabled`: Vector search service

Docker Compose sets all required environment variables.

## Current Implementation Status

See `FunctionalChecklist.md` for detailed feature tracking. Key implemented areas:
- ✅ User authentication and profiles
- ✅ Post creation, editing, media management
- ✅ Comments and reactions
- ✅ Collections
- ✅ Follow system
- ✅ Privacy controls
- ✅ Basic search
- ✅ Content reporting
- ⏳ AI-powered features (vector search implemented, other AI features planned)
- ⏳ Admin moderation tools (partially implemented)
