import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAdminTransactionHistoryPage } from '../../api/adminApi';
import { getTransactionHistoryPage } from '../../api/transactionApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

const userHistoryFilterOptions = [
  { value: 'All', label: 'Всички' },
  { value: 'Incoming', label: 'Входящи' },
  { value: 'Outgoing', label: 'Изходящи' },
  { value: 'Deposits', label: 'Депозити' },
];

const adminHistoryFilterOptions = [
  { value: 'All', label: 'Всички' },
  { value: 'Transfers', label: 'Преводи' },
  { value: 'Deposits', label: 'Депозити' },
];

const dateRangeOptions = [
  { value: 'All', label: 'Целият период' },
  { value: 'Today', label: 'Днес' },
  { value: 'Last7Days', label: 'Последни 7 дни' },
  { value: 'Last30Days', label: 'Последни 30 дни' },
];

function formatSignedAmount(transaction) {
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

function getUserTransactionTitle(transaction) {
  if (isDepositTransaction(transaction)) {
    return 'Депозит в портфейла';
  }

  return transaction.isIncoming
    ? `Получени от ${transaction.counterpartyUsername}`
    : `Изпратени към ${transaction.counterpartyUsername}`;
}

function getAdminTransactionTitle(transaction) {
  if (transaction.kind === 'Deposit') {
    return `Депозит към ${transaction.receiverUsername}`;
  }

  return `${transaction.senderUsername} превод към ${transaction.receiverUsername}`;
}

export function TransactionHistoryPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = session?.role === 'Admin';
  const canCreateTransfers = !isAdmin;
  const historyFilterOptions = isAdmin ? adminHistoryFilterOptions : userHistoryFilterOptions;

  const [transactions, setTransactions] = useState([]);
  const [summary, setSummary] = useState({
    incomingCount: 0,
    incomingTotal: 0,
    outgoingCount: 0,
    outgoingTotal: 0,
    depositCount: 0,
    depositTotal: 0,
    currency: 'EUR',
    transferCount: 0,
    transferTotal: 0,
    operationCount: 0,
    visibleUserCount: 0,
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

  async function loadUserHistoryPage(pageToLoad, shouldAppend) {
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
      const result = await getTransactionHistoryPage(
        {
          type: activeFilter,
          dateRange: activeDateRange,
          searchTerm,
          page: pageToLoad,
          pageSize: 10,
        },
        session.accessToken,
      );

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

      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна проблем при зареждане на историята. Опитай отново.');
    } finally {
      setIsLoading(false);
      setIsLoadingMore(false);
    }
  }

  async function loadAdminHistory() {
    if (!session?.accessToken) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      const result = await getAdminTransactionHistoryPage(
        {
          type: activeFilter,
          dateRange: activeDateRange,
          searchTerm,
          page: currentPage,
          pageSize: 10,
        },
        session.accessToken,
      );

      setTransactions((currentTransactions) =>
        currentPage > 1 ? [...currentTransactions, ...result.items] : result.items);
      setSummary(result.summary);
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

      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна проблем при зареждане на административната история. Опитай отново.');
    } finally {
      setIsLoading(false);
      setIsLoadingMore(false);
    }
  }

  useEffect(() => {
    setCurrentPage(1);
    setTransactions([]);
  }, [activeDateRange, activeFilter, searchTerm, isAdmin]);

  useEffect(() => {
    if (!isAdmin) {
      return undefined;
    }

    let isCancelled = false;

    const loadData = async () => {
      if (!isCancelled) {
        await loadAdminHistory();
      }
    };

    loadData();

    return () => {
      isCancelled = true;
    };
  }, [activeDateRange, activeFilter, currentPage, isAdmin, navigate, logout, searchTerm, session?.accessToken]);

  useEffect(() => {
    if (isAdmin) {
      return undefined;
    }

    let isCancelled = false;
    const timeoutId = window.setTimeout(async () => {
      if (!isCancelled) {
        await loadUserHistoryPage(1, false);
      }
    }, 250);

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [activeDateRange, activeFilter, isAdmin, navigate, logout, searchTerm, session?.accessToken]);

  const transactionsForRender = transactions;
  const totalCountForRender = totalCount;
  const hasMoreForRender = hasMore;

  const historyTitle = useMemo(() => {
    if (isLoading) {
      return 'Зареждане на историята...';
    }

    if (transactionsForRender.length === 0) {
      return isAdmin
        ? 'Няма операции за избраните филтри при потребителските и support акаунтите.'
        : 'Няма операции за избрания филтър.';
    }

    return isAdmin
      ? `Показани ${transactionsForRender.length} от ${totalCountForRender} операции на потребителските и support акаунтите.`
      : `Показани ${transactionsForRender.length} от ${totalCountForRender} операции.`;
  }, [isAdmin, isLoading, totalCountForRender, transactionsForRender.length]);

  function handleLoadMore() {
    if (isAdmin) {
      setIsLoadingMore(true);
      setCurrentPage((currentPageValue) => currentPageValue + 1);
      return;
    }

    loadUserHistoryPage(currentPage + 1, true);
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">История</p>
            <h1>{isAdmin ? 'Обща история на транзакциите' : 'История на транзакциите'}</h1>
            <p className="dashboard-copy">{historyTitle}</p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        <div className="inline-action-row">
          {canCreateTransfers && (
            <Link className="secondary-link-button" to="/transfers">
              Нов превод
            </Link>
          )}
          <Link className="secondary-link-button" to="/settings">
            Още
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <article className="dashboard-card dashboard-card--full">
          {isAdmin ? (
            <div className="history-summary-grid">
              <div className="history-summary-card">
                <span className="history-summary-label">Преводи между акаунти</span>
                <strong>{formatCurrencyAmount(summary.transferTotal, summary.currency)}</strong>
                <span className="history-summary-meta">Брой: {summary.transferCount}</span>
              </div>
              <div className="history-summary-card">
                <span className="history-summary-label">Депозити към акаунти</span>
                <strong>{formatCurrencyAmount(summary.depositTotal, summary.currency)}</strong>
                <span className="history-summary-meta">Брой: {summary.depositCount}</span>
              </div>
              <div className="history-summary-card">
                <span className="history-summary-label">Наблюдавани акаунти</span>
                <strong>{summary.visibleUserCount}</strong>
                <span className="history-summary-meta">Потребителски и support</span>
              </div>
            </div>
          ) : (
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
          )}

          <div className="card-header-inline">
            <h2>{isAdmin ? 'Всички операции на потребителските и support акаунтите' : 'Всички движения'}</h2>
            <div className="history-controls">
              <label className="history-search-field">
                <span>{isAdmin ? 'Търсене по потребител, референция или коментар' : 'Търсене по референция или коментар'}</span>
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  placeholder={isAdmin ? 'Например ali, TRX-20260608 или refund' : 'Например TRX-20260608 или заплата'}
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
          ) : transactionsForRender.length === 0 ? (
            <p className="field-hint">
              {isAdmin
                ? 'Няма операции за потребителските и support акаунтите при избраните филтри.'
                : 'Няма изпратени, получени или депозитни операции за този филтър.'}
            </p>
          ) : (
            <>
              <div className="transaction-list">
                {transactionsForRender.map((transaction) => (
                  <div className="transaction-row" key={transaction.transactionId}>
                    <div className="transaction-main">
                      <strong>{isAdmin ? getAdminTransactionTitle(transaction) : getUserTransactionTitle(transaction)}</strong>
                      <span>{transaction.description || 'Без коментар'}</span>
                      <span>Референция: {transaction.reference}</span>
                      {isAdmin && (
                        <span>
                          Тип: {transaction.kind === 'Deposit' ? 'Депозит' : 'Превод между акаунти'}
                        </span>
                      )}
                    </div>
                    <div className="transaction-side">
                      <span
                        className={
                          isAdmin
                            ? 'transaction-amount'
                            : transaction.isIncoming
                              ? 'transaction-amount transaction-amount--incoming'
                              : 'transaction-amount transaction-amount--outgoing'
                        }
                      >
                        {isAdmin
                          ? formatCurrencyAmount(transaction.amount, transaction.currency)
                          : formatSignedAmount(transaction)}
                      </span>
                      <span>{formatDate(transaction.createdAtUtc)}</span>
                      <span>{transaction.status}</span>
                    </div>
                  </div>
                ))}
              </div>

              {hasMoreForRender && (
                <div className="history-load-more-row">
                  <button
                    className="secondary-button"
                    type="button"
                    onClick={handleLoadMore}
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
