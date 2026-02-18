using Forssa.Application.Interfaces;
using Forssa.Application.Mappings;
using Forssa.Domain.Entities;
using Forssa.Infrastructure.Data;
using Forssa.Infrastructure.Repositories;
using Forssa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forssa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Register AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Register repositories
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

        // Register services - Add your custom services here
        services.AddScoped<ProductService>();
        
        return services;
    }
}

