import { getJson, postJson } from './httpClient';

export function registerUser(payload) {
  return postJson('/api/Auth/register', payload);
}

export function loginUser(payload) {
  return postJson('/api/Auth/login', payload);
}

export function refreshSession(payload) {
  return postJson('/api/Auth/refresh', payload);
}

export function verifyEmailCode(payload) {
  return postJson('/api/Auth/verify-email', payload);
}

export function resendEmailVerificationCode(payload) {
  return postJson('/api/Auth/verify-email/resend', payload);
}

export function requestPasswordResetCode(payload) {
  return postJson('/api/Auth/reset-password/request-code', payload);
}

export function verifyPasswordResetCode(payload) {
  return postJson('/api/Auth/reset-password/verify-code', payload);
}

export function completePasswordReset(payload) {
  return postJson('/api/Auth/reset-password/complete', payload);
}

export function beginTotpSetup(accessToken) {
  return getJson('/api/Auth/totp/setup', accessToken);
}

export function verifyTotpSetup(payload, accessToken) {
  return postJson('/api/Auth/totp/verify-setup', payload, accessToken);
}

export function disableTotp(payload, accessToken) {
  return postJson('/api/Auth/totp/disable', payload, accessToken);
}

export function resetTotpSetup(payload, accessToken) {
  return postJson('/api/Auth/totp/reset', payload, accessToken);
}
