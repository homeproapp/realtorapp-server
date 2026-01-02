import { check } from 'k6';

export function checkResponse(response, expectedStatus, checkName) {
  return check(response, {
    [`${checkName}: status is ${expectedStatus}`]: (r) => r.status === expectedStatus,
    [`${checkName}: response time < 500ms`]: (r) => r.timings.duration < 500,
    [`${checkName}: response has body`]: (r) => r.body && r.body.length > 0,
  });
}

export function checkSuccessResponse(response, checkName) {
  return check(response, {
    [`${checkName}: status is 200`]: (r) => r.status === 200,
    [`${checkName}: response time < 500ms`]: (r) => r.timings.duration < 500,
    [`${checkName}: no error message`]: (r) => {
      try {
        const body = JSON.parse(r.body);
        return !body.errorMessage && !body.error;
      } catch {
        return false;
      }
    },
  });
}

export function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

export function randomString(length = 10) {
  const chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}
