import http from 'k6/http';
import { sleep } from 'k6';
import { config, getHeaders } from '../config.js';
import { checkSuccessResponse } from '../utils/helpers.js';

export function getActiveListings(token) {
  const url = `${config.baseUrl}/api/listings/v1/active`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Active Listings');

  return response;
}

export function getListingDetails(token, listingId) {
  const url = `${config.baseUrl}/api/listings/v1/${listingId}/slim`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Listing Details');

  return response;
}

export function listingsScenario() {
  const token = config.auth.agentToken;
  const listingId = config.testData.listingId;

  getActiveListings(token);
  sleep(1);

  getListingDetails(token, listingId);
  sleep(1);
}
