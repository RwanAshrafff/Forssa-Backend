using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Forssa.Infrastructure.Data;

namespace Forssa.Infrastructure.Data;

public class DatabaseTriggerSetup
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseTriggerSetup> _logger;

    public DatabaseTriggerSetup(ApplicationDbContext context, ILogger<DatabaseTriggerSetup> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SetupUserSyncTriggerAsync()
    {
        try
        {
            _logger.LogInformation("Ensuring Supabase User Sync Trigger exists...");

            const string sql = @"
                -- 1. Create a function to handle new user signups
                CREATE OR REPLACE FUNCTION public.handle_new_user()
                RETURNS TRIGGER AS $$
                BEGIN
                  INSERT INTO public.""Users"" (""Id"", ""SupabaseId"", ""Email"", ""Role"", ""CreatedAt"", ""IsDeleted"", ""UpdatedAt"")
                  VALUES (
                    new.id, 
                    new.id::text, 
                    new.email, 
                    COALESCE(new.raw_user_meta_data->>'role', 'Student'),
                    now(),
                    false,
                    now()
                  )
                  ON CONFLICT (""SupabaseId"") DO UPDATE 
                  SET ""Email"" = EXCLUDED.""Email"",
                      ""Role"" = COALESCE(EXCLUDED.""Role"", public.""Users"".""Role""),
                      ""UpdatedAt"" = now();
                  RETURN NEW;
                END;
                $$ LANGUAGE plpgsql SECURITY DEFINER;

                -- 2. Create the trigger if not exists
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'on_auth_user_created') THEN
                        CREATE TRIGGER on_auth_user_created
                          AFTER INSERT ON auth.users
                          FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
                    END IF;
                END
                $$;
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Supabase User Sync Trigger setup successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup Supabase User Sync Trigger. This is expected if the DB user lacks permissions on 'auth' schema.");
        }
    }
}
