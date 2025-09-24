
using CondoSphere.Data;
using CondoSphere.Data.DependencyInjection;
using CondoSphere.Data.Interfaces;
using CondoSphere.Data.Repositories;
using CondoSphere.Infrastructure;
using CondoSphere.Messaging;
using CondoSphere.Models;
using CondoSphere.Services;
using CondoSphere.Services.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var keyPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keyPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("CondoSphere");

// 2) Resources nos assemblies
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            // habilita resiliencia de conexão
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                      // tenta até 5 vezes
                maxRetryDelay: TimeSpan.FromSeconds(10), // espera até 10s entre tentativas
                errorNumbersToAdd: null);              // deixa null para padrão
        }));


builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// DI
builder.Services.AddRepositories();




builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";

    // tempo do cookie quando RememberMe = true
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    // evita que o GDPR CookieConsent bloqueie o cookie de auth
    options.Cookie.IsEssential = true;



    // Impede redirect HTML para chamadas da API
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };

});

builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
{
    opt.TokenLifespan = TimeSpan.FromDays(4);
});


// JWT (for API)
var stripeKey = builder.Configuration["Stripe:SecretKey"];
Stripe.StripeConfiguration.ApiKey = stripeKey;

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IPaymentService, PaymentServiceStripe>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<DomainNotificationService>();
builder.Services.AddScoped<IChatBotService, ChatBotService>();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IAnnouncementBadgeService, AnnouncementBadgeService>();
builder.Services.AddScoped<IVotingRepository, VotingRepository>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ZoomOnlineMeetingProvider>();
builder.Services.AddScoped<IOnlineMeetingProviderFactory, OnlineMeetingProviderFactory>();
builder.Services.AddSingleton<GoogleCalendarServiceFactory>();
builder.Services.AddTransient<GoogleMeetOnlineMeetingProvider>();
builder.Services.AddTransient<GoogleManualOnlineMeetingProvider>(); // se usar fallback
builder.Services.Configure<GoogleMeetOptions>(
builder.Configuration.GetSection("OnlineMeetings:Google"));
builder.Services.AddTransient<GoogleMeetOnlineMeetingProvider>();
builder.Services.AddScoped<IResidentEmailService, ResidentEmailService>();
builder.Services.AddScoped<IChatAlertRepository, EfChatAlertRepository>();
builder.Services.AddScoped<IStaffDirectory, StaffDirectory>();
builder.Services.AddScoped<IChatAlertService, ChatAlertService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IUnitOwnershipRepository, UnitOwnershipRepository>();
builder.Services.AddScoped<IForumRepository, ForumRepository>();





builder.Services.AddScoped<IAnnouncementReadService, AnnouncementReadService>();

builder.Services.Configure<TwilioSmsOptions>(builder.Configuration.GetSection("Twilio"));
builder.Services.AddSingleton<ISmsSender, TwilioSmsSender>();

builder.Services.AddAuthentication(options =>
{
    // SITE MVC usa cookies por omissão
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
// JWT para a API (esquema nomeado “Bearer”)
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),


          RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            var msg = $"JWT fail: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}";
            ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
               .CreateLogger("JWT").LogWarning(msg);
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
               .CreateLogger("JWT");
            log.LogWarning("JWT challenge: {Error} {Desc}", ctx.Error, ctx.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("mobile", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());

    options.AddPolicy("hub", p => p
        .WithOrigins("https://condosphere-web-app.somee.com") 
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});


builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Lax;
});


//builder.Services.AddControllersWithViews(options =>
//{
//    // Políticas de autorização
//    var policy = new AuthorizationPolicyBuilder()
//        .RequireAuthenticatedUser()
//        .Build();
//    options.Filters.Add(new AuthorizeFilter(policy));
//})
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CondoSphere API",
        Version = "v1",
        Description = "API REST para gestão de condomínios"
    });

    // <<< evita estouro quando há actions “iguais”
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    // <<< evita conflito de nomes (tipos aninhados etc.)
    c.CustomSchemaIds(t => t.FullName!.Replace("+", "."));

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Type: Bearer {your token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.CustomSchemaIds(t => t.FullName!.Replace("+", "."));

   
    c.DocInclusionPredicate((doc, apiDesc) =>
    {
        var cad = apiDesc.ActionDescriptor as ControllerActionDescriptor;
        return cad?.ControllerTypeInfo
                   .GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true)
                   .Any() == true;
    });
});



builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.KeepAliveInterval = TimeSpan.FromSeconds(10);
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(40);
});

// Se MAUI estiver em outro domínio, libere CORS do hub:


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// habilita middleware CORS (sem escolher policy global aqui)
app.UseCors("mobile");

app.UseAuthentication();
app.UseAuthorization();

// Swagger opcional
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("./v1/swagger.json", "CondoSphere API v1");
});
app.MapGet("/swagger/index", ctx =>
{
    ctx.Response.Redirect("/swagger", permanent: false);
    return Task.CompletedTask;
});


// ===== Endpoints com a policy certa =====

// Controllers/API -> policy "maui" (caso MAUI consuma a API)
app.MapControllers().RequireCors("mobile");

// Hub SignalR -> policy "hub" + LongPolling (Somee)
app.MapHub<CondoSphere.Hubs.ChatHub>("/hubs/chat", opt =>
{
    opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    opt.LongPolling.PollTimeout = TimeSpan.FromSeconds(25);
}).RequireCors("hub");

//// MVC do site
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}").RequireAuthorization();



// (seu bloco de migrate/seed permanece igual)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var ctx = sp.GetRequiredService<ApplicationDbContext>();
    try
    {
        await ctx.Database.MigrateAsync();
        await DbSeeder.SeedAsync(sp);

        var afetados = await ctx.Users
            .Where(u => u.ProfileImagePath == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.ProfileImagePath, ""));
        if (afetados > 0)
            app.Logger.LogInformation("Corrigidos {Afetados} usuários com ProfileImagePath NULL.", afetados);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Falha ao migrar/seedar o banco.");
        if (app.Environment.IsDevelopment())
            throw;
    }
}

QuestPDF.Settings.License = LicenseType.Community;

app.Run();
