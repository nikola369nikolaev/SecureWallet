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

export function getAdminTransactionHistoryPage(query, accessToken) {
  const searchParams = new URLSearchParams();

  if (query.type) {
    searchParams.set('type', query.type);
  }

  if (query.dateRange) {
    searchParams.set('dateRange', query.dateRange);
  }

  if (query.searchTerm) {
    searchParams.set('searchTerm', query.searchTerm);
  }

  if (query.page) {
    searchParams.set('page', String(query.page));
  }

  if (query.pageSize) {
    searchParams.set('pageSize', String(query.pageSize));
  }

  return getJson(`/api/Admin/transactions/history?${searchParams.toString()}`, accessToken);
}

export function createSupportAccount(payload, accessToken) {
  return postJson('/api/Admin/support-accounts', payload, accessToken);
}

export function getAdminLogs(accessToken, take = 1000) {
  return getJson(`/api/Admin/logs?take=${take}`, accessToken);
}
