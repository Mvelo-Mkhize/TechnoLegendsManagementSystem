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
        builder.Services.AddSingleton<IAuthService, AuthBridge>();
        builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();

        // --- Tasks / chat / profile (API-backed) ---
        #if ANDROID
                // Emulator uses 10.0.2.2, physical device must use your PC's LAN IP
                string apiBase = DeviceInfo.DeviceType == DeviceType.Virtual
                    ? "http://10.0.2.2:5080"          // Android emulator
                    : "http://192.168.18.252:5080";     // physical device — your PC's LAN IP
        #else
            string apiBase = "http://localhost:5080";
        #endif

        builder.Services.AddTransient<AuthHeaderHandler>();
        builder.Services.AddHttpClient("api", c =>
        {
            c.BaseAddress = new Uri(apiBase);
            c.Timeout = TimeSpan.FromSeconds(15);
        }).AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services.AddSingleton<ITaskService, ApiTaskService>();
        builder.Services.AddSingleton<IChatService, ApiChatService>();
        builder.Services.AddSingleton<ChatClient>();
        builder.Services.AddSingleton<ProfileService>();
        builder.Services.AddSingleton<IMeetingService, ApiMeetingService>();
        builder.Services.AddSingleton<INotificationService, ApiNotificationService>();
        return builder.Build(); 

    }
}