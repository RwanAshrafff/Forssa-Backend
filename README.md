# Forssa Backend - Base Repository Pattern

A clean architecture .NET backend project with a complete base repository pattern implementation.

## Project Structure

```
Forssa-Backend/
├── Forssa.API/                 # Presentation Layer
│   ├── Controllers/
│   │   ├── BaseController.cs       # Base controller with common API responses
│   │   └── ProductsController.cs   # Example controller
│   ├── Program.cs
│   └── appsettings.json
│
├── Forssa.Application/         # Application Layer
│   ├── DTOs/
│   │   ├── BaseDTO.cs             # Base DTO with common properties
│   │   └── ProductDTO.cs          # Example DTO
│   ├── Interfaces/
│   │   ├── IBaseRepository.cs     # Base repository interface
│   │   └── IBaseService.cs        # Base service interface
│   ├── Mappings/
│   │   └── MappingProfile.cs      # AutoMapper configurations
│   └── Services/
│
├── Forssa.Domain/              # Domain Layer
│   └── Entities/
│       ├── BaseEntity.cs          # Base entity with common properties
│       └── Product.cs             # Example entity
│
└── Forssa.Infrastructure/      # Infrastructure Layer
    ├── Data/
    │   └── ApplicationDbContext.cs # EF Core DbContext
    ├── Repositories/
    │   └── BaseRepositories.cs    # Base repository implementation
    ├── Services/
    │   ├── BaseServices.cs        # Base service implementation
    │   └── ProductService.cs      # Example service
    └── DependencyInjection.cs     # DI configuration
```

## Features

### Base Entity (Domain Layer)
- `Id` (Guid) - Unique identifier
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime?) - Last update timestamp
- `IsDeleted` (bool) - Soft delete flag
- `DeletedAt` (DateTime?)  - Soft delete timestamp

### Base Repository (Infrastructure Layer)
- `GetByIdAsync` - Get entity by ID
- `GetAllAsync` - Get all entities
- `FindAsync` - Find entities by predicate
- `FirstOrDefaultAsync` - Get first entity matching predicate
- `AddAsync` - Create new entity
- `AddRangeAsync` - Create multiple entities
- `UpdateAsync` - Update existing entity
- `DeleteAsync` - Hard delete (permanent)
- `SoftDeleteAsync` - Soft delete (mark as deleted)
- `CountAsync` - Count entities
- `ExistsAsync` - Check if entity exists

### Base Service (Infrastructure Layer)
- Maps between DTOs and Entities using AutoMapper
- Implements business logic layer
- Provides CRUD operations with automatic mapping

### Base Controller (API Layer)
- `HandleResult` - Standard success response
- `Created` - 201 Created response
- `BadRequestResult` - 400 Bad Request
- `NotFoundResult` - 404 Not Found
- `UnauthorizedResult` - 401 Unauthorized
- `ForbiddenResult` - 403 Forbidden
- `InternalServerError` - 500 Internal Server Error

## How to Use

### 1. Create a New Entity

```csharp
public class YourEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    // Add your properties here
}
```

### 2. Create a DTO

```csharp
public class YourEntityDTO : BaseDTO
{
    public string Name { get; set; } = string.Empty;
    // Add your properties here
}
```

### 3. Add AutoMapper Configuration

```csharp
// In Forssa.Application/Mappings/MappingProfile.cs
CreateMap<YourEntity, YourEntityDTO>().ReverseMap();
```

### 4. Create a Service (Optional)

```csharp
public class YourEntityService : BaseService<YourEntity, YourEntityDTO>
{
    public YourEntityService(IBaseRepository<YourEntity> repository, IMapper mapper)
        : base(repository, mapper)
    {
    }

    // Add custom business logic here
}
```

### 5. Register Service in DependencyInjection.cs

```csharp
services.AddScoped<YourEntityService>();
```

### 6. Create a Controller

```csharp
[Route("api/[controller]")]
[ApiController]
public class YourEntitiesController : BaseController
{
    private readonly IBaseService<YourEntity, YourEntityDTO> _service;

    public YourEntitiesController(IBaseService<YourEntity, YourEntityDTO> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return HandleResult(items);
    }

    // Add more endpoints...
}
```

### 7. Add DbSet to ApplicationDbContext

```csharp
public DbSet<YourEntity> YourEntities { get; set; }
```

## Setup Instructions

### 1. Update Connection String

Edit `appsettings.json` in Forssa.API:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your_server;Database=ForssaDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 2. Install Required NuGet Packages

```bash
# In Forssa.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# In Forssa.API
dotnet add package Swashbuckle.AspNetCore
```

### 3. Create Database Migration

```bash
dotnet ef migrations add InitialCreate --project Forssa.Infrastructure --startup-project Forssa.API
dotnet ef database update --project Forssa.Infrastructure --startup-project Forssa.API
```

### 4. Run the Application

```bash
cd Forssa.API
dotnet run
```

### 5. Access Swagger UI

Navigate to: `https://localhost:5001/swagger`

## Built-in Features

- ✅ Soft Delete - Entities are marked as deleted instead of being removed
- ✅ Automatic Timestamps - CreatedAt and UpdatedAt are managed automatically
- ✅ Global Query Filters - Soft-deleted entities are automatically excluded
- ✅ Generic Repository Pattern - Reusable CRUD operations
- ✅ Clean Architecture - Separation of concerns
- ✅ AutoMapper Integration - Automatic DTO mapping
- ✅ Swagger Documentation - API documentation
- ✅ CORS Support - Cross-origin requests enabled

## Example Usage

The project includes a complete `Product` example:
- [Product.cs](Forssa.Domain/Entities/Product.cs) - Entity
- [ProductDTO.cs](Forssa.Application/DTOs/ProductDTO.cs) - DTO
- [ProductService.cs](Forssa.Infrastructure/Services/ProductService.cs) - Service
- [ProductsController.cs](Forssa.API/Controllers/ProductsController.cs) - Controller

## Technologies Used

- .NET 8.0 (or your version)
- Entity Framework Core
- AutoMapper
- SQL Server
- Swagger/OpenAPI
- Clean Architecture Pattern

## License

MIT License