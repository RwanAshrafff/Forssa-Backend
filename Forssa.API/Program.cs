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

/// <summary>
/// Reads the role stored inside Supabase's user_metadata JWT claim and promotes it
/// to the standard ClaimTypes.Role so ASP.NET Core role-based auth works normally.
/// </summary>
public class SupabaseClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Supabase embeds role as a nested JSON string: user_metadata → { "role": "student" }
        // The JWT middleware flattens it to the claim key "user_metadata" with a JSON value,
        // OR sometimes as individual keys like "user_metadata.role".
        // We handle both patterns.
        var identity = (ClaimsIdentity)principal.Identity!;

        // Already has a role claim? Nothing to do.
        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
            return Task.FromResult(principal);

        // Pattern 1: flattened key "user_metadata.role"
        var role = identity.FindFirst("user_metadata.role")?.Value;

        // Pattern 2: raw "role" claim (Supabase sometimes puts it here too)
        role ??= identity.FindFirst("role")?.Value;

        if (!string.IsNullOrEmpty(role))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return Task.FromResult(principal);
    }
}
