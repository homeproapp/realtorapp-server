# K6 Load Testing Suite

Comprehensive load testing suite for the RealtorApp API using k6.

## Prerequisites

Install k6:
```bash
# Linux
curl https://github.com/grafana/k6/releases/download/v0.48.0/k6-v0.48.0-linux-amd64.tar.gz -L | tar xvz
sudo mv k6-v0.48.0-linux-amd64/k6 /usr/local/bin/

# macOS
brew install k6
```

## Configuration

### Get Authentication Tokens

First, login to get JWT tokens:

```bash
# Get agent token
curl -X POST http://localhost:30080/api/auth/v1/login \
  -H "Content-Type: application/json" \
  -d '{"email":"agent@example.com","password":"yourpassword"}' \
  | jq -r '.accessToken'

# Get client token
curl -X POST http://localhost:30080/api/auth/v1/login \
  -H "Content-Type: application/json" \
  -d '{"email":"client@example.com","password":"yourpassword"}' \
  | jq -r '.accessToken'
```

### Configure Environment

Create a `.env` file:

```bash
# Copy the example
cp .env.example .env

# Edit with your tokens and IDs
nano .env
```

Your `.env` should look like:
```bash
BASE_URL=http://localhost:30080
AGENT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
CLIENT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
CONVERSATION_ID=1
LISTING_ID=1
TASK_ID=1
```

### Load and Run

```bash
export $(cat .env | xargs) && k6 run smoke-test.js
```

## Test Types

### 1. Smoke Test
Quick validation with minimal load.

```bash
k6 run smoke-test.js
```

- 1 concurrent user for 1 minute
- Tests basic endpoints

### 2. Load Test
Realistic user behavior with gradual ramp-up.

```bash
k6 run load-test.js
```

- Ramps: 5 → 10 → 20 → 0 users
- Duration: 3 minutes
- Weighted scenarios:
  - Chat: 30%
  - Tasks: 30%
  - Listings: 20%
  - Users: 10%
  - Invitations: 10%

### 3. Stress Test
Push beyond normal capacity.

```bash
k6 run stress-test.js
```

- Ramps: 10 → 50 → 100 → 150 → 0 users
- Duration: 19 minutes
- Finds breaking points

### 4. Spike Test
Sudden traffic burst.

```bash
k6 run spike-test.js
```

- Spikes: 5 → 100 → 5 → 0 users
- Duration: 1.5 minutes
- Tests resilience

## Running Tests

### Basic Usage

```bash
# With .env file
export $(cat .env | xargs) && k6 run load-test.js

# Or with inline variables
k6 run load-test.js \
  -e BASE_URL="http://localhost:30080" \
  -e AGENT_TOKEN="eyJhbGci..." \
  -e CLIENT_TOKEN="eyJhbGci..." \
  -e LISTING_ID=1
```

### With Summary Export

```bash
k6 run load-test.js --summary-export=results.json
```

## Monitoring During Tests

### Watch Pod Resources

```bash
watch -n 1 'sudo kubectl top pod -n realtorapp'
```

### Watch System Resources

```bash
htop
```

### Check API Logs

```bash
sudo kubectl logs -f -n realtorapp -l app=realtorapp-api
```

## Understanding Results

### Key Metrics

```
checks.........................: 100.00% ✓ 2400 ✗ 0
http_req_duration..............: avg=120ms p(95)=250ms p(99)=400ms
http_req_failed................: 0.00%
http_reqs......................: 800 26.6/s
vus............................: 10 min=0 max=20
```

- **checks**: % of successful assertions
- **http_req_duration**: Response times (p95 = 95th percentile)
- **http_req_failed**: Error rate
- **http_reqs**: Total requests and requests/second
- **vus**: Virtual users (concurrent)

### Success Criteria

- ✅ Checks > 95%
- ✅ p(95) response time < 500ms
- ✅ Error rate < 1%
- ✅ No pod restarts
- ✅ CPU/Memory within limits

## Scenarios Covered

### Chat
- Get conversations list
- Get message history
- Mark messages as read

### Tasks
- Get listing tasks
- Get task details
- Update task status

### Listings
- Get active listings
- Get listing details

### Users
- Get user profile

### Invitations
- Send invitations

## Troubleshooting

### Authentication Fails (401)

```
Get User Profile: status is 200...FAIL
```

**Fix**:
- Token expired - get a new one
- Token invalid - verify you copied it correctly
- Check BASE_URL is correct

### 404 Errors

```
Get Listing Details: status is 200...FAIL
```

**Fix**: Check that IDs (LISTING_ID, CONVERSATION_ID, etc.) exist in your database

### Connection Refused

```
ERRO[0000] GoError: Get "http://localhost:30080": dial tcp connection refused
```

**Fix**:
- Ensure API is running
- Check BASE_URL is correct
- Test with: `curl http://localhost:30080/api/users/v1/me`

### High Error Rates

**Check**:
- Pod logs for errors
- Database connection pool size
- Resource limits (CPU/Memory)

## Next Steps

1. **Run smoke test** to verify setup
2. **Monitor resources** during load test
3. **Note baseline metrics** (requests/sec, response times)
4. **Compare** against different VPS sizes
5. **Use stress test** to find limits
6. **Plan scaling** based on results
