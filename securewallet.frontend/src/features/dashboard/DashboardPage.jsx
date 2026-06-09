import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
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

  const loadDashboard = useCallback(async () => {
    if (!session?.accessToken) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
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
  }, [logout, navigate, selectedAnalysisPeriod.month, selectedAnalysisPeriod.year, session?.accessToken]);

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
          <button className="secondary-button" onClick={openDepositModal} type="button">
            Депозирай пари
          </button>
          <Link className="secondary-link-button" to="/transfers">
            Преводи
          </Link>
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
              Създай support
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

        {isDepositOpen && (
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
                          Отвори Google Authenticator, Microsoft Authenticator или друго подобно приложение.
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
                  <dt>2FA статус</dt>
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

            <div className="inline-action-row">
              <Link className="secondary-link-button" to="/security/two-factor">
                Настрой TOTP
              </Link>
            </div>
          </article>

          <div className="dashboard-side-stack">
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
          </div>
        </div>
      </section>
    </main>
  );
}
