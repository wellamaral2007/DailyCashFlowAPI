# 🏛️ Daily Cash Flow Solution: Test Evidences 

This section acts as the official engineering validation ledger, detailing the cloud resources provisioned inside the Google Cloud Platform (GCP) tenant, integration routines, and real-time black-box validations executed over the architecture edge.

---

## ☁️ Cloud Resources Created

### 🌐 1. VPC Network 

![DailyCashFlowTestsVPCResource](DailyCashFlowTestsVPCResource.png)

### 🔀 2. Google Apigee

![DailyCashFlowTestsAPIGeeResource](DailyCashFlowTestsAPIGeeResource.png)

### 🔀 3. Expose API Gateway by Load Balancer

![DailyCashFlowTestsLBPublicAPIGeeResource](DailyCashFlowTestsLBPublicAPIGeeResource.png)

### ⚡ 4. Google Cloud Functions Gen2 (Compute FaaS)

![DailyCashFlowTestsFaaSResource](DailyCashFlowTestsFaaSResource.png)

### 🔑 5. Secret Manager 

![DailyCashFlowTestsSecretsResource](DailyCashFlowTestsSecretsResource.png)

### 🗄️ 6. Cloud SQL Instance 

![DailyCashFlowTestsDBResource](DailyCashFlowTestsDBResource.png)

#### 🗄️ 6.1. Data Registred 

![DailyCashFlowTestsDBDataResource](DailyCashFlowTestsDBDataResource.png)

### 🧠 7. Memorystore for Redis 

![DailyCashFlowTestsRedisResource](DailyCashFlowTestsRedisResource.png)

#### 🗄️ 7.1. Data Values Keys 

![DailyCashFlowTestsRedisDataKeysResource](DailyCashFlowTestsRedisDataKeysResource.png)

### 📣 8. Cloud Pub/Sub Topic 

![DailyCashFlowTestPubSubResource](DailyCashFlowTestsPubSubResource.png)

### 📥 9. Cloud Pub/Sub Subscriptions 

![DailyCashFlowTestsPubSubSubscriptionResource](DailyCashFlowTestsPubSubSubscriptionResource.png)

### 🛡️ 10. VPC Firewall Rules 

![DailyCashFlowTestsVPCFirewallResource](DailyCashFlowTestsFirewallResource.png)

---

## 🚦 Basic Integration Test - End-to-End APIGEE Validation - Public IP Load Balancer - 136.81.203.14 - Self-Signed Certificate -> Apigee Gateway -> Cloud Functions -> Cloud SQL -> Redis -> Cloud Pub/Sub

The following sequence details black-box execution metrics routed through the public external Apigee Gateway facade, validating Token Generation, Cash Inflow Ingestion, and Real-Time Balance Caching.

### 🔐 Step 1: Requesting OAuth 2.0 Access Token
Executes client credential validation over HTTPS to acquire an active cryptographic bearer token:
```bash
curl -k -X POST https://136.81.203.14/v1/finance-auth/token  
    -H "Host: daily-cashflow-sol-17852.dns-replaceme.example.com"      
    -H "Content-Type: application/x-www-form-urlencoded"      -u "UsLIs0v1EGx0vjAWZ82yRl5JZtbDEsTzzhtHWsn3I3IeO3Mo:838js1Gae7nrGVpzjshjyB6gsbaJNruYlSN9jDAzH9UuijHWS7rdkwMGIBQs9agu"      
    -d "grant_type=client_credentials"
```
**Received Response JSON:**
```json
{
  "refresh_token_expires_in": "0",
  "api_product_list": "[DailyCashFlowProduct]",
  "api_product_list_json": [
    "DailyCashFlowProduct"
  ],
  "organization_name": "daily-cashflow-sol-17852",
  "developer.email": "well.amaral2007@gmail.com",
  "token_type": "BearerToken",
  "issued_at": "1792743300463",
  "client_id": "UsLIs0v1EGx0vjAWZ82yRl5JZtbDEsTzzhtHWsn3I3IeO3Mo",
  "access_token": "n8UgCl8trRqpYsDLPC7JNhAe3AoJ",
  "application_name": "07ada262-d311-4275-ac96-948ddf16922f",
  "scope": "",
  "expires_in": "3599",
  "refresh_count": "0",
  "status": "approved"
}
```

### 📖 Step 2: Query Baseline Consolidated Balance (GET)
Performs a data inquiry before any transactions are applied to evaluate the state machine cache layer:
```bash
curl -k -X GET "https://136.81.203.14/v1/cashflow/balance?date=2026-10-23&session=sessao-integrada-123"      
      -H "Host: daily-cashflow-sol-17852.dns-replaceme.example.com"      
      -H "Authorization: Bearer n8UgCl8trRqpYsDLPC7JNhAe3AoJ"
```
**Received Response JSON (Initial State):**
```json
{"balanceValue": "3192,38"} 
```

### ✍️ Step 3: Publish Financial Movement (POST - Brazilian Currency Format)
Injects an automated cash inflow transacting the string layout with a comma decimal indicator:
```bash
curl -k -X POST "https://136.81.203.14/v1/cashflow/entry?CreditDebitValue=3192,38"  
    -H "Host: daily-cashflow-sol-17852.dns-replaceme.example.com"     
    -H "Authorization: Bearer n8UgCl8trRqpYsDLPC7JNhAe3AoJ"      
    -H "Content-Type: application/json"     
     -d '{"Amount": -192,34}'
```
**Received Response JSON:**
```json
{"message": "Financial event successfully triggered.", "transactionId": "1ec3bd1b-88e5-4209-b043-b208d3f227d5"}
```

### 🦘 Step 4: Validate Caching & Consolidated Balance (GET - Cache Hit Verification)
Queries the balance again to ensure the SAGA choreography and the dual-write engine updated the Redis state:
```bash
curl -k -X GET "https://136.81.203.14/v1/cashflow/balance?date=2026-10-23&session=sessao-integrada-123"      
    -H "Host: daily-cashflow-sol-17852.dns-replaceme.example.com"      
    -H "Authorization: Bearer n8UgCl8trRqpYsDLPC7JNhAe3AoJ"
```
**Received Response JSON (Cache Hit Output):**
```json
{"balanceValue": "6384,76"}
```

---
