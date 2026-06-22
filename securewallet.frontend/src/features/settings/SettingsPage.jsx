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

async function tryCopyWithClipboardApi(value) {
  if (!navigator.clipboard || !window.isSecureContext) {
    return false;
  }

  await navigator.clipboard.writeText(value);
  return true;
}

function tryCopyWithTextareaFallback(value) {
  const textArea = document.createElement('textarea');
  textArea.value = value;
  textArea.setAttribute('readonly', '');
  textArea.style.position = 'fixed';
  textArea.style.top = '-9999px';
  textArea.style.left = '-9999px';

  document.body.appendChild(textArea);
  textArea.focus();
  textArea.select();

  try {
    return document.execCommand('copy');
  } finally {
    document.body.removeChild(textArea);
  }
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна проблем при зареждане на настройките. Опитай отново.');
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
  const isTwoFactorEnabled = session?.twoFactorEnabled ?? false;
  const isAdmin = session?.role === 'Admin';

  async function copyValue(value, label) {
    if (!value) {
      return;
    }

    setErrorMessage('');

    try {
      const copiedWithClipboardApi = await tryCopyWithClipboardApi(value)
        .catch(() => false);

      const copied = copiedWithClipboardApi || tryCopyWithTextareaFallback(value);

      if (!copied) {
        throw new Error('Copy failed.');
      }

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
            <h1>{isAdmin ? 'Профил и сигурност' : 'Профил, сигурност и карта'}</h1>
            <p className="dashboard-copy">
              {isAdmin
                ? 'Тук събираме информацията за акаунта и защитата.'
                : 'Тук събираме информацията за акаунта, защитата и картовите детайли.'}
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
                <dt>Статус на двуфакторната защита</dt>
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

            <div className="settings-toggle-card">
              <div>
                <p className="settings-toggle-title">Управлявай временния си код</p>
                <p className="field-hint">
                  Натисни превключвателя, за да отвориш настройката на двуфакторната защита.
                </p>
              </div>
              <Link
                className={`toggle-link-button ${isTwoFactorEnabled ? 'toggle-link-button--on' : 'toggle-link-button--off'}`}
                to="/security/two-factor"
                role="switch"
                aria-checked={isTwoFactorEnabled}
              >
                <span className="toggle-link-button__track">
                  <span className="toggle-link-button__thumb" />
                </span>
                <span className="toggle-link-button__label">{isTwoFactorEnabled ? 'Включено' : 'Изключено'}</span>
              </Link>
            </div>
          </article>

          {!isAdmin && (
            <article className="dashboard-card dashboard-card--full">
              <h2>Детайли на картата</h2>
              <div className="card-details-grid">
                <div className="card-detail-tile">
                  <span className="card-detail-label">IBAN</span>
                  <strong className="card-detail-value card-detail-value--mono">
                    {isLoading ? 'Зареждане...' : formatIban(wallet?.iban)}
                  </strong>
                  {!isLoading && wallet?.iban && (
                    <div className="card-detail-actions">
                      <button
                        className="secondary-button secondary-button--compact"
                        onClick={() => copyValue(wallet.iban, 'IBAN')}
                        type="button"
                      >
                        Копирай
                      </button>
                    </div>
                  )}
                </div>

                <div className="card-detail-tile">
                  <span className="card-detail-label">Номер на картата</span>
                  <strong className="card-detail-value card-detail-value--mono">
                    {isLoading
                      ? 'Зареждане...'
                      : isCardNumberVisible
                        ? formatCardNumber(wallet?.cardNumber)
                        : '**** **** **** ****'}
                  </strong>
                  {!isLoading && wallet?.cardNumber && (
                    <div className="card-detail-actions">
                      <button
                        className="secondary-button secondary-button--compact"
                        onClick={() => setIsCardNumberVisible((current) => !current)}
                        type="button"
                      >
                        {isCardNumberVisible ? 'Скрий' : 'Покажи'}
                      </button>
                      <button
                        className="secondary-button secondary-button--compact"
                        onClick={() => copyValue(wallet.cardNumber, 'Номерът на картата')}
                        type="button"
                      >
                        Копирай
                      </button>
                    </div>
                  )}
                </div>

                <div className="card-detail-tile">
                  <span className="card-detail-label">Валидна до</span>
                  <strong className="card-detail-value">
                    {isLoading ? 'Зареждане...' : formatCardExpiry(wallet?.cardExpiresAtUtc)}
                  </strong>
                </div>

                <div className="card-detail-tile">
                  <span className="card-detail-label">CVV код</span>
                  <strong className="card-detail-value card-detail-value--mono">
                    {isLoading
                      ? 'Зареждане...'
                      : isCvvVisible
                        ? wallet?.cardCvv ?? '-'
                        : '***'}
                  </strong>
                  {!isLoading && wallet?.cardCvv && (
                    <div className="card-detail-actions">
                      <button
                        className="secondary-button secondary-button--compact"
                        onClick={() => setIsCvvVisible((current) => !current)}
                        type="button"
                      >
                        {isCvvVisible ? 'Скрий' : 'Покажи'}
                      </button>
                    </div>
                  )}
                </div>

                <div className="card-detail-tile card-detail-tile--wide">
                  <span className="card-detail-label">Картата е създадена на</span>
                  <strong className="card-detail-value">
                    {isLoading ? 'Зареждане...' : formatDateTime(wallet?.cardCreatedAtUtc)}
                  </strong>
                </div>
              </div>
            </article>
          )}
        </div>
      </section>
    </main>
  );
}
