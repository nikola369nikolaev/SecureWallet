import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { beginTotpSetup, disableTotp, resetTotpSetup, verifyTotpSetup } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

export function TwoFactorSetupPage() {
  const navigate = useNavigate();
  const { session, setSession, logout } = useAuth();
  const [setupState, setSetupState] = useState(null);
  const [verifyCode, setVerifyCode] = useState('');
  const [managementCode, setManagementCode] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isVerifying, setIsVerifying] = useState(false);
  const [isDisabling, setIsDisabling] = useState(false);
  const [isResetting, setIsResetting] = useState(false);

  useEffect(() => {
    let isActive = true;

    async function loadSetup() {
      if (!session?.accessToken) {
        navigate('/login', { replace: true });
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const result = await beginTotpSetup(session.accessToken);

        if (!isActive) {
          return;
        }

        setSetupState(result);
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при подготовка на TOTP настройката.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadSetup();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken]);

  async function handleVerify(event) {
    event.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');
    setIsVerifying(true);

    try {
      const result = await verifyTotpSetup({ code: verifyCode }, session.accessToken);

      setSuccessMessage(result.message ?? 'Двуфакторната защита беше включена успешно.');
      setSetupState((current) =>
        current
          ? {
              ...current,
              isAlreadyEnabled: true,
              canShowQrCode: false,
            }
          : current,
      );
      setVerifyCode('');

      setSession({
        ...session,
        twoFactorEnabled: true,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при потвърждаване на TOTP кода.');
      }
    } finally {
      setIsVerifying(false);
    }
  }

  async function handleDisable() {
    setErrorMessage('');
    setSuccessMessage('');
    setIsDisabling(true);

    try {
      const result = await disableTotp({ code: managementCode }, session.accessToken);

      setSession({
        ...session,
        twoFactorEnabled: false,
      });

      setManagementCode('');
      setSuccessMessage(result.message ?? 'Двуфакторната защита беше изключена успешно.');
      navigate('/dashboard', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при изключване на двуфакторната защита.');
      }
    } finally {
      setIsDisabling(false);
    }
  }

  async function handleReset() {
    setErrorMessage('');
    setSuccessMessage('');
    setIsResetting(true);

    try {
      const result = await resetTotpSetup({ code: managementCode }, session.accessToken);

      setSetupState(result);
      setManagementCode('');
      setVerifyCode('');
      setSuccessMessage(result.message ?? 'Подготвен е нов QR код. Потвърди го с код от новото приложение.');
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при подмяна на 2FA настройката.');
      }
    } finally {
      setIsResetting(false);
    }
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Сигурност</p>
            <h1>Двуфакторна защита с TOTP</h1>
            <p className="dashboard-copy">
              TOTP е безплатен алгоритъм за еднократни кодове и работи с Google Authenticator,
              Microsoft Authenticator и други подобни приложения.
            </p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към таблото
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
        {successMessage && <div className="message-box message-box--success">{successMessage}</div>}

        {isLoading && <div className="dashboard-card">Зареждане на TOTP настройката...</div>}

        {!isLoading && setupState?.isAlreadyEnabled && (
          <article className="dashboard-card totp-card">
            <h2>2FA вече е включена</h2>
            <p className="dashboard-copy">
              Въведи текущия код от authenticator приложението, ако искаш да изключиш 2FA или да подготвиш
              нов QR код за същия акаунт.
            </p>

            <label className="field-group">
              <span>Текущ authenticator код</span>
              <input
                type="text"
                value={managementCode}
                onChange={(event) => setManagementCode(event.target.value)}
                placeholder="123456"
                inputMode="numeric"
              />
            </label>

            <div className="inline-action-row">
              <button className="secondary-button" type="button" onClick={handleDisable} disabled={isDisabling || isResetting}>
                {isDisabling ? 'Изключване...' : 'Изключи 2FA'}
              </button>
              <button className="primary-button" type="button" onClick={handleReset} disabled={isDisabling || isResetting}>
                {isResetting ? 'Подготвяне...' : 'Смени устройството / QR кода'}
              </button>
            </div>
          </article>
        )}

        {!isLoading && setupState?.canShowQrCode && (
          <div className="dashboard-grid">
            <article className="dashboard-card totp-card">
              <h2>1. Сканирай QR кода</h2>
              <p className="dashboard-copy">
                Отвори Google Authenticator или Microsoft Authenticator и сканирай QR кода.
              </p>
              <div className="message-box message-box--info">
                QR кодът се генерира локално от нашия backend и не изпраща setup текста към външен QR сайт.
              </div>
              <img className="qr-image" src={setupState.qrCodeImageDataUri} alt="TOTP QR code" />
            </article>

            <article className="dashboard-card totp-card">
              <h2>2. Ръчен ключ</h2>
              <p className="dashboard-copy">
                Ако не искаш да сканираш QR кода, можеш да въведеш този secret ключ ръчно в приложението.
              </p>
              <div className="secret-code-box">{setupState.manualEntryKey}</div>
              <p className="field-hint">Запази го внимателно. Това е тайният ключ за този TOTP setup.</p>
            </article>
          </div>
        )}

        {!isLoading && setupState?.canShowQrCode && (
          <article className="dashboard-card totp-card totp-card--verify">
            <h2>3. Потвърди кода</h2>
            <p className="dashboard-copy">
              След като приложението започне да показва 6-цифрен код, въведи го тук, за да включим
              двуфакторната защита за акаунта.
            </p>

            <form className="auth-form" onSubmit={handleVerify}>
              <label className="field-group">
                <span>Authenticator код</span>
                <input
                  type="text"
                  value={verifyCode}
                  onChange={(event) => setVerifyCode(event.target.value)}
                  placeholder="123456"
                  inputMode="numeric"
                />
              </label>

              <button className="primary-button" type="submit" disabled={isVerifying}>
                {isVerifying ? 'Проверка...' : 'Включи двуфакторната защита'}
              </button>
            </form>
          </article>
        )}
      </section>
    </main>
  );
}
