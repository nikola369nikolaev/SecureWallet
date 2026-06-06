import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCurrentWallet } from '../../api/walletApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

function formatDateTime(value) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('bg-BG', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export function SettingsPage() {
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на настройките.');
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

  const emailVerified = wallet?.isEmailVerified ?? session?.isEmailVerified ?? false;

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Още / Настройки</p>
            <h1>Профил и сигурност</h1>
            <p className="dashboard-copy">
              Тук събираме статусите, които са важни за сигурността на акаунта и се вземат от backend-а.
            </p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <div className="dashboard-grid">
          <article className="dashboard-card">
            <h2>Акаунт</h2>
            <dl>
              <div>
                <dt>Потребителско име</dt>
                <dd>{isLoading ? 'Зареждане...' : wallet?.username ?? session?.username ?? '-'}</dd>
              </div>
              <div>
                <dt>Имейл</dt>
                <dd>{isLoading ? 'Зареждане...' : wallet?.email ?? session?.email ?? '-'}</dd>
              </div>
              <div>
                <dt>Имейл потвърждение</dt>
                <dd>
                  <span className={emailVerified ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                    {emailVerified ? 'Потвърден' : 'Непотвърден'}
                  </span>
                </dd>
              </div>
            </dl>
          </article>

          <article className="dashboard-card">
            <h2>Сигурност</h2>
            <dl>
              <div>
                <dt>2FA статус</dt>
                <dd>
                  <span className={session?.twoFactorEnabled ? 'status-pill status-pill--success' : 'status-pill status-pill--pending'}>
                    {session?.twoFactorEnabled ? 'Включена' : 'Изключена'}
                  </span>
                </dd>
              </div>
              <div>
                <dt>Сесия до</dt>
                <dd>{formatDateTime(session?.expiresAtUtc)}</dd>
              </div>
              <div>
                <dt>Портфейл създаден на</dt>
                <dd>{isLoading ? 'Зареждане...' : formatDateTime(wallet?.createdAtUtc)}</dd>
              </div>
            </dl>

            <div className="inline-action-row">
              <Link className="secondary-link-button" to="/security/two-factor">
                Управлявай TOTP
              </Link>
            </div>
          </article>
        </div>
      </section>
    </main>
  );
}
