import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { completePasswordReset } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';
import { createSessionFromAuthResult } from '../../auth/sessionStorage';

const RESET_SESSION_STORAGE_KEY = 'securewallet.auth.passwordReset';

function loadResetSession() {
  try {
    const rawValue = window.sessionStorage.getItem(RESET_SESSION_STORAGE_KEY);

    if (!rawValue) {
      return null;
    }

    return JSON.parse(rawValue);
  } catch {
    return null;
  }
}

export function ResetPasswordConfirmPage() {
  const navigate = useNavigate();
  const { setSession } = useAuth();
  const [resetSession] = useState(() => loadResetSession());
  const [formState, setFormState] = useState({
    newPassword: '',
    confirmPassword: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const passwordRules = [
    {
      label: 'Поне 8 символа',
      isSatisfied: formState.newPassword.length >= 8,
    },
    {
      label: 'Поне една главна буква',
      isSatisfied: /\p{Lu}/u.test(formState.newPassword),
    },
    {
      label: 'Поне една цифра',
      isSatisfied: /[0-9]/.test(formState.newPassword),
    },
    {
      label: 'Двете полета съвпадат',
      isSatisfied:
        formState.confirmPassword.length > 0 &&
        formState.newPassword === formState.confirmPassword,
    },
  ];

  const isPasswordReady = passwordRules.every((rule) => rule.isSatisfied);

  useEffect(() => {
    if (!resetSession?.resetSessionToken) {
      navigate('/reset-password', { replace: true });
    }
  }, [navigate, resetSession]);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage('');

    if (!isPasswordReady) {
      setErrorMessage('Новата парола още не покрива всички условия.');
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await completePasswordReset({
        resetSessionToken: resetSession.resetSessionToken,
        newPassword: formState.newPassword,
      });

      window.sessionStorage.removeItem(RESET_SESSION_STORAGE_KEY);
      setSession(createSessionFromAuthResult(result));

      navigate('/security/two-factor', {
        replace: true,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна проблем при смяна на паролата. Опитай отново.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page auth-page--register">
      <section className="hero-panel hero-panel--warm">
        <p className="eyebrow">SecureWallet</p>
        <h1>Задай новата парола на потвърдения акаунт.</h1>
        <p className="hero-copy">
          Сега вече сменяш старата парола точно на акаунта с имейл <strong>{resetSession?.email ?? '-'}</strong>.
        </p>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Нова парола</p>
          <h2>Смени паролата</h2>
          <p>Въведи нова парола и я повтори, за да завършиш процеса.</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="field-group">
            <span>Нова парола</span>
            <input
              type="password"
              value={formState.newPassword}
              onChange={(event) => updateField('newPassword', event.target.value)}
              placeholder="Password1"
              autoComplete="new-password"
            />
          </label>

          <div className="password-rules-card">
            <p className="password-rules-title">Паролата трябва да съдържа:</p>
            <p className="field-hint">
              В момента системата позволява и специални символи. Задължителни са само условията по-долу.
            </p>
            <ul className="password-rules-list">
              {passwordRules.map((rule) => (
                <li
                  key={rule.label}
                  className={rule.isSatisfied ? 'password-rule password-rule--ok' : 'password-rule password-rule--pending'}
                >
                  {rule.label}
                </li>
              ))}
            </ul>
          </div>

          <label className="field-group">
            <span>Повтори новата парола</span>
            <input
              type="password"
              value={formState.confirmPassword}
              onChange={(event) => updateField('confirmPassword', event.target.value)}
              placeholder="Password1"
              autoComplete="new-password"
            />
          </label>

          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

          <button className="primary-button" type="submit" disabled={isSubmitting || !isPasswordReady}>
            {isSubmitting ? 'Изпращане...' : 'Смени паролата'}
          </button>
        </form>

        <div className="panel-footer panel-footer--split">
          <Link to="/reset-password">Назад към SMS проверката</Link>
          <Link to="/login">Назад към вход</Link>
        </div>
      </section>
    </main>
  );
}
