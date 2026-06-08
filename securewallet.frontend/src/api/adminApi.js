import { getJson, postJson } from './httpClient';

export function getAdminUsers(accessToken) {
  return getJson('/api/Admin/users', accessToken);
}

export function getAdminUserDetails(userId, accessToken) {
  return getJson(`/api/Admin/users/${userId}`, accessToken);
}

export function getAdminUserTransactions(userId, accessToken) {
  return getJson(`/api/Admin/users/${userId}/transactions`, accessToken);
}

export function createSupportAccount(payload, accessToken) {
  return postJson('/api/Admin/support-accounts', payload, accessToken);
}
