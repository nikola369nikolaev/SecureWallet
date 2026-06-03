using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Register;

namespace SecureWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();

        return services;
    }
}
