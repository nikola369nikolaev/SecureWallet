import { getJson } from './httpClient';

export function getCurrentWallet(accessToken) {
  return getJson('/api/Wallet/me', accessToken);
}
