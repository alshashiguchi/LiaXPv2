using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using AspNetCoreRateLimit;
using LiaXP.Domain.Interfaces;
using LiaXP.Infrastructure.Repositories;
using LiaXP.Infrastructure.Services;
using LiaXP.Infrastructure.WhatsApp;
using LiaXP.Application.UseCases;
using Quartz;
using LiaXP.Infrastructure.Cron;
using LiaXP.Api.Jobs;
using LiaXP.Application.UseCases.Auth;
using Microsoft.OpenApi.Models;
using LiaXP.Application.UseCases.Chat;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/liaxp-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new() { Title = "LiaXP API", Version = "v1" });
//    c.AddSecurityDefinition("Bearer", new()
//    {
//        Description = "JWT Authorization header using the Bearer scheme",
//        Name = "Authorization",
//        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });
//    c.AddSecurityRequirement(new()
//    {
//        {
//            new()
//            {
//                Reference = new()
//                {
//                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

/// ============================================================
// JWT AUTHENTICATION CONFIGURATION
// ============================================================

var jwtIssuer = builder.Configuration["JWT:Issuer"]
    ?? throw new InvalidOperationException("JWT:Issuer is required");

var jwtAudience = builder.Configuration["JWT:Audience"]
    ?? throw new InvalidOperationException("JWT:Audience is required");

if (jwtIssuer == "self")
{
    // ✅ LOCAL JWT with HS256 (Development/Self-Hosted)
    Console.WriteLine("🔐 Configuring LOCAL JWT Authentication (HS256)");

    // ✅ CRITICAL: Read key from configuration, NOT hardcoded!
    var jwtKey = builder.Configuration["JWT:SigningKey"]
        ?? throw new InvalidOperationException("JWT:SigningKey is required for local authentication");

    // Validate key size
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    if (keyBytes.Length < 32)
    {
        throw new InvalidOperationException(
            $"JWT:SigningKey is too short ({keyBytes.Length} bytes). " +
            $"Minimum required: 32 bytes (256 bits). " +
            $"Generate a secure key using: [Convert]::ToBase64String((1..64 | ForEach-Object {{ Get-Random -Minimum 0 -Maximum 256 }}))");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // No time tolerance for expiration

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };

            // ✅ Event handlers for debugging
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    logger.LogError(
                        "❌ JWT Authentication Failed: {Error} | Exception: {Exception}",
                        context.Exception.Message,
                        context.Exception.GetType().Name);

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    var userId = context.Principal?.FindFirst("sub")?.Value;
                    var email = context.Principal?.FindFirst("email")?.Value;
                    var companyCode = context.Principal?.FindFirst("company_code")?.Value;

                    logger.LogInformation(
                        "✅ JWT Token Validated | User: {Email} | Company: {CompanyCode}",
                        email,
                        companyCode);

                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    logger.LogWarning(
                        "⚠️  JWT Challenge | Error: {Error} | Description: {Description} | Path: {Path}",
                        context.Error,
                        context.ErrorDescription,
                        context.Request.Path);

                    return Task.CompletedTask;
                }
            };
        });

    Console.WriteLine($"✅ JWT Authentication configured | Issuer: {jwtIssuer} | Audience: {jwtAudience}");
}
else
{
    // ✅ AZURE AD JWT with RS256 (Production)
    Console.WriteLine("🔐 Configuring AZURE AD JWT Authentication (RS256)");

    var authority = builder.Configuration["JWT:Authority"]
        ?? throw new InvalidOperationException("JWT:Authority is required for Azure AD authentication");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = jwtAudience;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    Console.WriteLine($"✅ Azure AD JWT configured | Authority: {authority} | Audience: {jwtAudience}");
}

builder.Services.AddAuthorization();

// ============================================================
// SWAGGER CONFIGURATION
// ============================================================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LiaXP API",
        Version = "v1",
        Description = "AI-Powered Sales Assistant API with WhatsApp Integration",
        Contact = new OpenApiContact
        {
            Name = "LiaXP Support",
            Email = "support@liaxp.com"
        }
    });

    // ✅ JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.\n\n" +
                      "Enter your token in the text input below.\n\n" +
                      "Example: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"\n\n" +
                      "You don't need to add 'Bearer ' prefix - it will be added automatically."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireAdminOrManager", policy => 
        policy.RequireRole("Admin", "Manager"));
});

// Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddScoped<IMessageLogRepository, MessageLogRepository>();

// Register HttpContext accessor
builder.Services.AddHttpContextAccessor();

