import { getJson, postJson } from './httpClient';

export function createDeposit(payload, accessToken) {
  return postJson('/api/Transactions/deposit', payload, accessToken);
}

export function createTransfer(payload, accessToken) {
  return postJson('/api/Transactions/transfer', payload, accessToken);
}

export function getTransactionHistoryPage(filters, accessToken) {
  const params = new URLSearchParams();

  if (filters?.type) {
    params.set('type', filters.type);
  }

  if (filters?.dateRange) {
    params.set('dateRange', filters.dateRange);
  }

  if (filters?.searchTerm) {
    params.set('searchTerm', filters.searchTerm);
  }

  if (filters?.page) {
    params.set('page', String(filters.page));
  }

  if (filters?.pageSize) {
    params.set('pageSize', String(filters.pageSize));
  }

  const queryString = params.toString();
  const endpoint = queryString
    ? `/api/Transactions/history?${queryString}`
    : '/api/Transactions/history';

  return getJson(endpoint, accessToken);
}

export async function getTransactionHistory(accessToken) {
  const result = await getTransactionHistoryPage({ page: 1, pageSize: 20 }, accessToken);
  return result.items;
}
