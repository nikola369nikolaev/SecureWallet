const STORAGE_KEY = 'securewallet.auth.session';
const SESSION_EXPIRED_KEY = 'securewallet.auth.sessionExpired';

export function markSessionExpired() {
  window.sessionStorage.setItem(SESSION_EXPIRED_KEY, 'true');
}

export function clearSessionExpiredFlag() {
  window.sessionStorage.removeItem(SESSION_EXPIRED_KEY);
}

export function hasSessionExpiredFlag() {
  return window.sessionStorage.getItem(SESSION_EXPIRED_KEY) === 'true';
}

export function saveStoredSession(session) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
}

export function clearStoredSession() {
  window.localStorage.removeItem(STORAGE_KEY);
}

export function isAccessTokenExpired(session) {
  return isUtcDateExpired(session?.expiresAtUtc);
}

export function isRefreshTokenExpired(session) {
  if (!session?.refreshToken) {
    return true;
  }

  return isUtcDateExpired(session?.refreshTokenExpiresAtUtc);
}

export function loadStoredSession() {
  try {
    const rawValue = window.localStorage.getItem(STORAGE_KEY);
    if (!rawValue) {
      return null;
    }

    const parsedValue = JSON.parse(rawValue);
    if (isRefreshTokenExpired(parsedValue)) {
      markSessionExpired();
      clearStoredSession();
      return null;
    }

    return parsedValue;
  } catch {
    return null;
  }
}

export function createSessionFromAuthResult(result) {
  return {
    accessToken: result.accessToken,
    expiresAtUtc: result.expiresAtUtc,
    refreshToken: result.refreshToken,
    refreshTokenExpiresAtUtc: result.refreshTokenExpiresAtUtc,
    userId: result.userId,
    username: result.username,
    email: result.email,
    role: result.role,
    twoFactorEnabled: result.twoFactorEnabled,
    isEmailVerified: result.isEmailVerified,
    securitySetupRequired: result.securitySetupRequired,
  };
}

function isUtcDateExpired(value) {
  if (!value) {
    return true;
  }

  const expiresAt = new Date(value);
  if (Number.isNaN(expiresAt.getTime())) {
    return true;
  }

  return expiresAt.getTime() <= Date.now();
}
