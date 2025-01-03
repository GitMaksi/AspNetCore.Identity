using AspNetCore.Identity.Database;
using AspNetCore.Identity.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

        app.MapControllers();

        app.MapIdentityApi<User>();

        app.Run();
    }
}