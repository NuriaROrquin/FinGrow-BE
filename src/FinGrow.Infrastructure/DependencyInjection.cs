namespace FinGrow.Infrastructure;

using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using FinGrow.Application.Features.Receipts.Cleanup;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Infrastructure.Ai;
using FinGrow.Infrastructure.Identity;
using FinGrow.Infrastructure.Integrations;
using FinGrow.Infrastructure.Integrations.ArgentinaDatos;
using FinGrow.Infrastructure.Integrations.Byma;
using FinGrow.Infrastructure.Integrations.CoinGecko;
using FinGrow.Infrastructure.Integrations.DolarApi;
using FinGrow.Infrastructure.Integrations.Gmail;
using FinGrow.Infrastructure.Integrations.Iol;
using FinGrow.Infrastructure.Integrations.MercadoPago;
using FinGrow.Infrastructure.Integrations.Telegram;
using FinGrow.Infrastructure.Integrations.Twilio;
using FinGrow.Infrastructure.Mailing;
using FinGrow.Infrastructure.Persistence;
using FinGrow.Infrastructure.Persistence.Protection;
using FinGrow.Infrastructure.Persistence.Repositories;
using FinGrow.Infrastructure.Persistence.Seeding;
using FinGrow.Infrastructure.Services;
using FinGrow.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

public static class DependencyInjection
{
    private const string PriceSourcesUserAgent = "FinGrow/1.0";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddAiService(configuration);
        services.AddJwtAuthentication(configuration);

