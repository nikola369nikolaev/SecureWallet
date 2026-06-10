import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAdminLogs, getAdminUsers } from '../../api/adminApi';
import { getCurrentWallet } from '../../api/walletApi';
import { createDeposit, getTransactionHistoryPage } from '../../api/transactionApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

function formatStatus(value) {
  return value ? 'Потвърден' : 'Непотвърден';
}

function formatAmount(transaction) {
  const prefix = transaction.isIncoming ? '+' : '-';
  const amount = new Intl.NumberFormat('bg-BG', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(transaction.amount);

  return `${prefix}${amount} ${transaction.currency}`;
}

function formatCurrencyAmount(amount, currency) {
  const formattedAmount = new Intl.NumberFormat('bg-BG', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);

  return `${formattedAmount} ${currency}`;
}

function formatDate(value) {
  return new Intl.DateTimeFormat('bg-BG', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function getCurrentAnalysisPeriod() {
  const now = new Date();

  return {
    month: now.getMonth() + 1,
    year: now.getFullYear(),
  };
}

function getAnalysisMonthOptions() {
  const now = new Date();

  return Array.from({ length: 12 }, (_, index) => {
    const current = new Date(now.getFullYear(), now.getMonth() - index, 1);
    const label = new Intl.DateTimeFormat('bg-BG', {
      month: 'long',
      year: 'numeric',
    }).format(current);

    return {
      key: `${current.getFullYear()}-${String(current.getMonth() + 1).padStart(2, '0')}`,
      month: current.getMonth() + 1,
      year: current.getFullYear(),
      label: `${label.charAt(0).toUpperCase()}${label.slice(1)}`,
    };
  });
}

function getTransactionTitle(transaction) {
  if (transaction.isIncoming && transaction.counterpartyUsername === 'SecureWallet') {
    return 'Депозит в портфейла';
  }

  return transaction.isIncoming
    ? `Получени от ${transaction.counterpartyUsername}`
    : `Изпратени към ${transaction.counterpartyUsername}`;
}

function getAuditDisplayText(line) {
  const timestampMatch = line.match(/^\[(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2})(?::\d{2})? [A-Z]+\]\s*/);
  const message = line.replace(/^\[[^\]]+\]\s*/, '');

  if (!timestampMatch) {
    return message;
  }

  return `${timestampMatch[2]} | ${message}`;
}

function isHttpLogLine(line) {
  return line.includes('HTTP ') && line.includes('->');
}

function isInvalidCredentialWarningLine(line) {
  const lowerLine = line.toLowerCase();

  return lowerLine.includes('вход: защитна проверка за имейл') &&
    lowerLine.includes('грешен имейл или парола');
}

function isLockoutSecurityLine(line) {
  const lowerLine = line.toLowerCase();

  return lowerLine.includes('stage=lockoutactive') ||
    lowerLine.includes('lockoutseconds=') ||
    lowerLine.includes('твърде много неуспешни опити за вход') ||
    lowerLine.includes('опитай отново след');
}

function getSecurityAttemptCount(line) {
  const attemptsMatch = line.match(/Attempts=(\d+)/i);

  if (attemptsMatch) {
    return Number(attemptsMatch[1]);
  }

  const failedAttemptMatch = line.match(/опит номер:\s*(\d+)/i);

  if (failedAttemptMatch) {
    return Number(failedAttemptMatch[1]);
  }

  return null;
}

function isCriticalSecurityLogLine(line) {
  if (isLockoutSecurityLine(line)) {
    return true;
  }

  const attemptCount = getSecurityAttemptCount(line);

  if (attemptCount !== null && attemptCount >= 5) {
    return true;
  }

  if (isInvalidCredentialWarningLine(line)) {
    return false;
  }

  const lowerLine = line.toLowerCase();

  if (line.includes('[FTL]') || line.includes('[ERR]')) {
    return true;
  }

  if (!line.includes('[WRN]')) {
    return false;
  }

  return [
    'грешна парола',
    'невалидна парола',
    'невалиден код',
    'captcha',
    'капча',
    'lockout',
    'блок',
    'неуспеш',
    'reset',
    'ресет',
    'парола',
    'ddos',
    'подозр',
  ].some((keyword) => lowerLine.includes(keyword));
}

function isActionLogLine(line) {
  if (isHttpLogLine(line) || isCriticalSecurityLogLine(line) || isInvalidCredentialWarningLine(line)) {
    return false;
  }

  const lowerLine = line.toLowerCase();

  return [
    'транзакции:',
    'администрация:',
    'вход:',
    'регистрация:',
    'totp',
    'email verification',
    'имейл',
    'refresh',
    'парола:',
  ].some((keyword) => lowerLine.includes(keyword));
}

function isSecurityMonitoringLine(line) {
  if (isHttpLogLine(line)) {
    return false;
  }

  const lowerLine = line.toLowerCase();

  if (lowerLine.includes('totp настройка:')) {
    return false;
  }

  return [
    'вход:',
    'защитна проверка',
    'captcha',
    'капча',
    'totp',
    'парола',
    'lockout',
    'блок',
    'регистрация отказана',
    'смяна на парола:',
    'имейл потвърждение отказано',
    'повторно изпращане на имейл код отказано',
  ].some((keyword) => lowerLine.includes(keyword));
}

function getAuditLinePresentation(line) {
  if (isInvalidCredentialWarningLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--warning',
      badgeClassName: 'audit-badge audit-badge--warning',
      badgeText: 'Опит',
    };
  }

  if (isCriticalSecurityLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--critical',
      badgeClassName: 'audit-badge audit-badge--critical',
      badgeText: 'Критично',
    };
  }

  if (isActionLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--action',
      badgeClassName: 'audit-badge audit-badge--action',
      badgeText: 'Действие',
    };
  }

  if (line.includes('[WRN]')) {
    return {
      rowClassName: 'audit-entry audit-entry--warning',
      badgeClassName: 'audit-badge audit-badge--warning',
      badgeText: 'Предупр.',
    };
  }

  if (isHttpLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--http',
      badgeClassName: 'audit-badge audit-badge--http',
      badgeText: 'HTTP',
    };
  }

  return {
    rowClassName: 'audit-entry audit-entry--neutral',
    badgeClassName: 'audit-badge audit-badge--neutral',
    badgeText: 'Лог',
  };
}

