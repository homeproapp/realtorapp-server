import { sleep } from 'k6';
import { config } from './config.js';
import { getUserProfile } from './scenarios/users.js';
import { getActiveListings, getListingDetails } from './scenarios/listings.js';
import { getConversationsList } from './scenarios/chat.js';
import { getListingTasks } from './scenarios/tasks.js';

export const options = {
  vus: 1,
  duration: '1m',
  thresholds: config.thresholds,
};

export default function () {
  const token = config.auth.agentToken;

  getUserProfile(token);
  sleep(1);

  getActiveListings(token);
  sleep(1);

  getListingDetails(token, config.testData.listingId);
  sleep(1);

  getConversationsList(token);
  sleep(1);

  getListingTasks(token, config.testData.listingId);
  sleep(1);
}