        services.AddTwilio(configuration);
        services.AddTelegram(configuration);
        services.AddMercadoPago(configuration);
        services.AddGmail(configuration);
        services.AddIol(configuration);
        services.AddDolarApi(configuration);
        services.AddByma(configuration);
        services.AddArgentinaDatos(configuration);
        services.AddCoinGecko(configuration);
        services.AddFileStorage(configuration);
        services.AddEmail(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IEmployeeIntegrationRepository, EmployeeIntegrationRepository>();
        services.AddScoped<IIntegrationLinkCodeRepository, IntegrationLinkCodeRepository>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexion 'ConnectionStrings:Database' en la configuracion.");

        services.AddOptions<TokenEncryptionOptions>()
            .Bind(configuration.GetSection(TokenEncryptionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();

        services.AddScoped<DomainEventsInterceptor>();

        services.AddDbContext<FinGrowDbContext>((provider, options) =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(FinGrowDbContext).Assembly.FullName))
                .AddInterceptors(provider.GetRequiredService<DomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FinGrowDbContext>());
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionReceiptRepository, TransactionReceiptRepository>();
        services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
        services.AddScoped<IMonthlySummaryReadRepository, MonthlySummaryReadRepository>();
        services.AddScoped<ITransactionExcelExporter, TransactionExcelExporter>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IInvestmentRepository, InvestmentRepository>();
        services.AddScoped<IInvestmentReadRepository, InvestmentReadRepository>();
        services.AddScoped<ISecurityPriceRepository, SecurityPriceRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseRatingRepository, CourseRatingRepository>();
        services.AddScoped<ICourseRatingReadRepository, CourseRatingReadRepository>();
        services.AddScoped<IJobRunRepository, JobRunRepository>();
        services.AddScoped<IMetricsSnapshotRepository, MetricsSnapshotRepository>();
        services.AddScoped<IFinancialHealthScoreRepository, FinancialHealthScoreRepository>();
        services.AddScoped<IFinancialHealthReadRepository, FinancialHealthReadRepository>();
        services.AddScoped<IPeriodActivityReadRepository, PeriodActivityReadRepository>();
        services.AddScoped<ICompanyEducationMetricsReadRepository, CompanyEducationMetricsReadRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetSpendingReadRepository, BudgetSpendingReadRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationChannelSettingRepository, NotificationChannelSettingRepository>();
        services.AddScoped<INotificationTypeSettingRepository, NotificationTypeSettingRepository>();
        services.AddScoped<ICompanyNotificationSettingRepository, CompanyNotificationSettingRepository>();
        services.AddScoped<IOutboundEmailRepository, OutboundEmailRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IAchievementProgressReadRepository, AchievementProgressReadRepository>();

        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<EducationCatalogSeeder>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeIntegrationRepository, EmployeeIntegrationRepository>();
        services.AddScoped<IIntegrationLinkCodeRepository, IntegrationLinkCodeRepository>();

        return services;
    }

    private static IServiceCollection AddAiService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IAiService, AiService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AiServiceOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add(AiServiceOptions.ApiKeyHeader, options.ApiKey);
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHealthChecks()
            .AddCheck<AiServiceHealthCheck>("ai", HealthStatus.Degraded, timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    private static IServiceCollection AddTwilio(this IServiceCollection services, IConfiguration configuration)
    {
        var twilioOptions = services.AddOptions<TwilioOptions>()
            .Bind(configuration.GetSection(TwilioOptions.SectionName));

        if (!configuration.GetValue<bool>("Twilio:SkipValidation"))
        {
            twilioOptions
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        services.AddSingleton<ITwilioRequestValidator, TwilioRequestValidator>();

        services.AddHttpClient<ITwilioMediaClient, TwilioMediaClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<TwilioOptions>>().Value;
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.AccountSid}:{options.AuthToken}"));

                client.Timeout = TimeSpan.FromSeconds(options.MediaTimeoutSeconds);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITelegramWebhookValidator, TelegramWebhookValidator>();

        services.AddHttpClient<ITelegramBotClient, TelegramBotClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<TelegramOptions>>().Value;

                client.BaseAddress = new Uri($"https://api.telegram.org/bot{options.BotToken}/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IServiceCollection AddMercadoPago(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MercadoPagoOptions>()
            .Bind(configuration.GetSection(MercadoPagoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMercadoPagoOAuthClient, MercadoPagoOAuthClient>(ConfigureMercadoPagoClient)
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IMercadoPagoPaymentsClient, MercadoPagoPaymentsClient>(ConfigureMercadoPagoClient)
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IServiceCollection AddGmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GmailOptions>()
            .Bind(configuration.GetSection(GmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IGmailOAuthClient, GmailOAuthClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GmailOptions>>().Value;

                client.BaseAddress = new Uri(options.OAuthBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IGmailMailboxClient, GmailMailboxClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GmailOptions>>().Value;

                client.BaseAddress = new Uri(options.ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    // Sin la politica de reintentos: repetir un refresh que IOL ya proceso puede invalidar el refresh token nuevo.
    private static IServiceCollection AddIol(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IolOptions>()
            .Bind(configuration.GetSection(IolOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IIolAuthClient, IolAuthClient>(ConfigureIolClient);

        // Tampoco reintenta: si IOL no responde, la sincronizacion no toca nada y la siguiente corrida lo vuelve a pedir.
        services.AddHttpClient<IIolPortfolioClient, IolPortfolioClient>(ConfigureIolClient);

        return services;
    }

    private static void ConfigureIolClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<IolOptions>>().Value;

        client.BaseAddress = new Uri(options.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    private static IServiceCollection AddDolarApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<DolarApiOptions>()
            .Bind(configuration.GetSection(DolarApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IExchangeRateProvider, DolarApiExchangeRateProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<DolarApiOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    private static IServiceCollection AddByma(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<BymaOptions>()
            .Bind(configuration.GetSection(BymaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMarketPriceProvider, BymaMarketPriceProvider>(BymaOptions.HttpClientName, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<BymaOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    private static IServiceCollection AddArgentinaDatos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<ArgentinaDatosOptions>()
            .Bind(configuration.GetSection(ArgentinaDatosOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMarketPriceProvider, ArgentinaDatosFundPriceProvider>(
            ArgentinaDatosOptions.HttpClientName,
            (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ArgentinaDatosOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(PriceSourcesUserAgent);
            });

        return services;
    }

    private static IServiceCollection AddCoinGecko(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<CoinGeckoOptions>()
            .Bind(configuration.GetSection(CoinGeckoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IMarketPriceProvider, CoinGeckoCryptoPriceProvider>(
            CoinGeckoOptions.HttpClientName,
            (provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(PriceSourcesUserAgent);

                if (!string.IsNullOrWhiteSpace(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add(CoinGeckoOptions.ApiKeyHeader, options.ApiKey);
                }
            });

        return services;
    }

    private static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;

            return new AmazonS3Client(
                new BasicAWSCredentials(options.AccessKey, options.SecretKey),
                new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    AuthenticationRegion = options.Region,
                    ForcePathStyle = options.ForcePathStyle,
                    Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                    // R2 y S3Mock no aceptan los checksums que el SDK v4 agrega por defecto.
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                });
        });

        services.AddSingleton<IFileStorage, S3FileStorage>();

        services.AddOptions<ReceiptsCleanupOptions>()
            .Bind(configuration.GetSection(ReceiptsCleanupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ReceiptsCleanupOptions>>().Value);

        return services;
    }

    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => string.IsNullOrWhiteSpace(options.RedirectAllTo) || new EmailAddressAttribute().IsValid(options.RedirectAllTo),
                "Email:RedirectAllTo tiene que ser una direccion de correo valida.")
            .ValidateOnStart();

        services.AddHttpClient<IEmailSender, ResendEmailSender>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;

            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ResendApiKey);
        });

        return services;
    }

    private static void ConfigureMercadoPagoClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;

        client.BaseAddress = new Uri(options.ApiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var options = jwtOptions.Value;

                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();

        return services;
    }
}
