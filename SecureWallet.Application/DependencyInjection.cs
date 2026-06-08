using Microsoft.Extensions.DependencyInjection;
using SecureWallet.Application.Features.Admin.Commands.CreateSupportAccount;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUserDetails;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUsers;
using SecureWallet.Application.Features.Admin.Queries.GetAdminUserTransactions;
using SecureWallet.Application.Features.Auth;
using SecureWallet.Application.Features.Auth.Commands.Login;
using SecureWallet.Application.Features.Auth.Commands.Refresh;
using SecureWallet.Application.Features.Auth.Commands.Register;
using SecureWallet.Application.Features.Auth.Commands.ResetPassword;
using SecureWallet.Application.Features.Auth.Commands.Totp;
using SecureWallet.Application.Features.Auth.Commands.VerifyEmail;
using SecureWallet.Application.Features.Transactions.Commands.CreateDeposit;
using SecureWallet.Application.Features.Transactions.Commands.CreateTransfer;
using SecureWallet.Application.Features.Transactions.Queries.GetCurrentUserTransactionHistory;
using SecureWallet.Application.Features.Wallets.Queries.GetCurrentUserWallet;

namespace SecureWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthSessionIssuer>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<RequestPasswordResetCodeHandler>();
        services.AddScoped<VerifyPasswordResetCodeHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<VerifyEmailCodeHandler>();
        services.AddScoped<ResendEmailVerificationCodeHandler>();
        services.AddScoped<BeginTotpSetupHandler>();
        services.AddScoped<VerifyTotpSetupHandler>();
        services.AddScoped<DisableTotpHandler>();
        services.AddScoped<ResetTotpSetupHandler>();
        services.AddScoped<CreateSupportAccountHandler>();
        services.AddScoped<GetAdminUsersHandler>();
        services.AddScoped<GetAdminUserDetailsHandler>();
        services.AddScoped<GetAdminUserTransactionsHandler>();
        services.AddScoped<CreateDepositHandler>();
        services.AddScoped<CreateTransferHandler>();
        services.AddScoped<GetCurrentUserTransactionHistoryHandler>();
        services.AddScoped<GetCurrentUserWalletHandler>();

        return services;
    }
}
