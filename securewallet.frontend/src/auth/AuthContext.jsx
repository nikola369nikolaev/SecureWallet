import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { refreshSession } from '../api/authApi';
import { ApiError, configureSessionRenewal } from '../api/httpClient';
import {
  clearSessionExpiredFlag,
  clearStoredSession,
  createSessionFromAuthResult,
  loadStoredSession,
  saveStoredSession,
} from './sessionStorage';

const AuthContext = createContext(null);
const TEMPORARY_CODE_TOOLTIP = 'Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.';

export function AuthProvider({ children }) {
  const [session, setSessionState] = useState(() => loadStoredSession());
  const [renewalPrompt, setRenewalPrompt] = useState(null);
  const [renewalCode, setRenewalCode] = useState('');
  const [renewalError, setRenewalError] = useState('');
  const [isRenewingSession, setIsRenewingSession] = useState(false);

  function setSession(nextSession) {
    if (!nextSession) {
      setSessionState(null);
      clearStoredSession();
      clearSessionExpiredFlag();
      return;
    }

    setSessionState(nextSession);
    saveStoredSession(nextSession);
    clearSessionExpiredFlag();
  }

  function rejectRenewalPrompt() {
    if (!renewalPrompt) {
      return;
    }

    renewalPrompt.reject(new ApiError('Сесията изтече. Моля влез отново.', 401, { message: 'Сесията изтече. Моля влез отново.' }));
    setRenewalPrompt(null);
    setRenewalCode('');
    setRenewalError('');
    setIsRenewingSession(false);
  }

  function logout() {
    rejectRenewalPrompt();
    setSessionState(null);
    clearStoredSession();
    clearSessionExpiredFlag();
  }

  useEffect(() => {
    configureSessionRenewal((currentSession) => new Promise((resolve, reject) => {
      setRenewalCode('');
      setRenewalError('');
      setIsRenewingSession(false);
      setRenewalPrompt({ currentSession, resolve, reject });
    }));

    return () => configureSessionRenewal(null);
  }, []);

  async function handleRenewalSubmit(event) {
    event.preventDefault();

    if (!renewalPrompt) {
      return;
    }

    setIsRenewingSession(true);
    setRenewalError('');

    try {
      const result = await refreshSession({
        expiredAccessToken: renewalPrompt.currentSession.accessToken,
        totpCode: renewalCode,
      });

      const nextSession = createSessionFromAuthResult(result);
      setSession(nextSession);
      renewalPrompt.resolve(nextSession);
      setRenewalPrompt(null);
      setRenewalCode('');
      setRenewalError('');
    } catch (error) {
      if (error instanceof ApiError) {
        setRenewalError(error.payload?.message ?? error.message);
      } else {
        setRenewalError('Възникна проблем при подновяване на сесията. Опитай отново.');
      }
    } finally {
      setIsRenewingSession(false);
    }
  }

  const contextValue = useMemo(
    () => ({
      session,
      isAuthenticated: Boolean(session?.accessToken),
      setSession,
      logout,
    }),
    [session],
  );

  return (
    <AuthContext.Provider value={contextValue}>
      {children}

      {renewalPrompt && (
        <div className="auth-modal-backdrop" role="presentation">
          <section className="auth-modal-card" aria-label="Подновяване на сесията">
            <div className="panel-header auth-modal-card__header">
              <div>
                <p className="eyebrow">Сесия</p>
                <h2>Поднови сесията с временен код</h2>
                <p>10-минутната сесия изтече. Въведи временния код и ще продължиш без нов вход.</p>
              </div>
              <button
                className="secondary-button secondary-button--compact"
                type="button"
                onClick={logout}
              >
                Излез
              </button>
            </div>

            <form className="auth-form" onSubmit={handleRenewalSubmit}>
              <label className="field-group">
                <span className="inline-label-with-info">
                  <span>Временен код</span>
                  <span className="info-tooltip-badge" tabIndex={0}>
                    i
                    <span className="info-tooltip-content">{TEMPORARY_CODE_TOOLTIP}</span>
                  </span>
                </span>
                <input
                  type="text"
                  value={renewalCode}
                  onChange={(event) => setRenewalCode(event.target.value)}
                  placeholder="123456"
                  inputMode="numeric"
                  autoFocus
                />
              </label>

              {renewalError && <div className="message-box message-box--error">{renewalError}</div>}

              <div className="inline-action-row">
                <button className="primary-button" type="submit" disabled={isRenewingSession}>
                  {isRenewingSession ? 'Подновяване...' : 'Поднови сесията'}
                </button>
                <button className="secondary-button" type="button" onClick={logout}>
                  Изход
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}
