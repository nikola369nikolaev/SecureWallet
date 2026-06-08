import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { loginUser, resendEmailVerificationCode, verifyEmailCode } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';
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

        setSession(result);
        navigate('/security/two-factor', { replace: true });
        return;
      }

      const result = await loginUser({
        email: formState.email,
        password: formState.password,
        captchaToken: requiresCaptcha ? formState.captchaToken : null,
        totpCode: requiresTotp ? formState.totpCode : null,
      });

      setSession(result);
      navigate(result.securitySetupRequired ? '/security/two-factor' : '/dashboard', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        const nextRequiresTotp = Boolean(error.payload?.requiresTotp);
        const nextRequiresCaptcha = Boolean(error.payload?.requiresCaptcha);
        const nextRequiresEmailVerification = Boolean(error.payload?.requiresEmailVerification);
        const nextMessage = error.payload?.message ?? error.message;
        const isTotpStepPrompt = nextRequiresTotp && !formState.totpCode;

        if (nextRequiresEmailVerification) {
          setInfoMessage('Имейлът и паролата са приети. Въведи 6-цифрения код от имейла, за да потвърдиш акаунта.');
        } else if (isTotpStepPrompt) {
          setInfoMessage('Имейлът и паролата са приети. Въведи кода от authenticator приложението, за да завършиш входа.');
        } else {
          setErrorMessage(nextMessage);
        }

        setRequiresEmailVerification(nextRequiresEmailVerification);
        setRequiresCaptcha(nextRequiresEmailVerification ? false : nextRequiresCaptcha);
        setRequiresTotp(nextRequiresEmailVerification ? false : nextRequiresTotp);
        setCaptchaImageBase64(nextRequiresEmailVerification ? null : (error.payload?.captchaImageBase64 ?? null));
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
            ? 'Възникна неочаквана грешка при потвърждаване на имейла.'
            : 'Възникна неочаквана грешка при вход.',
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
      setInfoMessage(`${result.message} Кодът е валиден до ${new Date(result.expiresAtUtc).toLocaleString('bg-BG')}.`);
      setCooldownSeconds(RESEND_COOLDOWN_SECONDS);
      setFormState((current) => ({
        ...current,
        verificationCode: '',
      }));
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при изпращане на нов код.');
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
          Тук тестваш входа, captcha защитата, двуфакторния код от authenticator приложението
          и поведението на системата при временен lockout след грешни опити.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>Имейл + парола</strong>
            <span>Първо проверяваме имейла, паролата и дали акаунтът вече е потвърдил имейла си.</span>
          </div>
          <div className="hero-note">
            <strong>Captcha и 2FA</strong>
            <span>При нужда системата иска captcha и след това код от authenticator приложението.</span>
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
            <input
              type="password"
              value={formState.password}
              onChange={(event) => updateField('password', event.target.value)}
              placeholder="Въведи паролата"
              autoComplete="current-password"
              disabled={requiresEmailVerification}
            />
          </label>

          {requiresEmailVerification && (
            <div className="message-box message-box--info">
              <strong>Следваща стъпка:</strong> въведи 6-цифрения код, който изпратихме на имейла, и провери папка Spam, ако не го виждаш.
            </div>
          )}

          {requiresTotp && (
            <div className="message-box message-box--info">
              <strong>Следваща стъпка:</strong> въведи 6-цифрения код от authenticator приложението.
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
                <button className="secondary-button" type="button" onClick={handleResendVerificationCode} disabled={isResending || cooldownSeconds > 0}>
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
            <label className="field-group">
              <span>Код от authenticator приложението</span>
              <input
                type="text"
                value={formState.totpCode}
                onChange={(event) => updateField('totpCode', event.target.value)}
                placeholder="123456"
                inputMode="numeric"
              />
            </label>
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
    </main>
  );
}
