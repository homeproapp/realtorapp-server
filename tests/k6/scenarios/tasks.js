import http from 'k6/http';
import { sleep } from 'k6';
import { config, getHeaders } from '../config.js';
import { checkSuccessResponse, randomString } from '../utils/helpers.js';

export function getListingTasks(token, listingId) {
  const url = `${config.baseUrl}/api/tasks/v1/listings/${listingId}`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Listing Tasks');

  return response;
}

export function getListingTasksSlim(token, listingId) {
  const url = `${config.baseUrl}/api/tasks/v1/listings/${listingId}/slim`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Listing Tasks Slim');

  return response;
}

export function getTaskDetails(token, listingId, taskId) {
  const url = `${config.baseUrl}/api/tasks/v1/${listingId}/${taskId}`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Task Details');

  return response;
}

export function updateTaskStatus(token, listingId, taskId, newStatus) {
  const url = `${config.baseUrl}/api/tasks/v1/${listingId}/${taskId}`;
  const payload = JSON.stringify({
    newStatus: newStatus,
  });

  const params = {
    headers: getHeaders(token),
  };

  const response = http.patch(url, payload, params);
  checkSuccessResponse(response, 'Update Task Status');

  return response;
}

export function tasksScenario() {
  const agentToken = config.auth.agentToken;
  const clientToken = config.auth.clientToken;
  const listingId = config.testData.listingId;
  const taskId = config.testData.taskId;

  getListingTasks(agentToken, listingId);
  sleep(1);

  getListingTasksSlim(clientToken, listingId);
  sleep(1);

  getTaskDetails(agentToken, listingId, taskId);
  sleep(1);

  updateTaskStatus(clientToken, listingId, taskId, 1);
  sleep(1);
}
