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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LiaXP API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// Configure JWT Authentication
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "self";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "liaxp-api";

if (jwtIssuer == "self")
{
    // Local JWT with HS256
    var jwtKey = builder.Configuration["JWT_SIGNING_KEY"] 
        ?? throw new InvalidOperationException("JWT_SIGNING_KEY is required");
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
}
else
{
    // Azure AD JWT with RS256
    var authority = builder.Configuration["JWT_AUTHORITY"] 
        ?? throw new InvalidOperationException("JWT_AUTHORITY is required");
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = jwtAudience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };
        });
}

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

// Register HttpContext accessor
builder.Services.AddHttpContextAccessor();

// Configure Quartz for scheduled jobs
// Em Program.cs, após AddQuartz()
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Exemplo: Registrar jobs para a empresa ACME
    var acmeCompanyId = Guid.Parse("sua-company-guid-aqui");
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
builder.Services.AddScoped<ProcessChatMessageUseCase>();

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

// Company Scope Middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    var publicPaths = new[] { "/healthz", "/auth/token", "/webhook/whatsapp" };
    
    if (!publicPaths.Any(p => path.StartsWith(p)))
    {
        var companyCode = context.Request.Headers["X-Company-Code"].FirstOrDefault()
            ?? context.Request.Query["companyCode"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(companyCode))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "X-Company-Code header required" });
            return;
        }
        
        // TODO: Validate company code and set in context
        context.Items["CompanyCode"] = companyCode;
    }
    
    await next();
});

app.MapControllers();

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();
