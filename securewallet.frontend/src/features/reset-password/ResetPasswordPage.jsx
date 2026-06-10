import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { requestPasswordResetCode, verifyPasswordResetCode } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';

const RESET_SESSION_STORAGE_KEY = 'securewallet.auth.passwordReset';

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [formState, setFormState] = useState({
    email: '',
    phoneNumber: '',
    code: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [canEnterCode, setCanEnterCode] = useState(false);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [isVerifyingCode, setIsVerifyingCode] = useState(false);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSendCode(event) {
    event.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');
    setIsSendingCode(true);

    try {
      const result = await requestPasswordResetCode({
        email: formState.email,
        phoneNumber: formState.phoneNumber,
      });

      setCanEnterCode(Boolean(result.canEnterCode));
      setSuccessMessage(result.message ?? 'SMS кодът беше изпратен успешно.');
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна проблем при изпращане на SMS кода. Опитай отново.');
      }
    } finally {
      setIsSendingCode(false);
    }
  }

  async function handleVerifyCode(event) {
    event.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');
    setIsVerifyingCode(true);

    try {
      const result = await verifyPasswordResetCode({
        email: formState.email,
        phoneNumber: formState.phoneNumber,
        code: formState.code,
      });

      window.sessionStorage.setItem(
        RESET_SESSION_STORAGE_KEY,
        JSON.stringify({
          resetSessionToken: result.resetSessionToken,
          email: result.email,
        }),
      );

      navigate('/reset-password/confirm', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна проблем при проверка на SMS кода. Опитай отново.');
      }
    } finally {
      setIsVerifyingCode(false);
    }
  }

  return (
    <main className="auth-page auth-page--register">
      <section className="hero-panel hero-panel--warm">
        <p className="eyebrow">SecureWallet</p>
        <h1>Върни достъпа до профила си чрез телефон и SMS код.</h1>
        <p className="hero-copy">
          За да смениш паролата, първо трябва да докажеш, че имейлът и телефонът принадлежат на един и същ акаунт.
          След това ще получиш SMS код и чак тогава ще преминеш към новата парола.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>Проверка на акаунт</strong>
            <span>Код изпращаме само ако имейлът съществува и телефонът е свързан точно с него.</span>
          </div>
          <div className="hero-note">
            <strong>Защита на кода</strong>
            <span>При 3 грешни опита с reset кода процесът се блокира за 15 минути.</span>
          </div>
        </div>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Забравена парола</p>
          <h2>Потвърди акаунта си</h2>
          <p>Въведи имейл и телефонен номер в +359 формат, след което поискай SMS код. Имаш максимум 3 грешни опита за кода.</p>
        </div>

        <form className="auth-form" onSubmit={handleSendCode}>
          <label className="field-group">
            <span>Имейл</span>
            <input
              type="email"
              value={formState.email}
              onChange={(event) => updateField('email', event.target.value)}
              placeholder="nikola@example.com"
              autoComplete="email"
            />
          </label>

          <label className="field-group">
            <span>Телефонен номер</span>
            <input
              type="text"
              value={formState.phoneNumber}
              onChange={(event) => updateField('phoneNumber', event.target.value)}
              placeholder="+359888123456"
              autoComplete="tel"
            />
          </label>

          <button className="secondary-button" type="submit" disabled={isSendingCode}>
            {isSendingCode ? 'Изпращане...' : 'Изпрати SMS'}
          </button>
        </form>

        <form className="auth-form" onSubmit={handleVerifyCode}>
          <label className="field-group">
            <span className="inline-label-with-info">
              <span>Код от SMS</span>
              <span className="info-tooltip-badge" tabIndex={0} aria-label="Подсказка за кода">
                i
                <span className="info-tooltip-content">
                  Тук въведи еднократния код, който получи по SMS. Това не е код от Microsoft/Google Authenticator приложението.
                </span>
              </span>
            </span>
            <input
              type="text"
              value={formState.code}
              onChange={(event) => updateField('code', event.target.value)}
              placeholder="Въведи получения код"
              disabled={!canEnterCode}
            />
          </label>

          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
          {successMessage && <div className="message-box message-box--success">{successMessage}</div>}

          <button className="primary-button" type="submit" disabled={!canEnterCode || isVerifyingCode}>
            {isVerifyingCode ? 'Проверка...' : 'Потвърди кода'}
          </button>
        </form>

        <div className="panel-footer panel-footer--split">
          <Link to="/login">Назад към вход</Link>
          <Link to="/register">Създай нов акаунт</Link>
        </div>
      </section>
    </main>
  );
}
