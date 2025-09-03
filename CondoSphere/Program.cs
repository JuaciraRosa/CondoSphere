using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using CondoSphere.Data;
using CondoSphere.Data.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CondoSphere.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using CondoSphere.Services;
using System;
using CondoSphere.Messaging;

var builder = WebApplication.CreateBuilder(args);

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



// MVC (cookies) for web
// Cookies para MVC
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
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
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<DomainNotificationService>();



builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// CORS (allow mobile to call your API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("maui",
        p => p.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});



builder.Services.AddControllersWithViews(options =>
{
    // Políticas de autorização
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddViewLocalization()
.AddDataAnnotationsLocalization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CondoSphere API",
        Version = "v1",
        Description = "API REST para gestão de condomínios"
    });

    // ?? JWT in Swagger
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

    // ? Avoid “Conflicting schemaIds” (User, etc.)
    c.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
});


var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("maui");

// Important: auth order
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment() || true) // deixar sempre ativo por enquanto
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CondoSphere API v1");
        c.RoutePrefix = string.Empty; // Swagger abre na raiz /
    });
}

// Map API and MVC
app.MapControllers(); // if using attribute routing for API
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
        throw;
    }
}
app.Run();