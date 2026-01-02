export const config = {
  baseUrl: __ENV.BASE_URL || 'http://localhost:30080',

  auth: {
    agentToken: __ENV.AGENT_TOKEN || 'YOUR_AGENT_JWT_TOKEN',
    clientToken: __ENV.CLIENT_TOKEN || 'YOUR_CLIENT_JWT_TOKEN',
  },

  testData: {
    conversationId: parseInt(__ENV.CONVERSATION_ID) || 1,
    listingId: parseInt(__ENV.LISTING_ID) || 1,
    taskId: parseInt(__ENV.TASK_ID) || 1,
  },

  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.01'],
  },
};

export function getHeaders(token = null, contentType = 'application/json') {
  const headers = {
    'Content-Type': contentType,
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return headers;
}
