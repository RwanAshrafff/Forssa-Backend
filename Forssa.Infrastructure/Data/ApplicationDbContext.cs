using Forssa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace Forssa.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add your DbSets here
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Lesson> Lessons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Course -> Modules
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("courses");
            entity.HasMany(c => c.Modules)
                  .WithOne(m => m.Course)
                  .HasForeignKey(m => m.Course_id)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Module -> Lessons
        modelBuilder.Entity<Module>(entity =>
        {
            entity.ToTable("modules");
            entity.HasMany(m => m.Lessons)
                  .WithOne(l => l.Module)
                  .HasForeignKey(l => l.Module_id)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Lesson
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
        });

        // Product
        modelBuilder.Entity<Product>().ToTable("Products");

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.SupabaseId).IsUnique();
        });

        // Configure global defaults and query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var entity = modelBuilder.Entity(entityType.ClrType);

                entity.HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));

                // Add defaults for Id, CreatedAt, and IsDeleted
                entity.Property(nameof(BaseEntity.Id))
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("now()");

                entity.Property(nameof(BaseEntity.IsDeleted))
                    .HasDefaultValue(false);
            }
        }

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Add Unique Index for SupabaseId
        modelBuilder.Entity<User>()
            .HasIndex(u => u.SupabaseId)
            .IsUnique();
    }

    private static LambdaExpression GetSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
