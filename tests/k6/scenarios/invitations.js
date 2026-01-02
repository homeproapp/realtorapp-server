import http from 'k6/http';
import { sleep } from 'k6';
import { config, getHeaders } from '../config.js';
import { checkSuccessResponse, randomString } from '../utils/helpers.js';

export function sendInvitation(token) {
  const url = `${config.baseUrl}/api/invitations/v1/send`;

  const payload = JSON.stringify({
    propertyInvitationRequests: [
      {
        propertyId: config.testData.listingId,
        clientInvitationRequests: [
          {
            email: `test-${randomString(8)}@example.com`,
            name: `Test User ${randomString(5)}`,
          },
        ],
      },
    ],
  });

  const params = {
    headers: getHeaders(token),
  };

  const response = http.post(url, payload, params);
  checkSuccessResponse(response, 'Send Invitation');

  return response;
}

export function invitationsScenario() {
  const token = config.auth.agentToken;

  sendInvitation(token);
  sleep(1);
}
