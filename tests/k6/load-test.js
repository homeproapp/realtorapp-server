import { sleep } from 'k6';
import { config } from './config.js';
import { chatScenario } from './scenarios/chat.js';
import { tasksScenario } from './scenarios/tasks.js';
import { listingsScenario } from './scenarios/listings.js';
import { invitationsScenario } from './scenarios/invitations.js';
import { usersScenario } from './scenarios/users.js';

export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 10 },
    { duration: '1m', target: 20 },
    { duration: '30s', target: 0 },
  ],

  thresholds: config.thresholds,
};

export default function () {
  const scenarios = [
    { name: 'chat', fn: chatScenario, weight: 30 },
    { name: 'tasks', fn: tasksScenario, weight: 30 },
    { name: 'listings', fn: listingsScenario, weight: 20 },
    { name: 'users', fn: usersScenario, weight: 10 },
    { name: 'invitations', fn: invitationsScenario, weight: 10 },
  ];

  const totalWeight = scenarios.reduce((sum, s) => sum + s.weight, 0);
  const random = Math.random() * totalWeight;

  let cumulativeWeight = 0;
  for (const scenario of scenarios) {
    cumulativeWeight += scenario.weight;
    if (random <= cumulativeWeight) {
      scenario.fn();
      break;
    }
  }

  sleep(1);
}
