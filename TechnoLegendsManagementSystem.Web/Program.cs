using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Supabase;
using TechnoLegendsManagementSystem.Shared.Services;
using TechnoLegendsManagementSystem.Web.Components;
using TechnoLegendsManagementSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();     

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var url = config["Supabase:Url"]
        ?? throw new InvalidOperationException("Supabase:Url is not configured.");
    var key = config["Supabase:AnonKey"]
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
builder.Services.AddScoped<IAuthService, AuthBridge>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();

#if DEBUG
string apiBase = "http://localhost:5080";
#else
    string apiBase = "https://your-api-host";   // change for prod
#endif

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient("api", c =>
{
    c.BaseAddress = new Uri(apiBase);
    c.Timeout = TimeSpan.FromSeconds(15);
}).AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped<ITaskService, ApiTaskService>();
builder.Services.AddScoped<IChatService, ApiChatService>();
builder.Services.AddScoped<ChatClient>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<IMeetingService, ApiMeetingService>();
builder.Services.AddScoped<INotificationService, ApiNotificationService>();

builder.Services.AddRouting();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()     
   .AddAdditionalAssemblies(
       typeof(TechnoLegendsManagementSystem.Shared.Pages.Login).Assembly);

app.Run();