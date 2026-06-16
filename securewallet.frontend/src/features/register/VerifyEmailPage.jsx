import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { resendEmailVerificationCode, verifyEmailCode } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';
import { createSessionFromAuthResult } from '../../auth/sessionStorage';
import { AppBrand } from '../../components/AppBrand';

const RESEND_COOLDOWN_SECONDS = 15;

export function VerifyEmailPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [email] = useState(location.state?.email ?? '');
  const [code, setCode] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [infoMessage, setInfoMessage] = useState(
    location.state?.message ??
      'Провери входящата си поща и въведи 6-цифрения код, за да продължиш.',
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [cooldownSeconds, setCooldownSeconds] = useState(location.state?.message ? RESEND_COOLDOWN_SECONDS : 0);

  useEffect(() => {
    if (!location.state?.email) {
      navigate('/register', { replace: true });
      return;
    }

    if (cooldownSeconds <= 0) {
      return undefined;
    }

    const intervalId = window.setInterval(() => {
      setCooldownSeconds((current) => (current > 0 ? current - 1 : 0));
    }, 1000);

    return () => window.clearInterval(intervalId);
  }, [cooldownSeconds, location.state, navigate]);

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage('');
    setIsSubmitting(true);

    try {
      const result = await verifyEmailCode({
        email,
        code,
      });

      setSession(createSessionFromAuthResult(result));
      navigate('/security/two-factor', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна проблем при потвърждаване на имейла. Опитай отново.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResend() {
    setErrorMessage('');
    setIsResending(true);

    try {
      const result = await resendEmailVerificationCode({ email });
      setInfoMessage(`${result.message} Кодът е валиден до ${new Date(result.expiresAtUtc).toLocaleString('bg-BG')}.`);
      setCode('');
      setCooldownSeconds(RESEND_COOLDOWN_SECONDS);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна проблем при изпращане на нов код. Опитай отново.');
      }
    } finally {
      setIsResending(false);
    }
  }

  return (
    <main className="auth-page auth-page--register">
      <section className="hero-panel hero-panel--warm">
        <AppBrand subtitle="Имейл потвърждение" />
        <h1>Потвърди имейла си</h1>
        <p className="hero-copy">
          Изпратихме код за потвърждение. След успешния код ще преминеш към настройката на
          двуфакторната защита.
        </p>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Код от имейл</p>
          <h2>Въведи кода за потвърждение</h2>
          <p>{infoMessage}</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="message-box message-box--info">
            Кодът е изпратен за: <strong>{email}</strong>
          </div>

          <label className="field-group">
            <span>Код</span>
            <input
              type="text"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              inputMode="numeric"
            />
          </label>

          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

          <div className="inline-action-row">
            <button className="primary-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Проверка...' : 'Потвърди имейла'}
            </button>
            <button
              className="secondary-button"
              type="button"
              onClick={handleResend}
              disabled={isResending || cooldownSeconds > 0}
            >
              {isResending
                ? 'Изпращане...'
                : cooldownSeconds > 0
                  ? `Нов код след ${cooldownSeconds} сек.`
                  : 'Изпрати нов код'}
            </button>
          </div>

          {cooldownSeconds > 0 && (
            <p className="field-hint">Можеш да поискаш нов код след {cooldownSeconds} секунди.</p>
          )}
        </form>

        <div className="panel-footer panel-footer--split">
          <span>
            Искаш да влезеш? <Link to="/login">Обратно към вход</Link>
          </span>
          <Link to="/register">Назад към регистрация</Link>
        </div>
      </section>
    </main>
  );
}



