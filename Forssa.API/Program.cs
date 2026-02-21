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

public class SupabaseClaimsTransformation : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
            return principal;

        var userMetadataJson = identity.FindFirst("user_metadata")?.Value;
        if (!string.IsNullOrEmpty(userMetadataJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(userMetadataJson);
                var root = doc.RootElement;

                // 1. Role
                if (root.TryGetProperty("role", out var roleProp))
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleProp.GetString() ?? "Student"));

                // 2. Full Name
                if (root.TryGetProperty("full_name", out var nameProp))
                    identity.AddClaim(new Claim("full_name", nameProp.GetString() ?? ""));

                // 3. Bio
                if (root.TryGetProperty("bio", out var bioProp))
                    identity.AddClaim(new Claim("bio", bioProp.GetString() ?? ""));

                // 4. Avatar URL
                if (root.TryGetProperty("avatar_url", out var avatarProp))
                    identity.AddClaim(new Claim("avatar_url", avatarProp.GetString() ?? ""));

                // 5. Username
                if (root.TryGetProperty("username", out var userProp))
                    identity.AddClaim(new Claim("username", userProp.GetString() ?? ""));
            }
            catch { /* Ignore parsing errors */ }
        }

        // Fallback for role if not in JSON (e.g. flattened by some middleware)
        if (!identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            var role = identity.FindFirst("user_metadata.role")?.Value ?? identity.FindFirst("role")?.Value;
            if (!string.IsNullOrEmpty(role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return principal;
    }

}
