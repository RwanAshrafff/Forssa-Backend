using Forssa.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Supabase:Authority"];
        options.Audience = builder.Configuration["Supabase:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Map Supabase user_metadata.role → ClaimTypes.Role so [Authorize(Roles=...)] works
builder.Services.AddScoped<IClaimsTransformation, SupabaseClaimsTransformation>();

builder.Services.AddAuthorization();

var app = builder.Build();

// --- NEW: Run Database Trigger Setup ---
using (var scope = app.Services.CreateScope())
{
    var triggerSetup = scope.ServiceProvider.GetRequiredService<Forssa.Infrastructure.Data.DatabaseTriggerSetup>();
    await triggerSetup.SetupUserSyncTriggerAsync();
}
// ---------------------------------------


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Reads the role stored inside Supabase's user_metadata JWT claim and promotes it
/// to the standard ClaimTypes.Role so ASP.NET Core role-based auth works normally.
/// </summary>
public class SupabaseClaimsTransformation : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
            return principal;

        // Pattern 1: flattened key "user_metadata.role"
        var role = identity.FindFirst("user_metadata.role")?.Value;

        // Pattern 2: raw "role" claim
        role ??= identity.FindFirst("role")?.Value;

        // Pattern 3: user_metadata as JSON string
        if (string.IsNullOrEmpty(role))
        {
            var userMetadataJson = identity.FindFirst("user_metadata")?.Value;
            if (!string.IsNullOrEmpty(userMetadataJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(userMetadataJson);
                    if (doc.RootElement.TryGetProperty("role", out var roleProp))
                    {
                        role = roleProp.GetString();
                    }
                }
                catch { /* Ignore parsing errors */ }
            }
        }

        if (!string.IsNullOrEmpty(role))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return principal;
    }

}
