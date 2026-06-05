using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Register;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Features.Wallets.Queries.GetCurrentUserWallet;

namespace SecureWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<RequestPasswordResetCodeHandler>();
        services.AddScoped<VerifyPasswordResetCodeHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<GetCurrentUserWalletHandler>();

        return services;
    }
}
