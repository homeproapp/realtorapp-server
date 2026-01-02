import { sleep } from 'k6';
import { config } from './config.js';
import { getActiveListings } from './scenarios/listings.js';
import { getUserProfile } from './scenarios/users.js';
import { getConversationsList } from './scenarios/chat.js';

export const options = {
  stages: [
    { duration: '10s', target: 5 },
    { duration: '10s', target: 100 },
    { duration: '30s', target: 100 },
    { duration: '10s', target: 5 },
    { duration: '20s', target: 0 },
  ],

  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.1'],
  },
};

export default function () {
  const token = config.auth.agentToken;

  getUserProfile(token);
  getActiveListings(token);
  getConversationsList(token);

  sleep(0.5);
}
