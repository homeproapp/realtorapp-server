import http from 'k6/http';
import { sleep } from 'k6';
import { config, getHeaders } from '../config.js';
import { checkSuccessResponse } from '../utils/helpers.js';

export function getConversationsList(token) {
  const url = `${config.baseUrl}/api/chat/v1/conversations`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Conversations List');

  return response;
}

export function getMessages(token, conversationId) {
  const url = `${config.baseUrl}/api/chat/v1/conversations/${conversationId}/messages`;
  const params = {
    headers: getHeaders(token),
  };

  const response = http.get(url, params);
  checkSuccessResponse(response, 'Get Messages');

  return response;
}

export function markMessagesAsRead(token, conversationId, messageIds) {
  const url = `${config.baseUrl}/api/chat/v1/conversations/${conversationId}/messages/read`;
  const payload = JSON.stringify({
    messageIds: messageIds,
  });

  const params = {
    headers: getHeaders(token),
  };

  const response = http.post(url, payload, params);
  checkSuccessResponse(response, 'Mark Messages as Read');

  return response;
}

export function chatScenario() {
  const token = config.auth.agentToken;
  const conversationId = config.testData.conversationId;

  getConversationsList(token);
  sleep(1);

  getMessages(token, conversationId);
  sleep(1);

  markMessagesAsRead(token, conversationId, [1, 2, 3]);
  sleep(1);
}
