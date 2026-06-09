using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using SecureWallet.Application;
using SecureWallet.Infrastructure;
using SecureWallet.Infrastructure.Data.Seed;

Console.OutputEncoding = Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string logsDirectoryPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "logs"));
string logFilePath = Path.Combine(logsDirectoryPath, "securewallet-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        encoding: Encoding.UTF8,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    builder.Host.UseSerilog();

    string jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Не беше намерен JWT ключът.");

    string jwtIssuer = builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Не беше намерен JWT издателят.");

    string jwtAudience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Не беше намерена JWT аудиторията.");

    builder.Services.AddControllers();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    WebApplication app = builder.Build();

    await RoleSeeder.SeedDefaultRolesAsync(app.Services);
    await AdminAccountSeeder.SeedAdminAccountAsync(app.Services);

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP заявка {RequestMethod} {RequestPath} -> {StatusCode} за {Elapsed:0.0000} ms";
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("SecureWallet API стартира успешно.");
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "SecureWallet API спря неочаквано.");
}
finally
{
    Log.CloseAndFlush();
}