function buildAdminLogSnapshot(lines) {
  const orderedLines = [...lines].reverse();
  const uniqueLines = orderedLines.filter((line, index) => orderedLines.indexOf(line) === index);

  return {
    criticalLines: uniqueLines.filter((line) => isCriticalSecurityLogLine(line)),
    securityLines: uniqueLines.filter((line) => isSecurityMonitoringLine(line) && !isCriticalSecurityLogLine(line)),
  };
}

export function DashboardPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [wallet, setWallet] = useState(null);
  const [recentTransactions, setRecentTransactions] = useState([]);
  const [analysisSummary, setAnalysisSummary] = useState({
    incomingCount: 0,
    incomingTotal: 0,
    outgoingCount: 0,
    outgoingTotal: 0,
    depositCount: 0,
    depositTotal: 0,
    currency: 'EUR',
  });
  const [adminOverview, setAdminOverview] = useState({
    supportCount: 0,
    userCount: 0,
  });
  const [adminLogSnapshot, setAdminLogSnapshot] = useState({
    criticalLines: [],
    securityLines: [],
  });
  const [criticalLogsPage, setCriticalLogsPage] = useState(1);
  const [securityLogsPage, setSecurityLogsPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [isBalanceVisible, setIsBalanceVisible] = useState(true);
  const [selectedAnalysisPeriod, setSelectedAnalysisPeriod] = useState(getCurrentAnalysisPeriod);
  const [isDepositOpen, setIsDepositOpen] = useState(false);
  const [depositAmount, setDepositAmount] = useState('');
  const [depositTotpCode, setDepositTotpCode] = useState('');
  const [depositErrorMessage, setDepositErrorMessage] = useState('');
  const [isDepositing, setIsDepositing] = useState(false);

  const analysisMonthOptions = getAnalysisMonthOptions();
  const selectedAnalysisPeriodKey = `${selectedAnalysisPeriod.year}-${String(selectedAnalysisPeriod.month).padStart(2, '0')}`;
  const isAdmin = session?.role === 'Admin';
  const isSupport = session?.role === 'Support';

  const loadDashboard = useCallback(async () => {
    if (!session?.accessToken) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      if (isAdmin) {
        const [walletResult, adminUsersResult, adminLogsResult] = await Promise.all([
          getCurrentWallet(session.accessToken),
          getAdminUsers(session.accessToken),
          getAdminLogs(session.accessToken, 120),
        ]);

        setWallet(walletResult);
        setAdminOverview({
          supportCount: adminUsersResult.filter((user) => user.role === 'Support').length,
          userCount: adminUsersResult.filter((user) => user.role === 'User').length,
        });
        setAdminLogSnapshot(buildAdminLogSnapshot(adminLogsResult.lines ?? []));
        setCriticalLogsPage(1);
        setSecurityLogsPage(1);
        setRecentTransactions([]);
        setAnalysisSummary({
          incomingCount: 0,
          incomingTotal: 0,
          outgoingCount: 0,
          outgoingTotal: 0,
          depositCount: 0,
          depositTotal: 0,
          currency: walletResult.currency ?? 'EUR',
        });
      } else {
        const [walletResult, recentTransactionsResult, analysisSummaryResult] = await Promise.all([
          getCurrentWallet(session.accessToken),
          getTransactionHistoryPage({ page: 1, pageSize: 5 }, session.accessToken),
          getTransactionHistoryPage(
            {
              dateRange: 'Month',
              month: selectedAnalysisPeriod.month,
              year: selectedAnalysisPeriod.year,
              page: 1,
              pageSize: 1,
            },
            session.accessToken,
          ),
        ]);

        setWallet(walletResult);
        setRecentTransactions(recentTransactionsResult.items);
        setAnalysisSummary(analysisSummaryResult.summary);
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        navigate('/login', {
          replace: true,
          state: { sessionExpired: true },
        });
        return;
      }

      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на портфейла.');
    } finally {
      setIsLoading(false);
    }
  }, [isAdmin, logout, navigate, selectedAnalysisPeriod.month, selectedAnalysisPeriod.year, session?.accessToken]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  async function handleDepositSubmit(event) {
    event.preventDefault();
    setDepositErrorMessage('');
    setSuccessMessage('');
    setIsDepositing(true);

    try {
      const result = await createDeposit(
        {
          amount: Number(depositAmount),
          totpCode: depositTotpCode,
        },
        session.accessToken,
      );

      setIsDepositOpen(false);
      setDepositAmount('');
      setDepositTotpCode('');
      setSuccessMessage(`${result.message} Нов баланс: ${new Intl.NumberFormat('bg-BG', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(result.updatedBalance)} ${result.currency}.`);
      await loadDashboard();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        navigate('/login', {
          replace: true,
          state: { sessionExpired: true },
        });
        return;
      }

      setDepositErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при депозита.');
    } finally {
      setIsDepositing(false);
    }
  }

  function openDepositModal() {
    setDepositErrorMessage('');
    setSuccessMessage('');
    setIsDepositOpen(true);
  }

  function closeDepositModal() {
    setDepositErrorMessage('');
    setDepositAmount('');
    setDepositTotpCode('');
    setIsDepositOpen(false);
  }

  const formattedBalance = wallet
    ? new Intl.NumberFormat('bg-BG', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(wallet.balance)
    : '';

  const hiddenBalance = '****';

  const formattedCreatedAt = wallet?.createdAtUtc
    ? new Intl.DateTimeFormat('bg-BG', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(new Date(wallet.createdAtUtc))
    : '';

  const formattedAccessRefreshAt = session?.expiresAtUtc
    ? new Intl.DateTimeFormat('bg-BG', {
        dateStyle: 'medium',
        timeStyle: 'short',
      }).format(new Date(session.expiresAtUtc))
    : '-';

  function handleAnalysisPeriodChange(event) {
    const [year, month] = event.target.value.split('-').map(Number);

    setSelectedAnalysisPeriod({
      month,
      year,
    });
  }

  const emailVerified = wallet?.isEmailVerified ?? session?.isEmailVerified ?? false;
  const hasStaffAccess = session?.role === 'Admin' || session?.role === 'Support';
  const hasAdminAccess = session?.role === 'Admin';
  const canUseFinancialActions = session?.role !== 'Admin';
  const adminLogsPageSize = 4;
  const criticalLogsPageCount = Math.max(1, Math.ceil(adminLogSnapshot.criticalLines.length / adminLogsPageSize));
  const securityLogsPageCount = Math.max(1, Math.ceil(adminLogSnapshot.securityLines.length / adminLogsPageSize));
  const pagedCriticalLines = adminLogSnapshot.criticalLines.slice(
    (criticalLogsPage - 1) * adminLogsPageSize,
    criticalLogsPage * adminLogsPageSize,
  );
  const pagedSecurityLines = adminLogSnapshot.securityLines.slice(
    (securityLogsPage - 1) * adminLogsPageSize,
    securityLogsPage * adminLogsPageSize,
  );

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Начало</p>
            <h1>Добре дошъл, {wallet?.username ?? session?.username}</h1>
          </div>
          <button className="secondary-button" onClick={logout} type="button">
            Изход
          </button>
        </div>

        <div className="inline-action-row">
          {canUseFinancialActions && (
            <button className="secondary-button" onClick={openDepositModal} type="button">
              Депозирай пари
            </button>
          )}
          {canUseFinancialActions && (
            <Link className="secondary-link-button" to="/transfers">
              Преводи
            </Link>
          )}
          <Link className="secondary-link-button" to="/transactions">
            История
          </Link>
          {hasStaffAccess && (
            <Link className="secondary-link-button" to="/admin/users">
              Потребители
            </Link>
          )}
          {hasAdminAccess && (
            <Link className="secondary-link-button" to="/admin/users#create-support">
              Създай служител support
            </Link>
          )}
          {hasAdminAccess && (
            <Link className="secondary-link-button" to="/admin/logs">
              Логове
            </Link>
          )}
          <Link className="secondary-link-button" to="/settings">
            Още
          </Link>
        </div>

        {successMessage && <div className="message-box message-box--success">{successMessage}</div>}
        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        {canUseFinancialActions && isDepositOpen && (
          <div className="modal-backdrop" role="presentation">
            <article className="dashboard-card modal-card">
              <div className="dashboard-header dashboard-header--compact">
                <div>
                  <p className="eyebrow">Депозит</p>
                  <h2>Добави пари в портфейла</h2>
                  <p className="dashboard-copy">
                    Въведи сумата и потвърди операцията с временен код.
                  </p>
                </div>
                <button className="secondary-button" onClick={closeDepositModal} type="button">
                  Затвори
                </button>
              </div>

              <form className="auth-form" onSubmit={handleDepositSubmit}>
                <div className="inline-form-grid">
                  <label className="field-group">
                    <span>Сума</span>
                    <input
                      type="number"
                      min="0.01"
                      step="0.01"
                      value={depositAmount}
                      onChange={(event) => setDepositAmount(event.target.value)}
                      placeholder="100.00"
                    />
                  </label>

                  <label className="field-group">
                    <span className="inline-label-with-info">
                      <span>Временен код</span>
                        <span className="info-tooltip-badge" tabIndex={0}>
                          i
                          <span className="info-tooltip-content">
                            Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                          </span>
                        </span>
                      </span>
                    <input
                      type="text"
                      value={depositTotpCode}
                      onChange={(event) => setDepositTotpCode(event.target.value)}
                      inputMode="numeric"
                      placeholder="123456"
                    />
                  </label>
                </div>

                {depositErrorMessage && <div className="message-box message-box--error">{depositErrorMessage}</div>}

                <div className="inline-action-row">
                  <button className="primary-button" type="submit" disabled={isDepositing}>
                    {isDepositing ? 'Депозиране...' : 'Потвърди депозита'}
                  </button>
                  <button className="secondary-button" type="button" onClick={closeDepositModal} disabled={isDepositing}>
                    Отказ
                  </button>
                </div>
              </form>
            </article>
          </div>
        )}

        <div className="dashboard-overview-grid">
          {isAdmin ? (
            <article className="dashboard-card dashboard-balance-profile-card">
              <div className="card-header-inline">
                <h2>Административен преглед</h2>
              </div>

              <div className="history-summary-grid">
                <div className="history-summary-card">
                  <span className="history-summary-label">Брой служители support</span>
                  <strong>{isLoading ? 'Зареждане...' : adminOverview.supportCount}</strong>
                  <span className="history-summary-meta">Активни служебни акаунти в системата</span>
                </div>
                <div className="history-summary-card">
                  <span className="history-summary-label">Брой потребителски акаунти</span>
                  <strong>{isLoading ? 'Зареждане...' : adminOverview.userCount}</strong>
                  <span className="history-summary-meta">Регистрирани потребителски акаунти</span>
                </div>
              </div>

              <dl>
                <div>
                  <dt>Потребителско име</dt>
                  <dd>{wallet?.username ?? session?.username}</dd>
                </div>
                <div>
                  <dt>Имейл</dt>
                  <dd>{wallet?.email ?? session?.email}</dd>
                </div>
                <div>
                  <dt>Имейл статус</dt>
                  <dd>
                    <span className={emailVerified ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                      {formatStatus(emailVerified)}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Статус на двуфакторната защита</dt>
                  <dd>
                    <span className={session?.twoFactorEnabled ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                      {session?.twoFactorEnabled ? 'Включена' : 'Изключена'}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Следващо подновяване</dt>
                  <dd>{formattedAccessRefreshAt}</dd>
                </div>
              </dl>

              <p className="field-hint">
                Администраторският акаунт няма финансов баланс и не извършва депозити или преводи.
              </p>

              <div className="inline-action-row">
                <span className="inline-label-with-info">
                  <Link className="secondary-link-button" to="/security/two-factor">
                    Настрой временен код
                  </Link>
                  <span className="info-tooltip-badge" tabIndex={0}>
                    i
                    <span className="info-tooltip-content">
                      Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                    </span>
                  </span>
                </span>
              </div>
            </article>
          ) : (
            <article className="dashboard-card dashboard-balance-profile-card">
              <div className="card-header-inline">
                <h2>Баланс и профил</h2>
                <button
                  className="visibility-toggle-button"
                  type="button"
                  onClick={() => setIsBalanceVisible((current) => !current)}
                >
                  {isBalanceVisible ? 'Скрий сумата' : 'Покажи сумата'}
                </button>
              </div>
              <div className="dashboard-balance-profile-grid">
                <div className="dashboard-balance-column">
                  <div className="dashboard-balance-hero">
                    <span className="dashboard-balance-hero__label">Баланс</span>
                    <strong className="dashboard-balance-hero__amount">
                      {isLoading
                        ? 'Зареждане...'
                        : isBalanceVisible
                          ? `${formattedBalance} ${wallet?.currency ?? ''}`.trim()
                          : hiddenBalance}
                    </strong>
                  </div>

                  <dl className="dashboard-balance-meta">
                    <div>
                      <dt>Валута</dt>
                      <dd>{isLoading ? 'Зареждане...' : wallet?.currency ?? '-'}</dd>
                    </div>
                    <div>
                      <dt>Статус</dt>
                      <dd>{isLoading ? 'Зареждане...' : wallet?.isActive ? 'Активен' : 'Неактивен'}</dd>
                    </div>
                    <div>
                      <dt>Създаден на</dt>
                      <dd>{isLoading ? 'Зареждане...' : formattedCreatedAt || '-'}</dd>
                    </div>
                  </dl>
                </div>

                <dl>
                  <div>
                    <dt>Потребителско име</dt>
                    <dd>{wallet?.username ?? session?.username}</dd>
                  </div>
                  <div>
                    <dt>Имейл</dt>
                    <dd>{wallet?.email ?? session?.email}</dd>
                  </div>
                  <div>
                    <dt>Имейл статус</dt>
                    <dd>
                      <span className={emailVerified ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                        {formatStatus(emailVerified)}
                      </span>
                    </dd>
                  </div>
                  <div>
                    <dt>Статус на двуфакторната защита</dt>
                    <dd>
                      <span className={session?.twoFactorEnabled ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                        {session?.twoFactorEnabled ? 'Включена' : 'Изключена'}
                      </span>
                    </dd>
                  </div>
                  <div>
                    <dt>Следващо подновяване</dt>
                    <dd>{formattedAccessRefreshAt}</dd>
                  </div>
                </dl>
              </div>

              <p className="field-hint">
                Ако си активен в приложението, достъпът се подновява автоматично и няма да те изхвърли на всеки 10 минути.
              </p>

              {isSupport && (
                <p className="field-hint">
                  Акаунтите на служител support стартират със служебни средства от 1000 EUR. Те са предназначени само за нужда от съдействие и възстановяване към потребител при банков казус.
                </p>
              )}

              <div className="inline-action-row">
                <span className="inline-label-with-info">
                  <Link className="secondary-link-button" to="/security/two-factor">
                    Настрой временен код
                  </Link>
                  <span className="info-tooltip-badge" tabIndex={0}>
                    i
                    <span className="info-tooltip-content">
                      Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                    </span>
                  </span>
                </span>
              </div>
            </article>
          )}

          <div className="dashboard-side-stack">
            {isAdmin ? (
              <>
                <article className="dashboard-card dashboard-log-shortcut-card dashboard-log-shortcut-card--critical">
                  <div className="card-header-inline">
                    <h2>Критични сигнали</h2>
                    <div className="inline-action-row">
                      <button
                        className="secondary-button secondary-button--compact"
                        type="button"
                        onClick={loadDashboard}
                        disabled={isLoading}
                      >
                        {isLoading ? 'Обновяване...' : 'Обнови'}
                      </button>
                      <Link className="secondary-link-button secondary-link-button--compact" to="/admin/logs">
                        Всички логове
                      </Link>
                    </div>
                  </div>
                  <p className="field-hint">
                    Тук се показват най-важните предупреждения за грешни пароли, блокировки, captcha проблеми и други рискови действия.
                  </p>

                  {isLoading ? (
                    <p className="field-hint">Зареждане...</p>
                  ) : adminLogSnapshot.criticalLines.length === 0 ? (
                    <p className="field-hint">Няма активни критични сигнали в последните логове.</p>
                  ) : (
                    <>
                      <div className="dashboard-log-list">
                        {pagedCriticalLines.map((line, index) => {
                          const presentation = getAuditLinePresentation(line);

                        return (
                          <div className={presentation.rowClassName} key={`critical-${criticalLogsPage}-${index}-${line}`}>
                            <span className={presentation.badgeClassName}>{presentation.badgeText}</span>
                            <span className="audit-entry__text">{getAuditDisplayText(line)}</span>
                          </div>
                        );
                      })}
                      </div>
                      <div className="inline-action-row">
                        <button
                          className="secondary-button secondary-button--compact"
                          type="button"
                          onClick={() => setCriticalLogsPage((current) => Math.max(1, current - 1))}
                          disabled={criticalLogsPage === 1}
                        >
                          Назад
                        </button>
                        <span className="field-hint">Страница {criticalLogsPage} от {criticalLogsPageCount}</span>
                        <button
                          className="secondary-button secondary-button--compact"
                          type="button"
                          onClick={() => setCriticalLogsPage((current) => Math.min(criticalLogsPageCount, current + 1))}
                          disabled={criticalLogsPage === criticalLogsPageCount}
                        >
                          Напред
                        </button>
                      </div>
                    </>
                  )}
                </article>

                <article className="dashboard-card dashboard-log-shortcut-card">
                  <div className="card-header-inline">
                    <h2>Подозрителни опити и защити</h2>
                    <div className="inline-action-row">
                      <button
                        className="secondary-button secondary-button--compact"
                        type="button"
                        onClick={loadDashboard}
                        disabled={isLoading}
                      >
                        {isLoading ? 'Обновяване...' : 'Обнови'}
                      </button>
                      <Link className="secondary-link-button secondary-link-button--compact" to="/admin/logs">
                        Отвори логовете
                      </Link>
                    </div>
                  </div>
                  <p className="field-hint">
                    Тук остават само защитните събития като грешни пароли, captcha, блокировки,
                    {' '}
                    <span className="inline-label-with-info">
                      <span>временен код</span>
                      <span className="info-tooltip-badge" tabIndex={0}>
                        i
                        <span className="info-tooltip-content">
                          Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                        </span>
                      </span>
                    </span>
                    {' '}
                    и отказани опити.
                  </p>

                  {isLoading ? (
                    <p className="field-hint">Зареждане...</p>
                  ) : adminLogSnapshot.securityLines.length === 0 ? (
                    <p className="field-hint">Все още няма защитни събития за показване.</p>
                  ) : (
                    <>
                      <div className="dashboard-log-list">
                        {pagedSecurityLines.map((line, index) => {
                          const presentation = getAuditLinePresentation(line);

                        return (
                            <div className={presentation.rowClassName} key={`security-${securityLogsPage}-${index}-${line}`}>
                              <span className={presentation.badgeClassName}>{presentation.badgeText}</span>
                              <span className="audit-entry__text">{getAuditDisplayText(line)}</span>
                            </div>
                          );
                        })}
                      </div>
                      <div className="inline-action-row">
                        <button
                          className="secondary-button secondary-button--compact"
                          type="button"
                          onClick={() => setSecurityLogsPage((current) => Math.max(1, current - 1))}
                          disabled={securityLogsPage === 1}
                        >
                          Назад
                        </button>
                        <span className="field-hint">Страница {securityLogsPage} от {securityLogsPageCount}</span>
                        <button
                          className="secondary-button secondary-button--compact"
                          type="button"
                          onClick={() => setSecurityLogsPage((current) => Math.min(securityLogsPageCount, current + 1))}
                          disabled={securityLogsPage === securityLogsPageCount}
                        >
                          Напред
                        </button>
                      </div>
                    </>
                  )}
                </article>
              </>
            ) : (
              <>
                <article className="dashboard-card">
                  <h2>Последни 5 транзакции</h2>

                  {isLoading ? (
                    <p className="field-hint">Зареждане...</p>
                  ) : recentTransactions.length === 0 ? (
                    <p className="field-hint">Все още няма транзакции.</p>
                  ) : (
                    <div className="transaction-list">
                      {recentTransactions.map((transaction) => (
                        <div className="transaction-row" key={transaction.transactionId}>
                          <div className="transaction-main">
                            <strong>{getTransactionTitle(transaction)}</strong>
                            <span>{transaction.description || 'Без коментар'}</span>
                          </div>
                          <div className="transaction-side">
                            <span className={transaction.isIncoming ? 'transaction-amount transaction-amount--incoming' : 'transaction-amount transaction-amount--outgoing'}>
                              {formatAmount(transaction)}
                            </span>
                            <span>{formatDate(transaction.createdAtUtc)}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </article>

                <article className="dashboard-card dashboard-analysis-card">
                  <div className="card-header-inline">
                    <h2>Месечен анализ</h2>
                    <label className="field-group">
                      <span>Месец</span>
                      <select value={selectedAnalysisPeriodKey} onChange={handleAnalysisPeriodChange}>
                        {analysisMonthOptions.map((option) => (
                          <option key={option.key} value={option.key}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>
                  <p className="field-hint">Избери месец, за да видиш справката, подготвена от backend-а.</p>

                  <div className="dashboard-summary-grid">
                    <div className="dashboard-summary-tile">
                      <span className="dashboard-summary-label">Общо входящи</span>
                      <strong>{formatCurrencyAmount(analysisSummary.incomingTotal, analysisSummary.currency)}</strong>
                      <span className="dashboard-summary-meta">Брой: {analysisSummary.incomingCount}</span>
                    </div>

                    <div className="dashboard-summary-tile">
                      <span className="dashboard-summary-label">Общо изходящи</span>
                      <strong>{formatCurrencyAmount(analysisSummary.outgoingTotal, analysisSummary.currency)}</strong>
                      <span className="dashboard-summary-meta">Брой: {analysisSummary.outgoingCount}</span>
                    </div>

                    <div className="dashboard-summary-tile">
                      <span className="dashboard-summary-label">Общо депозити</span>
                      <strong>{formatCurrencyAmount(analysisSummary.depositTotal, analysisSummary.currency)}</strong>
                      <span className="dashboard-summary-meta">Брой: {analysisSummary.depositCount}</span>
                    </div>
                  </div>
                </article>
              </>
            )}
          </div>
        </div>
      </section>
    </main>
  );
}

