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


var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, AppClaimsPrincipalFactory>();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IQuotaService, QuotaService>();
builder.Services.AddScoped<IPaymentService, PaymentServiceStripe>();



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


// MVC
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(new AuthorizeFilter(policy));
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

// Map API and MVC
app.MapControllers(); // if using attribute routing for API
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1) Cria as tabelas (inclui AspNetUsers/AspNetRoles)
    await ctx.Database.MigrateAsync();

    // 2) Só depois faz o seed
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}
app.Run();