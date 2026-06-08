import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getAdminUserDetails, getAdminUserTransactions } from '../../api/adminApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

function formatDate(value) {
  return new Intl.DateTimeFormat('bg-BG', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function formatMoney(amount, currency) {
  const formattedAmount = new Intl.NumberFormat('bg-BG', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);

  return `${formattedAmount} ${currency}`;
}

function formatTransactionAmount(transaction) {
  const prefix = transaction.isIncoming ? '+' : '-';
  const amount = new Intl.NumberFormat('bg-BG', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(transaction.amount);

  return `${prefix}${amount} ${transaction.currency}`;
}

export function AdminUserDetailsPage() {
  const { userId } = useParams();
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [userDetails, setUserDetails] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    let isActive = true;

    async function loadData() {
      if (!session?.accessToken || !userId) {
        if (isActive) {
          setIsLoading(false);
        }
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const [detailsResult, transactionsResult] = await Promise.all([
          getAdminUserDetails(userId, session.accessToken),
          getAdminUserTransactions(userId, session.accessToken),
        ]);

        if (!isActive) {
          return;
        }

        setUserDetails(detailsResult);
        setTransactions(transactionsResult);
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          logout();
          navigate('/login', {
            replace: true,
            state: { sessionExpired: true },
          });
          return;
        }

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на потребителските детайли.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadData();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken, userId]);

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Потребителски детайли</p>
            <h1>{isLoading ? 'Зареждане...' : `Акаунт: ${userDetails?.username ?? ''}`}</h1>
            <p className="dashboard-copy">
              Тук {session?.role === 'Admin' ? 'admin' : 'support'} ролята може да види данните на потребителя, wallet статуса и историята на транзакциите.
            </p>
          </div>
          <Link className="secondary-link-button" to="/admin/users">
            Назад към списъка
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <div className="dashboard-grid">
          <article className="dashboard-card">
            <h2>Профил</h2>
            <dl>
              <div>
                <dt>Потребителско име</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.username ?? '-'}</dd>
              </div>
              <div>
                <dt>Имейл</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.email ?? '-'}</dd>
              </div>
              <div>
                <dt>Собствено име</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.firstName || '-'}</dd>
              </div>
              <div>
                <dt>Фамилия</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.lastName || '-'}</dd>
              </div>
              <div>
                <dt>Телефон</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.phoneNumber || '-'}</dd>
              </div>
              <div>
                <dt>Роля</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.role ?? '-'}</dd>
              </div>
              <div>
                <dt>Създаден на</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.createdAtUtc ? formatDate(userDetails.createdAtUtc) : '-'}</dd>
              </div>
            </dl>
          </article>

          <article className="dashboard-card">
            <h2>Сигурност и wallet</h2>
            <dl>
              <div>
                <dt>Имейл статус</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.isEmailVerified ? 'Потвърден' : 'Непотвърден'}</dd>
              </div>
              <div>
                <dt>2FA статус</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.twoFactorEnabled ? 'Включена' : 'Изключена'}</dd>
              </div>
              <div>
                <dt>Активен акаунт</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.isActive ? 'Да' : 'Не'}</dd>
              </div>
              <div>
                <dt>Wallet статус</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.walletIsActive ? 'Активен' : 'Неактивен'}</dd>
              </div>
              <div>
                <dt>Баланс</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails ? formatMoney(userDetails.walletBalance, userDetails.walletCurrency) : '-'}</dd>
              </div>
              <div>
                <dt>Wallet създаден на</dt>
                <dd>{isLoading ? 'Зареждане...' : userDetails?.walletCreatedAtUtc ? formatDate(userDetails.walletCreatedAtUtc) : '-'}</dd>
              </div>
            </dl>
          </article>

          <article className="dashboard-card dashboard-card--full">
            <h2>История на транзакциите</h2>

            {isLoading ? (
              <p className="field-hint">Зареждане...</p>
            ) : !transactions.length ? (
              <p className="field-hint">Този потребител все още няма транзакции.</p>
            ) : (
              <div className="transaction-list">
                {transactions.map((transaction) => (
                  <div className="transaction-row" key={transaction.transactionId}>
                    <div className="transaction-main">
                      <strong>
                        {transaction.isIncoming
                          ? `Получени от ${transaction.counterpartyUsername}`
                          : `Изпратени към ${transaction.counterpartyUsername}`}
                      </strong>
                      <span>{transaction.description || 'Без коментар'}</span>
                      <span>Статус: {transaction.status}</span>
                    </div>
                    <div className="transaction-side">
                      <span className={transaction.isIncoming ? 'transaction-amount transaction-amount--incoming' : 'transaction-amount transaction-amount--outgoing'}>
                        {formatTransactionAmount(transaction)}
                      </span>
                      <span>{formatDate(transaction.createdAtUtc)}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </article>
        </div>
      </section>
    </main>
  );
}
