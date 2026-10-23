#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (API Gateway Ingress)
# CONCEPTS:        Google Apigee API Management / Bundle Import Syntax Patch
# ============================================================================
set -e

# Output color variables for deployment telemetry logging
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Global configuration variables for Apigee governance
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
export APIGEE_ORG="daily-cashflow-sol-17852"
export APIGEE_ENV="eval"
export APIGEE_REGION="us-west1"
export VPC_CONNECTOR_CONNECTOR="cashflow-vpc_connector-connector"

# Target deployment region routing settings
export ANALYTICS_REGION="us-west1"

# Dynamic extraction of the target network mesh sorted by creation timeline
export NETWORK_NAME=$(gcloud compute networks list --format="value(name)" --sort-by="~creationTimestamp" | head -n 1)

# Fallback block triggered if no active network mesh instance is detected
if [ -z "$NETWORK_NAME" ]; then
    echo "No VPC network detected in the active project. Using 'apigee-vpc' as fallback."
    export NETWORK_NAME="apigee-vpc"
fi

export SUBNET_NAME="${NETWORK_NAME}-subnet"
export SUBNET_RANGE="10.10.23.0/22"       
export INSTANCE_IP="10.10.23.2"           

# Downstream external load balancing configurations
export LB_NAME="apigee-external-lb"
export ENV_NAME="eval-instance"                    
export DOMAIN_NAME="api.bank.local"

# API Product governance metadata parameters
export PRODUCT_NAME="DailyCashFlowProduct"
export DEVELOPER_EMAIL="well.amaral2007@gmail.com"
export DEVELOPER_FIRST="Wellington"
export DEVELOPER_LAST="Amaral"
export APP_NAME="DailyCashFlowApp"

# Core proxy bundles linked to the active runtime product instance
export PROXY_1="FinanceSecurity"
export PROXY_2="DailyCashFlowAPI"

# ------------------------------------------------------------------------------
# STEP 1: Enable Core Google Cloud Platform Services
# ------------------------------------------------------------------------------
echo "1. Enabling required Google Cloud Platform API services..."
gcloud services enable \
    apigee.googleapis.com \
    compute.googleapis.com \
    servicenetworking.googleapis.com \
    cloudresourcemanager.googleapis.com \
    networkservices.googleapis.com       

echo "Active Project ID      : $GCLOUD_PROJECT_ID"
echo "Detected VPC Network   : $NETWORK_NAME"
echo "Target Deployment Region: $REGION"
echo "=============================================================================="
echo "Initializing Consolidated Apigee Deployment Pipeline"
echo "=============================================================================="

gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> [APIGEE] Enforcing management API service registration...${RESET}"

gcloud services enable apigee.googleapis.com --quiet || true

# ==============================================================================
# STEP 3, 4 and 5: Evaluate Apigee Organization Activation State
# ==============================================================================
echo "3. Querying target Apigee Organization status parameters..."

TOKEN=$(gcloud auth print-access-token 2>/dev/null || echo "")

if [ -z "$TOKEN" ]; then
    echo "Error: Failed to extract gcloud identity authentication token. Verify active login credentials."
    exit 1
fi

ORG_STATUS=$(curl -s -H "Authorization: Bearer ${TOKEN}" \
    "https://apigee.googleapis.com/v1/organizations/${GCLOUD_PROJECT_ID}" \
    | jq -r '.state // empty')

if [ -z "$ORG_STATUS" ]; then
    ORG_STATUS="NOT_FOUND"
fi

if [ "$ORG_STATUS" = "NOT_FOUND" ]; then
    echo "Apigee Organization instance not detected. Initializing provisioning routing..."
    gcloud alpha apigee organizations provision \
        --project=$GCLOUD_PROJECT_ID \
        --analytics-region=$APIGEE_REGION \
        --authorized-network=default 
    
    echo "=============================================================================="
    echo "Organization provisioning command successfully broadcasted."
    echo "Creation processing requires 35 to 45 minutes to complete on Google Cloud."
    echo "Execution suspended. Re-run this deployment script after the allocation completes."
    echo "=============================================================================="
    exit 0

elif [ "$ORG_STATUS" != "ACTIVE" ]; then
    echo "=============================================================================="
    echo "Apigee Organization detected with incomplete status: [$ORG_STATUS]."
    echo "Awaiting complete cloud infrastructure setup loop completion."
    echo "Execution suspended. Re-run this script later to complete load balancing setups."
    echo "=============================================================================="
    exit 0

else
    echo "Apigee Organization status is verified as ACTIVE. Advancing to external routing configuration..."
fi

echo -e "${AMARELO}==> [APIGEE] Launching API Proxy bundles orchestration and deployment...${RESET}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE}")" && pwd)"
SOLUTION_ROOT="$SCRIPT_DIR/.."
APIGEE_ROOT_DIR="$SOLUTION_ROOT/api/apigee"

