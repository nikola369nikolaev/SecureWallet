import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { AdminUserDetailsPage } from '../features/admin/AdminUserDetailsPage';
import { AdminUsersPage } from '../features/admin/AdminUsersPage';
import { useAuth } from '../auth/AuthContext';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { LoginPage } from '../features/login/LoginPage';
import { RegisterPage } from '../features/register/RegisterPage';
import { VerifyEmailPage } from '../features/register/VerifyEmailPage';
import { ResetPasswordConfirmPage } from '../features/reset-password/ResetPasswordConfirmPage';
import { ResetPasswordPage } from '../features/reset-password/ResetPasswordPage';
import { SettingsPage } from '../features/settings/SettingsPage';
import { TransactionHistoryPage } from '../features/transactions/TransactionHistoryPage';
import { TransferPage } from '../features/transfers/TransferPage';
import { TwoFactorSetupPage } from '../features/two-factor/TwoFactorSetupPage';

function AuthenticatedRoute() {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}

function CompletedSecurityRoute() {
  const { isAuthenticated, session } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return session?.securitySetupRequired ? <Navigate to="/security/two-factor" replace /> : <Outlet />;
}

function PublicRoute() {
  const { isAuthenticated, session } = useAuth();

  if (!isAuthenticated) {
    return <Outlet />;
  }

  return <Navigate to={session?.securitySetupRequired ? '/security/two-factor' : '/dashboard'} replace />;
}

function RootRedirect() {
  const { isAuthenticated, session } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={session?.securitySetupRequired ? '/security/two-factor' : '/dashboard'} replace />;
}

function StaffRoute() {
  const { session } = useAuth();
  const hasStaffAccess = session?.role === 'Admin' || session?.role === 'Support';

  return hasStaffAccess ? <Outlet /> : <Navigate to="/dashboard" replace />;
}

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<RootRedirect />} />

        <Route element={<PublicRoute />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/register/verify-email" element={<VerifyEmailPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/reset-password/confirm" element={<ResetPasswordConfirmPage />} />
        </Route>

        <Route element={<AuthenticatedRoute />}>
          <Route path="/security/two-factor" element={<TwoFactorSetupPage />} />
        </Route>

        <Route element={<CompletedSecurityRoute />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/transfers" element={<TransferPage />} />
          <Route path="/transactions" element={<TransactionHistoryPage />} />
          <Route path="/settings" element={<SettingsPage />} />

          <Route element={<StaffRoute />}>
            <Route path="/admin/users" element={<AdminUsersPage />} />
            <Route path="/admin/users/:userId" element={<AdminUserDetailsPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
