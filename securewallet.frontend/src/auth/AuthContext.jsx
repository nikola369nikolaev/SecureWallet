import { createContext, useContext, useEffect, useMemo, useState } from 'react';

const STORAGE_KEY = 'securewallet.auth.session';
const SESSION_EXPIRED_KEY = 'securewallet.auth.sessionExpired';
const AuthContext = createContext(null);

function markSessionExpired() {
  window.sessionStorage.setItem(SESSION_EXPIRED_KEY, 'true');
}

function loadStoredSession() {
  try {
    const rawValue = window.localStorage.getItem(STORAGE_KEY);
    if (!rawValue) {
      return null;
    }

    const parsedValue = JSON.parse(rawValue);

    if (isSessionExpired(parsedValue)) {
      markSessionExpired();
      window.localStorage.removeItem(STORAGE_KEY);
      return null;
    }

    return parsedValue;
  } catch {
    return null;
  }
}

function isSessionExpired(session) {
  if (!session?.expiresAtUtc) {
    return true;
  }

  const expiresAt = new Date(session.expiresAtUtc);

  if (Number.isNaN(expiresAt.getTime())) {
    return true;
  }

  return expiresAt.getTime() <= Date.now();
}

export function AuthProvider({ children }) {
  const [session, setSessionState] = useState(() => loadStoredSession());

  function setSession(nextSession) {
    setSessionState(nextSession);
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextSession));
    window.sessionStorage.removeItem(SESSION_EXPIRED_KEY);
  }

  function logout() {
    setSessionState(null);
    window.localStorage.removeItem(STORAGE_KEY);
  }

  useEffect(() => {
    if (!session) {
      return undefined;
    }

    if (isSessionExpired(session)) {
      markSessionExpired();
      logout();
      return undefined;
    }

    const millisecondsUntilExpiry = new Date(session.expiresAtUtc).getTime() - Date.now();
    const timeoutId = window.setTimeout(() => {
      markSessionExpired();
      logout();
    }, millisecondsUntilExpiry);

    return () => window.clearTimeout(timeoutId);
  }, [session]);

  const contextValue = useMemo(
    () => ({
      session,
      isAuthenticated: Boolean(session?.accessToken),
      setSession,
      logout,
    }),
    [session],
  );

  return <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}