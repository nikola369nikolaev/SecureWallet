import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCurrentWallet } from '../../api/walletApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

export function DashboardPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [wallet, setWallet] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    let isActive = true;

    async function loadWallet() {
      if (!session?.accessToken) {
        if (isActive) {
          setIsLoading(false);
        }
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const result = await getCurrentWallet(session.accessToken);

        if (!isActive) {
          return;
        }

        setWallet(result);
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на портфейла.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadWallet();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken]);

  const formattedBalance = useMemo(() => {
    if (!wallet) {
      return '';
    }

    return new Intl.NumberFormat('bg-BG', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(wallet.balance);
  }, [wallet]);

  const formattedCreatedAt = useMemo(() => {
    if (!wallet?.createdAtUtc) {
      return '';
    }

    return new Intl.DateTimeFormat('bg-BG', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(wallet.createdAtUtc));
  }, [wallet?.createdAtUtc]);

  const formattedSessionExpiry = useMemo(() => {
    if (!session?.expiresAtUtc) {
      return '-';
    }

    return new Intl.DateTimeFormat('bg-BG', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(session.expiresAtUtc));
  }, [session?.expiresAtUtc]);

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Моят портфейл</p>
            <h1>Добре дошъл, {wallet?.username ?? session?.username}</h1>
            <p className="dashboard-copy">
              Тук вече показваме реални данни от защитен backend endpoint, а не временен debug изглед.
            </p>
          </div>
          <button className="secondary-button" onClick={logout} type="button">
            Изход
          </button>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <div className="dashboard-grid">
          <article className="dashboard-card">
            <h2>Портфейл</h2>
            <dl>
              <div>
                <dt>Баланс</dt>
                <dd>{isLoading ? 'Зареждане...' : `${formattedBalance} ${wallet?.currency ?? ''}`.trim()}</dd>
              </div>
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
          </article>

          <article className="dashboard-card">
            <h2>Потребител</h2>
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
                <dt>Сесия до</dt>
                <dd>{formattedSessionExpiry}</dd>
              </div>
              <div>
                <dt>2FA статус</dt>
                <dd>
                  <span className={session?.twoFactorEnabled ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                    {session?.twoFactorEnabled ? 'Включена' : 'Изключена'}
                  </span>
                </dd>
              </div>
            </dl>

            <div className="inline-action-row">
              <Link className="secondary-link-button" to="/security/two-factor">
                Настрой TOTP
              </Link>
            </div>
          </article>
        </div>
      </section>
    </main>
  );
}
