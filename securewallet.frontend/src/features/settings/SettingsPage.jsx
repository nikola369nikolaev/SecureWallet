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

function formatCardNumber(value) {
  if (!value) {
    return '-';
  }

  return value.replace(/(.{4})/g, '$1 ').trim();
}

function formatIban(value) {
  if (!value) {
    return '-';
  }

  return value.replace(/(.{4})/g, '$1 ').trim();
}

function formatCardExpiry(value) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('en-GB', {
    month: '2-digit',
    year: '2-digit',
  }).format(new Date(value));
}

export function SettingsPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [wallet, setWallet] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [isCardNumberVisible, setIsCardNumberVisible] = useState(false);
  const [isCvvVisible, setIsCvvVisible] = useState(false);

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

  async function copyValue(value, label) {
    if (!value) {
      return;
    }

    setErrorMessage('');

    try {
      await navigator.clipboard.writeText(value);
      setSuccessMessage(`${label} беше копиран успешно.`);
    } catch {
      setSuccessMessage('');
      setErrorMessage(`Не успяхме да копираме ${label.toLowerCase()}.`);
    }
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Още</p>
            <h1>Профил, сигурност и карта</h1>
            <p className="dashboard-copy">
              Тук събираме информацията за акаунта, защитата и картовите детайли, които идват от backend-а.
            </p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
        {successMessage && <div className="message-box message-box--success">{successMessage}</div>}

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

          <article className="dashboard-card dashboard-card--full">
            <h2>Детайли на картата</h2>
            <dl>
              <div>
                <dt>IBAN</dt>
                <dd>
                  {isLoading ? 'Зареждане...' : formatIban(wallet?.iban)}
                  {!isLoading && wallet?.iban && (
                    <button
                      className="secondary-button"
                      onClick={() => copyValue(wallet.iban, 'IBAN')}
                      style={{ marginLeft: '0.75rem' }}
                      type="button"
                    >
                      Копирай
                    </button>
                  )}
                </dd>
              </div>
              <div>
                <dt>Номер на картата</dt>
                <dd>
                  {isLoading
                    ? 'Зареждане...'
                    : isCardNumberVisible
                      ? formatCardNumber(wallet?.cardNumber)
                      : '**** **** **** ****'}
                  {!isLoading && wallet?.cardNumber && (
                    <>
                      <button
                        className="secondary-button"
                        onClick={() => setIsCardNumberVisible((current) => !current)}
                        style={{ marginLeft: '0.75rem' }}
                        type="button"
                      >
                        {isCardNumberVisible ? 'Скрий' : 'Покажи'}
                      </button>
                      <button
                        className="secondary-button"
                        onClick={() => copyValue(wallet.cardNumber, 'Номерът на картата')}
                        style={{ marginLeft: '0.5rem' }}
                        type="button"
                      >
                        Копирай
                      </button>
                    </>
                  )}
                </dd>
              </div>
              <div>
                <dt>Валидна до</dt>
                <dd>{isLoading ? 'Зареждане...' : formatCardExpiry(wallet?.cardExpiresAtUtc)}</dd>
              </div>
              <div>
                <dt>CVV код</dt>
                <dd>
                  {isLoading
                    ? 'Зареждане...'
                    : isCvvVisible
                      ? wallet?.cardCvv ?? '-'
                      : '***'}
                  {!isLoading && wallet?.cardCvv && (
                    <button
                      className="secondary-button"
                      onClick={() => setIsCvvVisible((current) => !current)}
                      style={{ marginLeft: '0.75rem' }}
                      type="button"
                    >
                      {isCvvVisible ? 'Скрий' : 'Покажи'}
                    </button>
                  )}
                </dd>
              </div>
              <div>
                <dt>Картата е създадена на</dt>
                <dd>{isLoading ? 'Зареждане...' : formatDateTime(wallet?.cardCreatedAtUtc)}</dd>
              </div>
            </dl>
          </article>
        </div>
      </section>
    </main>
  );
}
