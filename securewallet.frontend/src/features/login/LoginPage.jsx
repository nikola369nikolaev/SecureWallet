import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { loginUser } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';
import { CaptchaImage } from '../../components/CaptchaImage';

const SESSION_EXPIRED_KEY = 'securewallet.auth.sessionExpired';

export function LoginPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [formState, setFormState] = useState({
    email: location.state?.email ?? '',
    password: '',
    captchaToken: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [requiresCaptcha, setRequiresCaptcha] = useState(false);
  const [captchaImageBase64, setCaptchaImageBase64] = useState(null);
  const [lockoutSeconds, setLockoutSeconds] = useState(null);

  useEffect(() => {
    const hasExpiredSessionFlag = window.sessionStorage.getItem(SESSION_EXPIRED_KEY) === 'true';

    if (location.state?.sessionExpired || hasExpiredSessionFlag) {
      setErrorMessage('Сесията изтече, моля влез отново.');
      window.sessionStorage.removeItem(SESSION_EXPIRED_KEY);
    }
  }, [location.state]);

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

    try {
      const result = await loginUser({
        email: formState.email,
        password: formState.password,
        captchaToken: requiresCaptcha ? formState.captchaToken : null,
      });

      setSession(result);
      navigate('/dashboard', { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
        setRequiresCaptcha(Boolean(error.payload?.requiresCaptcha));
        setCaptchaImageBase64(error.payload?.captchaImageBase64 ?? null);
        setLockoutSeconds(error.payload?.lockoutSeconds ?? null);
        setFormState((current) => ({
          ...current,
          captchaToken: '',
        }));
      } else {
        setErrorMessage('Възникна неочаквана грешка при вход.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page auth-page--login">
      <section className="hero-panel">
        <p className="eyebrow">SecureWallet</p>
        <h1>Влез в своя защитен дигитален портфейл.</h1>
        <p className="hero-copy">
          Това е първият работещ frontend екран към нашия auth backend. През него тестваш входа,
          визуализацията на captcha защитата и поведението при временен lockout след грешни опити.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>JWT вход</strong>
            <span>При успешен вход token-ът се пази локално и ни държи в активна потребителска сесия.</span>
          </div>
          <div className="hero-note">
            <strong>Captcha поток</strong>
            <span>След поредица от грешни опити backend-ът връща base64 изображение на captcha.</span>
          </div>
        </div>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Вход</p>
          <h2>Влез в профила си</h2>
          <p>Въведи регистрирания email и паролата си, за да продължиш към таблото.</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field-group">
            <span>Email</span>
            <input
              type="email"
              value={formState.email}
              onChange={(event) => updateField('email', event.target.value)}
              placeholder="nikola@example.com"
              autoComplete="email"
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
            />
          </label>

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

          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
          {lockoutSeconds && (
            <div className="message-box message-box--warning">
              Оставащо време до отключване: {lockoutSeconds} секунди.
            </div>
          )}

          <button className="primary-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Изпращане...' : 'Вход'}
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