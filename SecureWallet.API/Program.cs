using SecureWallet.Application;
using SecureWallet.Infrastructure;
using SecureWallet.Infrastructure.Data.Seed;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

await RoleSeeder.SeedDefaultRolesAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