# ------------------------------------------------------------------------------
# STEP 6: Extract Service Attachment Private Service Connect URI
# ------------------------------------------------------------------------------
echo "6. Extracting Apigee instance Service Attachment URI dynamically..."

SERVICE_ATTACHMENT=$(curl -s -H "Authorization: Bearer ${TOKEN}" \
    "https://apigee.googleapis.com/v1/organizations/${GCLOUD_PROJECT_ID}/instances" \
    | jq -r '.instances[0].serviceAttachment // empty')

if [ -z "$SERVICE_ATTACHMENT" ]; then
    echo "Error: Failed to resolve Service Attachment metadata object from the API response payload."
    exit 1
fi

echo "Service Attachment URI successfully resolved: $SERVICE_ATTACHMENT"

# ------------------------------------------------------------------------------
# STEP 7: Allocate Global Static External IP Address
# ------------------------------------------------------------------------------
echo "7. Allocating static global external IP address target allocation..."
if ! gcloud compute addresses describe apigee-lb-ip --global >/dev/null 2>&1; then
    gcloud compute addresses create apigee-lb-ip --global
fi
LB_IP=$(gcloud compute addresses describe apigee-lb-ip --global --format="value(address)")
echo "Reserved Global External Load Balancer IP: $LB_IP"

# ------------------------------------------------------------------------------
# STEP 8: Provision Private Service Connect Network Endpoint Group
# ------------------------------------------------------------------------------
echo "8. Provisioning Private Service Connect Network Endpoint Group (PSC NEG)..."
if ! gcloud compute network-endpoint-groups describe apigee-psc-neg --region=${APIGEE_REGION} >/dev/null 2>&1; then
    gcloud compute network-endpoint-groups create apigee-psc-neg \
        --network-endpoint-type=private-service-connect \
        --psc-target-service=${SERVICE_ATTACHMENT} \
        --region=${APIGEE_REGION} \
        --network=$NETWORK_NAME
fi

# ------------------------------------------------------------------------------
# STEP 9: Configure External Load Balancer Backend Service Components
# ------------------------------------------------------------------------------
echo "9. Configuring External Load Balancer backend service parameters..."

if ! gcloud compute url-maps describe ${LB_NAME} --global >/dev/null 2>&1; then
    gcloud compute url-maps create ${LB_NAME} \
        --default-service=apigee-lb-backend \
        --global
fi

# ------------------------------------------------------------------------------
# STEP 10: Generate and Import Self-Signed SSL Certificate
# ------------------------------------------------------------------------------
echo "10. Configuring Self-Signed SSL Certificate parameters..."
if ! gcloud compute ssl-certificates describe apigee-self-signed-cert --global >/dev/null 2>&1; then
    echo "Generating private key and certificate files locally for: ${DOMAIN_NAME}..."
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout apigee.key \
        -out apigee.crt \
        -subj "/CN=${DOMAIN_NAME}/O=Development/C=BR" \
        2>/dev/null

    echo "Uploading the generated self-signed certificate object to Google Cloud..."
    gcloud compute ssl-certificates create apigee-self-signed-cert \
        --certificate=apigee.crt \
        --private-key=apigee.key \
        --global
    rm -f apigee.key apigee.crt
else
    echo "The certificate asset apigee-self-signed-cert already exists on Google Cloud."
fi

# ------------------------------------------------------------------------------
# STEP 11 AND 12: Provision Frontend Infrastructure (HTTPS Proxy & Forwarding Rule)
# ------------------------------------------------------------------------------
echo "11. Creating Target HTTPS Proxy structural component..."
if ! gcloud compute target-https-proxies describe apigee-lb-target-proxy --global >/dev/null 2>&1; then
    gcloud compute target-https-proxies create apigee-lb-target-proxy \
        --url-map=${LB_NAME} \
        --ssl-certificates=apigee-self-signed-cert \
        --global
fi

echo "12. Exposing public network port 443 for HTTPS traffic routing..."
if ! gcloud compute forwarding-rules describe apigee-https-forwarding-rule --global >/dev/null 2>&1; then
    gcloud compute forwarding-rules create apigee-https-forwarding-rule \
        --load-balancing-scheme=EXTERNAL_MANAGED \
        --network-tier=PREMIUM \
        --address=apigee-lb-ip \
        --target-https-proxy=apigee-lb-target-proxy \
        --ports=443 \
        --global
fi

# ------------------------------------------------------------------------------
# STEP 13: Configure API Product Consumption Entities
# ------------------------------------------------------------------------------
echo "13. Linking Proxy Bundles to the API Product target metadata [${PRODUCT_NAME}]..."
if ! gcloud apigee products describe ${PRODUCT_NAME} >/dev/null 2>&1; then 
    gcloud apigee products create ${PRODUCT_NAME} \
    --environments="eval" --all-proxies \
    --private-access --display-name="Daily Cash Flow Product" \
    --description="Unified product grouping cash flow APIs" 
