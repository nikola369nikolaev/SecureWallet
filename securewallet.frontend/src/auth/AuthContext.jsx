import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import {
  clearSessionExpiredFlag,
  clearStoredSession,
  isRefreshTokenExpired,
  loadStoredSession,
  markSessionExpired,
  saveStoredSession,
} from './sessionStorage';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [session, setSessionState] = useState(() => loadStoredSession());

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

  function logout() {
    setSessionState(null);
    clearStoredSession();
    clearSessionExpiredFlag();
  }

  useEffect(() => {
    if (!session) {
      return undefined;
    }

    if (isRefreshTokenExpired(session)) {
      markSessionExpired();
      setSessionState(null);
      clearStoredSession();
      return undefined;
    }

    const millisecondsUntilRefreshExpiry = new Date(session.refreshTokenExpiresAtUtc).getTime() - Date.now();
    const timeoutId = window.setTimeout(() => {
      markSessionExpired();
      setSessionState(null);
      clearStoredSession();
    }, millisecondsUntilRefreshExpiry);

    return () => window.clearTimeout(timeoutId);
  }, [session]);

  const contextValue = useMemo(
    () => ({
      session,
      isAuthenticated: Boolean(session?.accessToken && !isRefreshTokenExpired(session)),
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
