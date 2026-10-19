using Microsoft.AspNetCore.Components.Authorization;
using Supabase;
using TechnoLegendsManagementSystem.Shared.Services;
using TechnoLegendsManagementSystem.Web.Components;
using TechnoLegendsManagementSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();

// Supabase client — credentials loaded from appsettings.json
builder.Services.AddSingleton(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var url = config["Supabase:https://msasxhinrbmuvrjnhhlz.supabase.co"]
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");
    var key = config["Supabase:sb_publishable_QVBOLD4JK6gM4SAiN3ykxQ_QNM6yNBJ"]
        ?? throw new InvalidOperationException("Supabase:AnonKey is not configured.");

    var options = new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true,
        SessionHandler = provider.GetRequiredService<ISessionPersistence>()
    };
    return new Supabase.Client(url, key, options);
});

builder.Services.AddScoped<ISessionPersistence, WebSessionHandler>();
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveWebAssemblyRenderMode()
   .AddAdditionalAssemblies(typeof(TechnoLegendsManagementSystem.Shared.Pages.Login).Assembly);

app.Run();