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
    public DbSet<LessonProgress> LessonProgress { get; set; }
    public DbSet<LessonNote> LessonNotes { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<QuizSubmission> QuizSubmissions { get; set; }
    public DbSet<UserFollow> UserFollows { get; set; }

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

            entity.HasMany(m => m.Quizzes)
                  .WithOne(q => q.Module)
                  .HasForeignKey(q => q.ModuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Lesson
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.ToTable("lessons");
        });

        // LessonProgress
        modelBuilder.Entity<LessonProgress>(entity =>
        {
            entity.ToTable("lesson_progress");
            entity.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();
        });

        // LessonNote
        modelBuilder.Entity<LessonNote>(entity =>
        {
            entity.ToTable("lesson_notes");
            entity.HasIndex(ln => new { ln.UserId, ln.LessonId }).IsUnique();
        });

        // Enrollments
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("enrollments");
            entity.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
            
            entity.HasOne(e => e.Student)
                  .WithMany()
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Product
        modelBuilder.Entity<Product>().ToTable("Products");

        // Quizzes
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.ToTable("quizzes");
            entity.HasMany(q => q.Questions)
                  .WithOne(qn => qn.Quiz)
                  .HasForeignKey(qn => qn.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Questions
        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("questions");
            entity.HasMany(q => q.Options)
                  .WithOne(o => o.Question)
                  .HasForeignKey(o => o.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Options
        modelBuilder.Entity<Option>(entity =>
        {
            entity.ToTable("options");
        });

        // Quiz Submissions
        modelBuilder.Entity<QuizSubmission>(entity =>
        {
            entity.ToTable("quiz_submissions");
            entity.HasOne(qs => qs.User)
                  .WithMany()
                  .HasForeignKey(qs => qs.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qs => qs.Quiz)
                  .WithMany()
                  .HasForeignKey(qs => qs.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.SupabaseId).IsUnique();
        });

        // User follows
        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("user_follows", t => t.HasCheckConstraint("CK_user_follows_not_self", "follower_id <> followed_id"));
            entity.HasIndex(f => new { f.FollowerId, f.FollowedId }).IsUnique();

            entity.HasOne(f => f.Follower)
                  .WithMany()
                  .HasForeignKey(f => f.FollowerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Followed)
                  .WithMany()
                  .HasForeignKey(f => f.FollowedId)
                  .OnDelete(DeleteBehavior.Cascade);
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
