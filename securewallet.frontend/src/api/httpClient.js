import {
  clearStoredSession,
  isAccessTokenExpired,
  loadStoredSession,
  markSessionExpired,
} from '../auth/sessionStorage';

export class ApiError extends Error {
  constructor(message, status, payload) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.payload = payload;
  }
}

let sessionRenewalHandler = null;
let sessionRenewalPromise = null;

export function configureSessionRenewal(handler) {
  sessionRenewalHandler = handler;
}

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

function handleExpiredSession() {
  markSessionExpired();
  clearStoredSession();
}

async function renewStoredSession(currentSession) {
  if (sessionRenewalPromise) {
    return sessionRenewalPromise;
  }

  if (!currentSession?.accessToken || !currentSession.twoFactorEnabled || !sessionRenewalHandler) {
    handleExpiredSession();
    throw new ApiError('Сесията изтече. Моля влез отново.', 401, { message: 'Сесията изтече. Моля влез отново.' });
  }

  sessionRenewalPromise = (async () => {
    try {
      const renewedSession = await sessionRenewalHandler(currentSession);
      if (!renewedSession?.accessToken) {
        throw new ApiError('Сесията изтече. Моля влез отново.', 401, { message: 'Сесията изтече. Моля влез отново.' });
      }

      return renewedSession;
    } catch (error) {
      handleExpiredSession();
      if (error instanceof ApiError) {
        throw error;
      }

      throw new ApiError('Сесията изтече. Моля влез отново.', 401, { message: 'Сесията изтече. Моля влез отново.' });
    } finally {
      sessionRenewalPromise = null;
    }
  })();

  return sessionRenewalPromise;
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

  const renewedSession = await renewStoredSession(storedSession);
  return renewedSession.accessToken;
}

async function requestJson(url, method, payload, accessToken = null) {
  let resolvedAccessToken = await getValidAccessToken(accessToken);
  let { response, body } = await sendRequest(url, method, payload, resolvedAccessToken);

  if (response.status === 401 && resolvedAccessToken) {
    const storedSession = loadStoredSession();
    const renewedSession = await renewStoredSession(storedSession ?? { accessToken: resolvedAccessToken, twoFactorEnabled: true });
    resolvedAccessToken = renewedSession.accessToken;
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
