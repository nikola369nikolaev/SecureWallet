import {
  clearStoredSession,
  createSessionFromAuthResult,
  isAccessTokenExpired,
  isRefreshTokenExpired,
  loadStoredSession,
  markSessionExpired,
  saveStoredSession,
} from '../auth/sessionStorage';

export class ApiError extends Error {
  constructor(message, status, payload) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.payload = payload;
  }
}

let refreshRequestPromise = null;

function createHeaders(accessToken, contentType = 'application/json') {
  const headers = {};

  if (contentType) {
    headers['Content-Type'] = contentType;
  }

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  return headers;
}

async function readResponseBody(response) {
  const contentType = response.headers.get('content-type') ?? '';
  const hasJson = contentType.includes('application/json');
  return hasJson ? await response.json() : null;
}

function ensureSuccess(response, body) {
  if (!response.ok) {
    const message = body?.message ?? 'Възникна проблем при заявката. Опитай отново.';
    throw new ApiError(message, response.status, body);
  }
}

async function sendRequest(url, method, payload, accessToken = null) {
  const response = await fetch(url, {
    method,
    headers: createHeaders(accessToken, payload === undefined ? null : 'application/json'),
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });

  const body = await readResponseBody(response);
  return { response, body };
}

async function getValidAccessToken(accessToken) {
  if (!accessToken) {
    return null;
  }

  const storedSession = loadStoredSession();
  if (!storedSession || storedSession.accessToken !== accessToken) {
    return accessToken;
  }

  if (!isAccessTokenExpired(storedSession)) {
    return accessToken;
  }

  const refreshedSession = await refreshStoredSession();
  return refreshedSession.accessToken;
}

async function refreshStoredSession() {
  if (refreshRequestPromise) {
    return refreshRequestPromise;
  }

  const currentSession = loadStoredSession();
  if (!currentSession?.refreshToken || isRefreshTokenExpired(currentSession)) {
    markSessionExpired();
    clearStoredSession();
    throw new ApiError('Сесията изтече. Моля влез отново.', 401, { message: 'Сесията изтече. Моля влез отново.' });
  }

  refreshRequestPromise = (async () => {
    const { response, body } = await sendRequest('/api/Auth/refresh', 'POST', {
      userId: currentSession.userId,
      refreshToken: currentSession.refreshToken,
    });

    if (!response.ok) {
      markSessionExpired();
      clearStoredSession();
      throw new ApiError('Сесията изтече. Моля влез отново.', 401, body);
    }

    const refreshedSession = createSessionFromAuthResult(body);
    saveStoredSession(refreshedSession);
    return refreshedSession;
  })();

  try {
    return await refreshRequestPromise;
  } finally {
    refreshRequestPromise = null;
  }
}

async function requestJson(url, method, payload, accessToken = null) {
  let resolvedAccessToken = await getValidAccessToken(accessToken);
  let { response, body } = await sendRequest(url, method, payload, resolvedAccessToken);

  if (response.status === 401 && resolvedAccessToken) {
    const refreshedSession = await refreshStoredSession();
    resolvedAccessToken = refreshedSession.accessToken;
    ({ response, body } = await sendRequest(url, method, payload, resolvedAccessToken));
  }

  ensureSuccess(response, body);
  return body;
}

export async function postJson(url, payload, accessToken = null) {
  return requestJson(url, 'POST', payload, accessToken);
}

export async function getJson(url, accessToken = null) {
  return requestJson(url, 'GET', undefined, accessToken);
}