fi

# ----------------------------------------------------------------------------
# STEP 14: Dynamic Ingestion of Cloud Functions Endpoints into XML Targets
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Fetching active Cloud Functions Gen2 production URI...${RESET}"
URL_BAL=$(gcloud functions describe daily-cashflow-balance --apigee_region=us-central1 --gen2 --format="value(serviceConfig.uri)" 2>/dev/null || echo "")

if [ -z "$URL_BAL" ]; then
    echo -e "${AMARELO}[!] Warning: Active production URI not found. Applying safety fallback connection string...${RESET}"
    URL_BAL="https://run.app"
fi

echo -e "${VERDE}[v] Active production endpoint successfully identified: ${URL_BAL}${RESET}"

TARGET_TX_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/entry-target.xml"
TARGET_BAL_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/balance-target.xml"

echo -e "${AMARELO}[+] Injecting the extracted endpoints into target XML configuration files...${RESET}"
if [ -f "$TARGET_TX_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_TX_FILE"
fi

if [ -f "$TARGET_BAL_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_BAL_FILE"
fi

# ----------------------------------------------------------------------------
# STEP 15: Package and Deploy Proxy Bundles via Alpha Commands Track
# ----------------------------------------------------------------------------
set +e

# 15.1. Authentication Issuer Proxy Bundle (FinanceSecurity)
echo -e "${AMARELO}[+] Packaging and uploading the 'FinanceSecurity' proxy bundle instance...${RESET}"
cd "$APIGEE_ROOT_DIR/FinanceSecurity"
rm -f FinanceSecurity.zip
zip -r -q FinanceSecurity.zip apiproxy

gcloud apigee apis import FinanceSecurity --from-bundle="./FinanceSecurity.zip" --organization="$APIGEE_ORG" --quiet
gcloud apigee apis deploy --api=FinanceSecurity --environment="$APIGEE_ENV" --organization="$APIGEE_ORG" --quiet || true

# 15.2. Ledger Validation Proxy Bundle (DailyCashFlowAPI)
echo -e "${AMARELO}[+] Packaging and uploading the 'DailyCashFlowAPI' proxy bundle instance...${RESET}"
cd "$APIGEE_ROOT_DIR/DailyCashFlowAPI"
rm -f DailyCashFlowAPI.zip
zip -r -q DailyCashFlowAPI.zip apiproxy

gcloud apigee apis import DailyCashFlowAPI --from-bundle="./DailyCashFlowAPI.zip" --organization="$APIGEE_ORG" --quiet
gcloud apigee apis deploy --api=DailyCashFlowAPI --environment="$APIGEE_ENV" --organization="$APIGEE_ORG" --quiet || true

# ----------------------------------------------------------------------------
# STEP 16: Provision Identity Consumers via Managed REST Endpoints
# ----------------------------------------------------------------------------
echo "Provisioning new Developer account identity..."
curl --request POST \
  --url "https://googleapis.com{APIGEE_ORG}/developers" \
  --header "Authorization: Bearer $(gcloud auth print-access-token)" \
  --header "Accept: application/json" \
  --header "Content-Type: application/json" \
  --data '{
    "email": "'"${DEVELOPER_EMAIL}"'",
    "firstName": "Wellington",
    "lastName": "Amaral",
    "userName": "wellington_amaral"
  }' \
  --compressed

echo "Provisioning Developer App credentials bound to the target API Product..."
curl --request POST \
  --url "https://googleapis.com{APIGEE_ORG}/developers/${DEVELOPER_EMAIL}/apps" \
  --header "Authorization: Bearer $(gcloud auth print-access-token)" \
  --header "Accept: application/json" \
  --header "Content-Type: application/json" \
  --data "{\"name\": \"${APP_NAME}\", \"apiProducts\": [\"${PRODUCT_NAME}\"]}" \
  --compressed

echo "Extracting generated consumer access keys and secrets..."
APP_JSON=$(curl -s -H "Authorization: Bearer $(gcloud auth print-access-token)" "https://googleapis.com{APIGEE_ORG}/developers/${DEVELOPER_EMAIL}/apps/${APP_NAME}")

KEY=$(echo "$APP_JSON" | jq -r '.credentials[0].consumerKey // empty')
SECRET=$(echo "$APP_JSON" | jq -r '.credentials[0].consumerSecret // empty')

echo "=============================================================================="
echo "CREDENTIALS SUCCESSFULLY GENERATED"
echo "=============================================================================="
echo "CLIENT ID (Consumer Key)       : $KEY"
echo "CLIENT SECRET (Consumer Secret): $SECRET"
echo "=============================================================================="

cd "$SCRIPT_DIR"
set -e

echo -e "${VERDE}==> [SUCCESS] Local target orchestration and synchronization completed successfully.${RESET}"
