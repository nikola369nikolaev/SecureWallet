import { postJson } from './httpClient';

export function registerUser(payload) {
  return postJson('/api/Auth/register', payload);
}

export function loginUser(payload) {
  return postJson('/api/Auth/login', payload);
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