// Configure Quartz for scheduled jobs
// Em Program.cs, após AddQuartz()
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Exemplo: Registrar jobs para a empresa ACME
    var acmeCompanyId = Guid.NewGuid();//Guid.Parse("sua-company-guid-aqui");
    var timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    // Job 1: Mensagem Matinal (7:00)
    var morningJobKey = new JobKey("morning-messages-acme");
    q.AddJob<SendScheduledMessagesJob>(opts => opts
        .WithIdentity(morningJobKey)
        .UsingJobData("moment", "morning")
        .UsingJobData("companyId", acmeCompanyId.ToString()));

    q.AddTrigger(opts => opts
        .ForJob(morningJobKey)
        .WithIdentity("morning-trigger-acme")
        .WithCronSchedule("0 0 7 * * ?", x => x.InTimeZone(timezone))
        .WithDescription("Mensagem matinal às 7h"));

    // Job 2: Mensagem Meio-dia (12:00)
    var middayJobKey = new JobKey("midday-messages-acme");
    q.AddJob<SendScheduledMessagesJob>(opts => opts
        .WithIdentity(middayJobKey)
        .UsingJobData("moment", "midday")
        .UsingJobData("companyId", acmeCompanyId.ToString()));

    q.AddTrigger(opts => opts
        .ForJob(middayJobKey)
        .WithIdentity("midday-trigger-acme")
        .WithCronSchedule("0 0 12 * * ?", x => x.InTimeZone(timezone))
        .WithDescription("Mensagem meio-dia às 12h"));

    // Job 3: Mensagem Noturna (18:00)
    var eveningJobKey = new JobKey("evening-messages-acme");
    q.AddJob<SendScheduledMessagesJob>(opts => opts
        .WithIdentity(eveningJobKey)
        .UsingJobData("moment", "evening")
        .UsingJobData("companyId", acmeCompanyId.ToString()));

    q.AddTrigger(opts => opts
        .ForJob(eveningJobKey)
        .WithIdentity("evening-trigger-acme")
        .WithCronSchedule("0 0 18 * * ?", x => x.InTimeZone(timezone))
        .WithDescription("Mensagem noturna às 18h"));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Register HttpClient
builder.Services.AddHttpClient();

// Register Domain Services
builder.Services.AddScoped<ISalesDataSource, SqlSalesDataSource>();
builder.Services.AddScoped<IDataImporter, ExcelDataImporter>();
builder.Services.AddScoped<IInsightsService, InsightsService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IIntentRouter, IntentRouter>();
builder.Services.AddScoped<ICronService, CronService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IAIService, OpenAIService>();
builder.Services.AddScoped<IIntentRouter, IntentRouter>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageLogRepository, MessageLogRepository>();

builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICompanyResolver, CompanyResolver>();
builder.Services.AddScoped<IProcessChatMessageUseCase, ProcessChatMessageUseCase>();
builder.Services.AddMemoryCache(); // Para cache do resolver

// HttpClient para OpenAI
builder.Services.AddHttpClient<OpenAIService>();

// Register WhatsApp Client based on provider
var whatsAppProvider = builder.Configuration["WHATS_PROVIDER"] ?? "twilio";
if (whatsAppProvider.ToLower() == "meta")
{
    builder.Services.AddScoped<IWhatsAppClient, MetaWhatsAppClient>();
}
else
{
    builder.Services.AddScoped<IWhatsAppClient, TwilioWhatsAppClient>();
}

// Register Use Cases
builder.Services.AddScoped<ImportDataUseCase>();
//builder.Services.AddScoped<ProcessChatMessageUseCase>();
builder.Services.AddScoped<ILoginUseCase, LoginUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    await next();
});

app.UseIpRateLimiting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

//// Company Scope Middleware
//app.Use(async (context, next) =>
//{
//    var path = context.Request.Path.Value?.ToLower() ?? "";

//    // Public paths that don't require company scope
//    var publicPaths = new[] { "/healthz", "/auth/token", "/webhook/whatsapp", "/swagger" };

//    if (publicPaths.Any(p => path.StartsWith(p)))
//    {
//        await next();
//        return;
//    }

//    string? companyCode = null;

//    // ✅ PRIORITY 1: Try to get from JWT claims (for authenticated requests)
//    if (context.User?.Identity?.IsAuthenticated == true)
//    {
//        companyCode = context.User.FindFirst("company_code")?.Value;

//        if (!string.IsNullOrEmpty(companyCode))
//        {
//            context.Items["CompanyCode"] = companyCode;

//            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//            logger.LogDebug(
//                "✅ Company scope set from JWT | CompanyCode: {CompanyCode} | Path: {Path}",
//                companyCode,
//                path);

//            await next();
//            return;
//        }
//    }

//    // ✅ PRIORITY 2: Try X-Company-Code header (for webhooks, public APIs)
//    companyCode = context.Request.Headers["X-Company-Code"].FirstOrDefault()
//                  ?? context.Request.Query["companyCode"].FirstOrDefault();

//    if (!string.IsNullOrEmpty(companyCode))
//    {
//        context.Items["CompanyCode"] = companyCode;

//        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//        logger.LogDebug(
//            "✅ Company scope set from header | CompanyCode: {CompanyCode} | Path: {Path}",
//            companyCode,
//            path);

//        await next();
//        return;
//    }

//    // ❌ No company code found - reject request
//    var loggerError = context.RequestServices.GetRequiredService<ILogger<Program>>();
//    loggerError.LogWarning(
//        "⚠️  Missing company scope | Path: {Path} | Authenticated: {IsAuthenticated}",
//        path,
//        context.User?.Identity?.IsAuthenticated);

//    context.Response.StatusCode = 401;
//    await context.Response.WriteAsJsonAsync(new
//    {
//        error = "Company identification required",
//        details = "Company code must be provided via JWT token (company_code claim) or X-Company-Code header"
//    });
//});

app.MapControllers();

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();
