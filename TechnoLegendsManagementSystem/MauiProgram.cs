using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Supabase;
using TechnoLegendsManagementSystem.Services;
using TechnoLegendsManagementSystem.Shared.Services;

namespace TechnoLegendsManagementSystem;

public static class MauiProgram
{
    private const string SupabaseUrl = "https://msasxhinrbmuvrjnhhlz.supabase.co";
    private const string SupabaseAnonKey = "sb_publishable_QVBOLD4JK6gM4SAiN3ykxQ_QNM6yNBJ";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton(provider =>
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                SessionHandler = provider.GetRequiredService<ISessionPersistence>()
            };
            return new Supabase.Client(SupabaseUrl, SupabaseAnonKey, options);
        });

        builder.Services.AddSingleton<ISessionPersistence, MauiSessionHandler>();
        builder.Services.AddSingleton<SupabaseAuthService>();
        builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        return builder.Build();
    }
}