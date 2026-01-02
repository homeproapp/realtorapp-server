import { sleep } from 'k6';
import { config } from './config.js';
import { chatScenario } from './scenarios/chat.js';
import { tasksScenario } from './scenarios/tasks.js';
import { listingsScenario } from './scenarios/listings.js';

export const options = {
  stages: [
    { duration: '2m', target: 10 },
    { duration: '5m', target: 50 },
    { duration: '5m', target: 100 },
    { duration: '5m', target: 150 },
    { duration: '2m', target: 0 },
  ],

  thresholds: {
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    http_req_failed: ['rate<0.05'],
  },
};

export default function () {
  const scenarios = [
    chatScenario,
    tasksScenario,
    listingsScenario,
  ];

  const randomScenario = scenarios[Math.floor(Math.random() * scenarios.length)];
  randomScenario();

  sleep(0.5);
}
