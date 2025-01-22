using CleanArchitecture.Application.Cache;
using CleanArchitecture.Application.Repositories;
using CleanArchitecture.Domain.Models;
using CleanArchitecture.Infrastructure.Cache;
using CleanArchitecture.Infrastructure.Database;
using CleanArchitecture.Infrastructure.Database.Repositories;
using CleanArchitecture.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
         
        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);

        builder.Services.AddIdentityCore<User>()
            .AddEntityFrameworkStores<ApplicationDBContext>()
            .AddApiEndpoints();

        builder.Services.AddDbContext<ApplicationDBContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "SampleInstance";
        });

        builder.Services.AddScoped<ITasksRepository, TaskRepository>();
        builder.Services.AddScoped<ICacheService, InMemoryCacheService>();

        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Input your Bearer token to access this API"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {            
            app.UseSwagger();
            app.UseSwaggerUI();            

            app.ApplyMigrations();
        }

        app.MapGet("users/me", async (ClaimsPrincipal claims, ApplicationDBContext context) =>
        {
            string userId = claims.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;

            return await context.Users.FindAsync(userId);
        })
        .RequireAuthorization();

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.MapIdentityApi<User>();

        app.Run();
    }
}