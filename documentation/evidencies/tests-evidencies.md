# 🏛️ Daily Cash Flow Solution: Test Evidences 

This section acts as the official engineering validation ledger, detailing the cloud resources provisioned inside the Google Cloud Platform (GCP) tenant, integration routines, and real-time black-box validations executed over the architecture edge.

---

## ☁️ Cloud Resources Created

### 🌐 1. VPC Network 

![DailyCashFlowLoadTestsRunner-Local-Emulator-Results](DailyCashTestsRunner-Local-Results.png)

### 🔀 2. Google Apigee

![DailyCashFlowTestsAPIGeeResource](DailyCashFlowTestsAPIGeeResource.png)

### ⚡ 3. Google Cloud Functions Gen2 (Compute FaaS)

![DailyCashFlowTestsFaaSResource](DailyCashFlowTestsFaaSResource.png)

### 🔑 4. Secret Manager 

![DailyCashFlowTestsSecretsResource](DailyCashFlowTestsSecretsResource.png)

### 🗄️ 5. Cloud SQL Instance 

![DailyCashFlowTestsDBResource](DailyCashFlowTestsDBResource.png)

#### 🗄️ 5.1. Data Registred 

![DailyCashFlowTestsDBDataResource](DailyCashFlowTestsDBDataResource.png)

### 🧠 6. Memorystore for Redis 

![DailyCashFlowTestsRedisResource](DailyCashFlowTestsRedisResource.png)

### 📣 7. Cloud Pub/Sub Topic 

![DailyCashFlowTestPubSubResource](DailyCashFlowTestPubSubResource.png)

### 📥 8. Cloud Pub/Sub Subscriptions 

![DailyCashFlowTestsPubSubSubscriptionResource](DailyCashFlowTestsPubSubSubscriptionResource.png)

### 🛡️ 9. VPC Firewall Rules 

![DailyCashFlowTestsVPCFirewallResource](DailyCashFlowTestsFirewallResource.png)

---

## 🚦 Basic Test - End-to-End APIGEE Validation

The following sequence details black-box execution metrics routed through the public external Apigee Gateway facade, validating Token Generation, Cash Inflow Ingestion, and Real-Time Balance Caching.

### 🔐 Step 1: Requesting OAuth 2.0 Access Token
Executes client credential validation over HTTPS to acquire an active cryptographic bearer token:
```bash
curl -X POST https://carrefour.com \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=YOUR_CLIENT_ID&client_secret=YOUR_CLIENT_SECRET"
```
**Expected Response JSON:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

### 📖 Step 2: Query Baseline Consolidated Balance (GET)
Performs a data inquiry before any transactions are applied to evaluate the state machine cache layer:
```bash
curl -X GET "https://carrefour.com" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```
**Expected Response JSON (Initial State):**
```json
{
  "timestamp": "2026-10-21T04:00:01Z",
  "status": "SUCCESS",
  "date": "2026-10-21",
  "balanceValue": "0,00"
}
```

### ✍️ Step 3: Publish Financial Movement (POST - Brazilian Currency Format)
Injects an automated cash inflow transacting the string layout with a comma decimal indicator:
```bash
curl -X POST "https://carrefour.com" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```
**Expected Response JSON:**
```json
{
  "message": "Financial event successfully triggered.",
  "transactionId": "fa73b412-dae6-4512-af29-a132cd472b42"
}
```

### 🦘 Step 4: Validate Caching & Consolidated Balance (GET - Cache Hit Verification)
Queries the balance again to ensure the SAGA choreography and the dual-write engine updated the Redis state:
```bash
curl -X GET "https://carrefour.com" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```
**Expected Response JSON (Cache Hit Output):**
```json
{
  "timestamp": "2026-10-21T04:02:15Z",
  "status": "SUCCESS",
  "date": "2026-10-21",
  "balanceValue": "4944325,91"
}
```

---

## 📷 Integration Test Evidence 

👉 ![DailyCashFlowTestsVPCFirewallResource](DailyCashFlowIntegrationTests.png)