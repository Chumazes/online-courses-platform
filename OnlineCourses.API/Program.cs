using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using OnlineCourses.API.Infrastructure;
using OnlineCourses.Data;
using OnlineCourses.Data.Repositories.Implementations;
using OnlineCourses.Data.Repositories.Interfaces;
using OnlineCourses.API.Services.Implementations;
using OnlineCourses.API.Services.Interfaces;

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting API...");

    var builder = WebApplication.CreateBuilder(args);

    // Замена стандартного логирования на Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Введите JWT access token. Пример: Bearer eyJhbGciOi..."
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

    // Database
    var databaseProvider = (builder.Configuration["DatabaseProvider"] ?? "Postgres").ToLowerInvariant();
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        if (databaseProvider == "sqlite")
        {
            var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
                ?? "Data Source=online-courses-dev.db";
            options.UseSqlite(sqliteConnection);
            return;
        }

        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    // JWT Authentication
    var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "super-secret-key-32-chars-long-for-jwt!";
    var key = Encoding.UTF8.GetBytes(jwtSecret);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // Dependency Injection
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICourseRepository, CourseRepository>();
    builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
    builder.Services.AddScoped<ISectionRepository, SectionRepository>();
    builder.Services.AddScoped<ILessonRepository, LessonRepository>();
    builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
    builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddSingleton<ICacheService, CacheService>();
    
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCors();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await DevelopmentDatabaseInitializer.InitializeAsync(app.Services, builder.Configuration);

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
