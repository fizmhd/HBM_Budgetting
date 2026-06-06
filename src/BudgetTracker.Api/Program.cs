using FastEndpoints;
using FastEndpoints.Swagger;
using Serilog;
using Microsoft.EntityFrameworkCore;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Logging;
using BudgetTracker.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Services.Interfaces;

using BudgetTracker.Api.Services;
using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Infrastructure.Middleware;
using SessionOptions = BudgetTracker.Api.Infrastructure.Options.SessionOptions;
using BudgetTracker.Api.Services.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Make the implicit Program class public for integration tests

public partial class Program {
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load local overrides (gitignored) for secrets
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add DbContext
        // Only register the Postgres provider if not in Testing environment
        // (integration tests register their own container-backed DbContext).
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        }

        // Add Unit of Work
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Generic Repository
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Add Specific Repositories
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        builder.Services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        builder.Services.AddScoped<IHouseholdMemberRepository, HouseholdMemberRepository>();
        builder.Services.AddScoped<IHouseholdInviteRepository, HouseholdInviteRepository>();
        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
        builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
        builder.Services.AddScoped<ITagRepository, TagRepository>();
        builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();

        // Configure Options
        builder.Services.Configure<SupabaseOptions>(
            builder.Configuration.GetSection("Auth:Supabase"));
        builder.Services.Configure<LockoutOptions>(
            builder.Configuration.GetSection(LockoutOptions.SectionName));
        builder.Services.Configure<SessionOptions>(
            builder.Configuration.GetSection(SessionOptions.SectionName));
        builder.Services.Configure<AuthOptions>(
            builder.Configuration.GetSection(AuthOptions.SectionName));
        builder.Services.Configure<CsrfOptions>(
            builder.Configuration.GetSection("Security:Csrf"));
        builder.Services.Configure<PasswordOptions>(
            builder.Configuration.GetSection("Security:Password"));

        // Add Supabase Client
        builder.Services.AddSingleton(provider =>
        {
            var supabaseOptions = provider.GetRequiredService<IOptions<SupabaseOptions>>().Value;
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = supabaseOptions.AutoRefreshToken,
                AutoConnectRealtime = false
            };
            return new Supabase.Client(supabaseOptions.Url, supabaseOptions.AnonKey, options);
        });

        // Add Authentication Services
        builder.Services.AddScoped<IAuthProvider, SupabaseAuthProvider>();
        builder.Services.AddSingleton<ITokenValidator, SupabaseTokenValidator>();
        builder.Services.AddScoped<IUserResolutionService, UserResolutionService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Domain services (Categories / Transactions)
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<ICategoryReferenceChecker, BudgetTracker.Api.Services.Categories.CategoryReferenceChecker>();
        builder.Services.AddScoped<ICategorySeeder, BudgetTracker.Api.Services.Categories.CategorySeeder>();
        builder.Services.AddScoped<ITransactionService, TransactionService>();
        builder.Services.AddScoped<IBalanceService, BalanceService>();
        builder.Services.AddScoped<BudgetTracker.Api.Features.Transactions.TransactionWriteService>();

        // Domain services (Budgets)
        builder.Services.AddScoped<IBudgetAlertService, BudgetTracker.Api.Services.Budgets.BudgetAlertService>();
        builder.Services.AddScoped<BudgetTracker.Api.Features.Budgets.BudgetProgressService>();
        builder.Services.AddScoped<BudgetTracker.Api.Features.Budgets.BudgetWriteService>();

        // Application email (MVP: logs instead of sending). Swap for a real provider here.
        builder.Services.AddScoped<BudgetTracker.Api.Infrastructure.Email.IEmailSender,
            BudgetTracker.Api.Infrastructure.Email.LoggingEmailSender>();

        // Add Security Services
        builder.Services.AddScoped<ICsrfService, CsrfService>();
        builder.Services.AddSingleton<PasswordValidator>();

        // Add Mappers
        builder.Services.AddScoped<UserMapper>();

        // Add HttpContextAccessor for CurrentUserService and CookieService
        builder.Services.AddHttpContextAccessor();

        // Add Cookie Service
        builder.Services.AddScoped<ICookieService, CookieService>();

        // Add Authorization services (required by app.UseAuthorization())
        builder.Services.AddAuthorization();

        // Add Authentication services (required by authorization middleware)
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Bearer";
            options.DefaultChallengeScheme = "Bearer";
        }).AddJwtBearer("Bearer", options =>
        {
            // We're using custom JWT validation middleware, so just configure minimal options
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Skip default JWT validation - our custom middleware handles it
                    context.NoResult();
                    return Task.CompletedTask;
                }
            };
        });

        // Configure CORS
        var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowWebClient", builder =>
            {
                builder.WithOrigins(corsOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials()
                       .WithExposedHeaders("X-CSRF-TOKEN");
            });
        });

        // Add services
        builder.Services.AddFastEndpoints(options =>
        {
            // Ensure validators are discovered and registered
            options.Assemblies = new[] { typeof(Program).Assembly };
        });

        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "Budget Tracker API";
                s.Version = "v1";
                s.Description = "RESTful API for Budget Tracker application";
            };
            o.EnableJWTBearerAuth = true;
        });


        var app = builder.Build();

        // Configure middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerGen();
        }

        app.UseHttpsRedirection();

        // Add CORS middleware (must be before auth and rate limiting)
        app.UseCors("AllowWebClient");

        // Add correlation ID tracking
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add request/response logging
        app.UseMiddleware<RequestLoggingMiddleware>();

        // Add JWT validation
        app.UseMiddleware<JwtValidationMiddleware>();

        // Add user context resolution
        app.UseMiddleware<UserContextMiddleware>();

        // Add authentication middleware (required before authorization)
        app.UseAuthentication();

        // Add authorization middleware (required by FastEndpoints)
        app.UseAuthorization();

        // Add CSRF protection
        app.UseMiddleware<CsrfMiddleware>();

        app.UseFastEndpoints(config =>
        {
            config.Errors.ResponseBuilder = (failures, _, _) =>
            {
                return new
                {
                    errors = failures.Select(f => new { field = f.PropertyName, message = f.ErrorMessage })
                };
            };
        });

        try
        {
            Log.Information("Starting BudgetTracker API");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

// Make the implicit Program class public for integration tests
public partial class Program { }
