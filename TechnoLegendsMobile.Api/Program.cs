using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechnoLegendsMobile.Api;
using TechnoLegendsMobile.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var legacySecret = builder.Configuration["Supabase:JwtSecret"]; // optional fallback
var issuer = $"{supabaseUrl}/auth/v1";

var jwks = new ConfigurationManager<JsonWebKeySet>(
    $"{issuer}/.well-known/jwks.json",
    new JwksRetriever(),
    new HttpDocumentRetriever());

builder.Services.AddControllers();

// right after builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();


builder.Services.AddExceptionHandler<TechnoLegendsMobile.Api.ApiExceptionHandler>();
builder.Services.AddProblemDetails();

// ...
builder.Services.AddDbContext<AppDb>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Supabase"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<AppDb>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Supabase"))
    .UseSnakeCaseNamingConvention());

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = "authenticated",
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKeyResolver = (_, _, _, _) =>
            {
                var keys = jwks.GetConfigurationAsync(CancellationToken.None)
                               .GetAwaiter().GetResult()
                               .GetSigningKeys().ToList();

                if (!string.IsNullOrEmpty(legacySecret))
                    keys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(legacySecret)));

                return keys;
            }
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Mfa", p => p.RequireAuthenticatedUser().RequireClaim("aal", "aal2"));
    o.FallbackPolicy = o.GetPolicy("Mfa");
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.Run();

class JwksRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public async Task<JsonWebKeySet> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
        => new(await retriever.GetDocumentAsync(address, cancel));
}   