import http from 'k6/http';
import { sleep } from 'k6';
import { config, getHeaders } from '../config.js';
import { checkSuccessResponse } from '../utils/helpers.js';

export function getUserProfile(token) {
  const url = `${config.baseUrl}/api/users/v1/me`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get User Profile');

  return response;
}

export function usersScenario() {
  const token = config.auth.agentToken;

  getUserProfile(token);
  sleep(1);
}
