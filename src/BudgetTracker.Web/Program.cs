using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using BudgetTracker.Web;
using BudgetTracker.Web.Logging;
using BudgetTracker.Web.Services;
using BudgetTracker.Web.Services.ErrorHandling;
using BudgetTracker.Web.Auth;
using Blazored.LocalStorage;
using Refit;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001";

// Configure logging
builder.Services.Configure<ClientLoggerOptions>(options =>
{
    options.Enabled = true;
    options.MinimumLevel = builder.HostEnvironment.IsDevelopment() 
        ? BudgetTracker.Web.Logging.LogLevel.Debug 
        : BudgetTracker.Web.Logging.LogLevel.Information;
});

// Configure CSRF options
builder.Services.Configure<CsrfOptions>(builder.Configuration.GetSection("Csrf"));

// Register singleton services
builder.Services.AddSingleton<IClientLogger, ClientLogger>();
builder.Services.AddSingleton<CorrelationIdService>();

// Register scoped services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<TokenManager>();

// Register scoped services
builder.Services.AddScoped<IAuthService, AuthService>();

// Register authentication
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddAuthorizationCore();

// Configure HttpClient with handler chain and Refit
// Chain: HttpClient → CorrelationIdHandler → LoggingHttpHandler → CsrfHandler → AuthHttpHandler → Server
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddTransient<LoggingHttpHandler>();
builder.Services.AddTransient<CsrfHandler>();
builder.Services.AddTransient<AuthHttpHandler>();
builder.Services.AddTransient<CredentialsHttpHandler>();
builder.Services.AddScoped<ApiErrorHandler>();

// Register a simple HttpClient for token refresh (no auth handler to avoid circular dependency)
builder.Services.AddHttpClient("RefreshClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<CredentialsHttpHandler>();

// Register Refit API client with handler chain
// Configure Refit settings
var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    })
};

// Register Refit API client with handler chain
builder.Services.AddRefitClient<IApiClient>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<CorrelationIdHandler>()
    .AddHttpMessageHandler<LoggingHttpHandler>()
    .AddHttpMessageHandler<CredentialsHttpHandler>()
    .AddHttpMessageHandler<CsrfHandler>()
    .AddHttpMessageHandler<AuthHttpHandler>();

await builder.Build().RunAsync();
