import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { loginUser, resendEmailVerificationCode, verifyEmailCode } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';
import { createSessionFromAuthResult } from '../../auth/sessionStorage';
import { CaptchaImage } from '../../components/CaptchaImage';

const SESSION_EXPIRED_KEY = 'securewallet.auth.sessionExpired';
const RESEND_COOLDOWN_SECONDS = 15;

export function LoginPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [formState, setFormState] = useState({
    email: location.state?.email ?? '',
    password: '',
    captchaToken: '',
    totpCode: '',
    verificationCode: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [infoMessage, setInfoMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [requiresCaptcha, setRequiresCaptcha] = useState(false);
  const [requiresTotp, setRequiresTotp] = useState(false);
  const [requiresEmailVerification, setRequiresEmailVerification] = useState(false);
  const [captchaImageBase64, setCaptchaImageBase64] = useState(null);
  const [lockoutSeconds, setLockoutSeconds] = useState(null);
  const [cooldownSeconds, setCooldownSeconds] = useState(0);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);

  useEffect(() => {
    const hasExpiredSessionFlag = window.sessionStorage.getItem(SESSION_EXPIRED_KEY) === 'true';

    if (location.state?.sessionExpired || hasExpiredSessionFlag) {
      setErrorMessage('Сесията изтече, моля влез отново.');
      window.sessionStorage.removeItem(SESSION_EXPIRED_KEY);
    }
  }, [location.state]);

  useEffect(() => {
    if (cooldownSeconds <= 0) {
      return undefined;
    }

    const intervalId = window.setInterval(() => {
      setCooldownSeconds((current) => (current > 0 ? current - 1 : 0));
    }, 1000);

    return () => window.clearInterval(intervalId);
  }, [cooldownSeconds]);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessage('');

    if (!requiresEmailVerification) {
      setInfoMessage('');
    }

    try {
      if (requiresEmailVerification) {
        const result = await verifyEmailCode({
          email: formState.email,
          code: formState.verificationCode,
        });

        setSession(createSessionFromAuthResult(result));
        navigate(result.securitySetupRequired ? '/security/two-factor' : '/dashboard', { replace: true });
        return;
      }

      const result = await loginUser({
        email: formState.email,
        password: formState.password,
        captchaToken: requiresCaptcha ? formState.captchaToken : null,
        totpCode: requiresTotp ? formState.totpCode : null,
      });

      setSession(createSessionFromAuthResult(result));
      navigate(result.securitySetupRequired ? '/security/two-factor' : '/dashboard', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        const nextRequiresTotp = Boolean(error.payload?.requiresTotp);
        const nextRequiresCaptcha = Boolean(error.payload?.requiresCaptcha);
        const nextRequiresEmailVerification = Boolean(error.payload?.requiresEmailVerification);
        const nextMessage = error.payload?.message ?? error.message;
        const shouldOpenTotpStep = nextRequiresTotp && !nextRequiresEmailVerification;
        const isTotpStepPrompt = nextRequiresTotp && !formState.totpCode;

        if (nextRequiresEmailVerification) {
          setInfoMessage('Имейлът и паролата са приети. Въведи 6-цифрения код от имейла, за да потвърдиш акаунта. Провери и папка Spam, ако не го виждаш.');
          setErrorMessage('');
        } else if (isTotpStepPrompt) {
          setInfoMessage('Имейлът и паролата са приети. Въведи временния код от Microsoft/Google Authenticator приложението, за да завършиш входа.');
          setErrorMessage('');
        } else {
          setErrorMessage(nextMessage);
        }

        setRequiresEmailVerification(nextRequiresEmailVerification);
        setRequiresCaptcha(nextRequiresEmailVerification || shouldOpenTotpStep ? false : nextRequiresCaptcha);
        setRequiresTotp(nextRequiresEmailVerification ? false : nextRequiresTotp);
        setCaptchaImageBase64(nextRequiresEmailVerification || shouldOpenTotpStep ? null : (error.payload?.captchaImageBase64 ?? null));
        setLockoutSeconds(nextRequiresEmailVerification ? null : (error.payload?.lockoutSeconds ?? null));
        setFormState((current) => ({
          ...current,
          email: error.payload?.email ?? current.email,
          password: nextRequiresEmailVerification || isTotpStepPrompt || nextRequiresTotp ? current.password : '',
          captchaToken: '',
          totpCode: nextRequiresTotp ? '' : current.totpCode,
          verificationCode: nextRequiresEmailVerification ? '' : current.verificationCode,
        }));
      } else {
        setErrorMessage(
          requiresEmailVerification
            ? 'Възникна проблем при потвърждаване на имейла. Опитай отново.'
            : 'Възникна проблем при вход. Опитай отново.',
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResendVerificationCode() {
    setErrorMessage('');
    setIsResending(true);

    try {
      const result = await resendEmailVerificationCode({ email: formState.email });
      setInfoMessage(`${result.message} Кодът е валиден до ${new Date(result.expiresAtUtc).toLocaleString('bg-BG')}. Провери и папка Spam.`);
      setCooldownSeconds(RESEND_COOLDOWN_SECONDS);
      setFormState((current) => ({
        ...current,
        verificationCode: '',
      }));
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
    <main className="auth-page auth-page--login">
      <section className="hero-panel">
        <p className="eyebrow">SecureWallet</p>
        <h1>Влез в своя защитен дигитален портфейл.</h1>
        <p className="hero-copy">
          Тук тестваш входа, captcha защитата, временния код от Microsoft/Google Authenticator приложението
          и поведението на системата при временен lockout след грешни опити.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>Имейл и парола</strong>
            <span>Първо проверяваме имейла, паролата и дали акаунтът вече е потвърдил имейла си.</span>
          </div>
          <div className="hero-note">
            <strong>Captcha и двуфакторна защита</strong>
            <span>При нужда системата иска captcha и след това временен код от Microsoft/Google Authenticator приложението.</span>
          </div>
        </div>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Вход</p>
          <h2>Влез в профила си</h2>
          <p>Въведи регистрирания имейл и паролата си, за да продължиш към защитената част.</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field-group">
            <span>Имейл</span>
            <input
              type="email"
              value={formState.email}
              onChange={(event) => updateField('email', event.target.value)}
              placeholder="nikola@example.com"
              autoComplete="email"
              disabled={requiresEmailVerification}
            />
          </label>

          <label className="field-group">
            <span>Парола</span>
            <div className="password-input-wrapper">
              <input
                type={isPasswordVisible ? 'text' : 'password'}
                value={formState.password}
                onChange={(event) => updateField('password', event.target.value)}
                placeholder="Въведи паролата"
                autoComplete="current-password"
                disabled={requiresEmailVerification}
              />
              <button
                className="password-toggle-button"
                type="button"
                onClick={() => setIsPasswordVisible((current) => !current)}
                aria-label={isPasswordVisible ? 'Скрий паролата' : 'Покажи паролата'}
                title={isPasswordVisible ? 'Скрий паролата' : 'Покажи паролата'}
                disabled={requiresEmailVerification}
              >
                👁
              </button>
            </div>
          </label>

          {requiresEmailVerification && (
            <div className="message-box message-box--info">
              <strong>Следваща стъпка:</strong> въведи 6-цифрения код, който изпратихме на имейла, и провери папка Spam, ако не го виждаш.
            </div>
          )}

          {requiresTotp && (
            <div className="message-box message-box--info">
              <strong>Следваща стъпка:</strong> въведи временния 6-цифрен код от Microsoft/Google Authenticator приложението.
            </div>
          )}

          {requiresCaptcha && (
            <>
              <CaptchaImage imageBase64={captchaImageBase64} />
              <label className="field-group">
                <span>Код от картинката</span>
                <input
                  type="text"
                  value={formState.captchaToken}
                  onChange={(event) => updateField('captchaToken', event.target.value)}
                  placeholder="Въведи символите от картинката"
                />
              </label>
            </>
          )}

          {requiresEmailVerification && (
            <>
              <label className="field-group">
                <span>Код от имейла</span>
                <input
                  type="text"
                  value={formState.verificationCode}
                  onChange={(event) => updateField('verificationCode', event.target.value)}
                  inputMode="numeric"
                />
              </label>

              <div className="inline-action-row">
                <button
                  className="secondary-button"
                  type="button"
                  onClick={handleResendVerificationCode}
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
            </>
          )}

          {requiresTotp && (
            <div className="message-box message-box--success">
              Паролата и captcha кодът са приети. Отвори следващата стъпка и въведи временния код от Microsoft/Google Authenticator приложението.
            </div>
          )}

          {infoMessage && <div className="message-box message-box--info">{infoMessage}</div>}
          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
          {lockoutSeconds && (
            <div className="message-box message-box--warning">
              Оставащо време до отключване: {lockoutSeconds} секунди.
            </div>
          )}

          <button className="primary-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Изпращане...' : requiresEmailVerification ? 'Потвърди имейла' : 'Вход'}
          </button>
        </form>

        <div className="panel-footer panel-footer--split">
          <span>
            Нямаш профил? <Link to="/register">Създай нов акаунт</Link>
          </span>
          <Link to="/reset-password">Забравена парола</Link>
        </div>
      </section>

      {requiresTotp && !requiresEmailVerification && (
        <div className="auth-modal-backdrop" role="presentation">
          <section className="auth-modal-card" aria-label="Потвърждение с временен код">
            <div className="panel-header auth-modal-card__header">
              <div>
                <p className="eyebrow">Следваща стъпка</p>
                <h2>Потвърди входа с временен код</h2>
                <p>Паролата и captcha кодът са приети. Въведи 6-цифрения код от Microsoft/Google Authenticator приложението.</p>
              </div>
              <button
                className="secondary-button secondary-button--compact"
                type="button"
                onClick={() => {
                  setRequiresTotp(false);
                  updateField('totpCode', '');
                  setInfoMessage('');
                }}
              >
                Затвори
              </button>
            </div>

            <form className="auth-form" onSubmit={handleSubmit}>
              <label className="field-group">
                <span className="inline-label-with-info">
                  <span>Временен код</span>
                  <span className="info-tooltip-badge" tabIndex={0}>
                    i
                    <span className="info-tooltip-content">
                      Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                    </span>
                  </span>
                </span>
                <input
                  type="text"
                  value={formState.totpCode}
                  onChange={(event) => updateField('totpCode', event.target.value)}
                  placeholder="123456"
                  inputMode="numeric"
                  autoFocus
                />
              </label>

              {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

              <div className="inline-action-row">
                <button className="primary-button" type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Потвърждаване...' : 'Потвърди входа'}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  );
}
