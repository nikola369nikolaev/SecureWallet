import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getTransactionHistoryPage } from '../../api/transactionApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

const historyFilterOptions = [
  { value: 'All', label: 'Всички' },
  { value: 'Incoming', label: 'Входящи' },
  { value: 'Outgoing', label: 'Изходящи' },
  { value: 'Deposits', label: 'Депозити' },
];

const dateRangeOptions = [
  { value: 'All', label: 'Целият период' },
  { value: 'Today', label: 'Днес' },
  { value: 'Last7Days', label: 'Последни 7 дни' },
  { value: 'Last30Days', label: 'Последни 30 дни' },
];

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

function isDepositTransaction(transaction) {
  return transaction.isIncoming && transaction.counterpartyUsername === 'SecureWallet';
}

function getTransactionTitle(transaction) {
  if (isDepositTransaction(transaction)) {
    return 'Депозит в портфейла';
  }

  return transaction.isIncoming
    ? `Получени от ${transaction.counterpartyUsername}`
    : `Изпратени към ${transaction.counterpartyUsername}`;
}

export function TransactionHistoryPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [transactions, setTransactions] = useState([]);
  const [summary, setSummary] = useState({
    incomingCount: 0,
    incomingTotal: 0,
    outgoingCount: 0,
    outgoingTotal: 0,
    depositCount: 0,
    depositTotal: 0,
    currency: 'EUR',
  });
  const [activeFilter, setActiveFilter] = useState('All');
  const [activeDateRange, setActiveDateRange] = useState('All');
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  async function loadHistoryPage(pageToLoad, shouldAppend) {
    if (!session?.accessToken) {
      setIsLoading(false);
      return;
    }

    if (pageToLoad === 1) {
      setIsLoading(true);
    } else {
      setIsLoadingMore(true);
    }

    setErrorMessage('');

    try {
      const result = await getTransactionHistoryPage({
        type: activeFilter,
        dateRange: activeDateRange,
        searchTerm,
        page: pageToLoad,
        pageSize: 10,
      }, session.accessToken);

      setTransactions((currentTransactions) =>
        shouldAppend ? [...currentTransactions, ...result.items] : result.items);
      setSummary(result.summary);
      setCurrentPage(result.page);
      setTotalCount(result.totalCount);
      setHasMore(result.hasMore);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        navigate('/login', {
          replace: true,
          state: { sessionExpired: true },
        });
        return;
      }

      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на историята.');
    } finally {
      setIsLoading(false);
      setIsLoadingMore(false);
    }
  }

  useEffect(() => {
    let isCancelled = false;
    const timeoutId = window.setTimeout(async () => {
      if (isCancelled) {
        return;
      }

      await loadHistoryPage(1, false);
    }, 250);

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [activeDateRange, activeFilter, navigate, logout, searchTerm, session?.accessToken]);

  const historyTitle = useMemo(() => {
    if (isLoading) {
      return 'Зареждане на историята...';
    }

    if (transactions.length === 0) {
      return 'Няма операции за избрания филтър.';
    }

    return `Показани ${transactions.length} от ${totalCount} операции.`;
  }, [isLoading, totalCount, transactions.length]);

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">История</p>
            <h1>История на транзакциите</h1>
            <p className="dashboard-copy">{historyTitle}</p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        <div className="inline-action-row">
          <Link className="secondary-link-button" to="/transfers">
            Нов превод
          </Link>
          <Link className="secondary-link-button" to="/settings">
            Още
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <article className="dashboard-card dashboard-card--full">
          <div className="history-summary-grid">
            <div className="history-summary-card">
              <span className="history-summary-label">Входящи</span>
              <strong>{formatCurrencyAmount(summary.incomingTotal, summary.currency)}</strong>
              <span className="history-summary-meta">Брой: {summary.incomingCount}</span>
            </div>
            <div className="history-summary-card">
              <span className="history-summary-label">Изходящи</span>
              <strong>{formatCurrencyAmount(summary.outgoingTotal, summary.currency)}</strong>
              <span className="history-summary-meta">Брой: {summary.outgoingCount}</span>
            </div>
            <div className="history-summary-card">
              <span className="history-summary-label">Депозити</span>
              <strong>{formatCurrencyAmount(summary.depositTotal, summary.currency)}</strong>
              <span className="history-summary-meta">Брой: {summary.depositCount}</span>
            </div>
          </div>

          <div className="card-header-inline">
            <h2>Всички движения</h2>
            <div className="history-controls">
              <label className="history-search-field">
                <span>Търсене по референция или коментар</span>
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  placeholder="Например TRX-20260608 или заплата"
                />
              </label>

              <div className="history-filter-grid">
                {dateRangeOptions.map((option) => (
                  <button
                    key={option.value}
                    className={activeDateRange === option.value ? 'history-filter-button history-filter-button--active' : 'history-filter-button'}
                    type="button"
                    onClick={() => setActiveDateRange(option.value)}
                  >
                    {option.label}
                  </button>
                ))}
              </div>

              <div className="history-filter-grid">
                {historyFilterOptions.map((option) => (
                  <button
                    key={option.value}
                    className={activeFilter === option.value ? 'history-filter-button history-filter-button--active' : 'history-filter-button'}
                    type="button"
                    onClick={() => setActiveFilter(option.value)}
                  >
                    {option.label}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {isLoading ? (
            <p className="field-hint">Зареждане...</p>
          ) : transactions.length === 0 ? (
            <p className="field-hint">Няма изпратени, получени или депозитни операции за този филтър.</p>
          ) : (
            <>
              <div className="transaction-list">
                {transactions.map((transaction) => (
                  <div className="transaction-row" key={transaction.transactionId}>
                    <div className="transaction-main">
                      <strong>{getTransactionTitle(transaction)}</strong>
                      <span>{transaction.description || 'Без коментар'}</span>
                      <span>Референция: {transaction.reference}</span>
                    </div>
                    <div className="transaction-side">
                      <span className={transaction.isIncoming ? 'transaction-amount transaction-amount--incoming' : 'transaction-amount transaction-amount--outgoing'}>
                        {formatAmount(transaction)}
                      </span>
                      <span>{formatDate(transaction.createdAtUtc)}</span>
                      <span>{transaction.status}</span>
                    </div>
                  </div>
                ))}
              </div>

              {hasMore && (
                <div className="history-load-more-row">
                  <button
                    className="secondary-button"
                    type="button"
                    onClick={() => loadHistoryPage(currentPage + 1, true)}
                    disabled={isLoadingMore}
                  >
                    {isLoadingMore ? 'Зареждане...' : 'Зареди още'}
                  </button>
                </div>
              )}
            </>
          )}
        </article>
      </section>
    </main>
  );
}
