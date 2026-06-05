export class ApiError extends Error {
  constructor(message, status, payload) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.payload = payload;
  }
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
    const message = body?.message ?? `Request failed with status ${response.status}.`;
    throw new ApiError(message, response.status, body);
  }
}

export async function postJson(url, payload, accessToken = null) {
  const response = await fetch(url, {
    method: 'POST',
    headers: createHeaders(accessToken),
    body: JSON.stringify(payload),
  });

  const body = await readResponseBody(response);
  ensureSuccess(response, body);

  return body;
}

export async function getJson(url, accessToken = null) {
  const response = await fetch(url, {
    method: 'GET',
    headers: createHeaders(accessToken, null),
  });

  const body = await readResponseBody(response);
  ensureSuccess(response, body);

  return body;
}
