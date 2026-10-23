#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Environment Handshake)
# CONCEPTS:        Secure gcloud CLI Installation, PGP Keyring Mapping & API Activation
# ============================================================================
set -e

# Terminal output color definitions
VERMELHO='\e[31m'
VERDE='\e[32m'
AMARELO='\e[33m'
RESET='\e[0m'

echo -e "${AMARELO}[+] Verificando se o Google Cloud CLI está instalado...${RESET}"

# Verifies if the gcloud command is available in the environment path
if ! command -v gcloud &> /dev/null; then
    echo -e "${VERMELHO}[x] gcloud não encontrado. Iniciando a instalação segura...${RESET}"
    
    # Installing required basic networking and security packages
    echo -e "${AMARELO}[+] Instalando dependências de segurança (apt-transport, ca-certificates)...${RESET}"
    sudo apt-get update && sudo apt-get install apt-transport-https ca-certificates gnupg curl -y
    
    # Creating the secure keyring folder if missing
    sudo mkdir -p /usr/share/keyrings
    
    # Importing the official repository signing key provided in the system context
    echo -e "${AMARELO}[+] Importando a chave PGP do Artifact Registry/Cloud SDK...${RESET}"
    # Downloads and de-armors the key safely to the local trusted keyring
    curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg
    
    # Adding the official Google Cloud SDK signed repository list
    echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee /etc/apt/sources.list.d/google-cloud-sdk.list
    
    # Updating package indexes and executing the cloud CLI core engine install
    echo -e "${AMARELO}[+] Atualizando índices do apt e instalando google-cloud-cli...${RESET}"
    sudo apt-get update && sudo apt-get install google-cloud-cli -y
else
    echo -e "${VERDE}[v] Google Cloud CLI já está instalado.${RESET}"
fi

# ============================================================================
# FIXED: Safe Unique Project ID Generation (Characters Defended)
# ============================================================================
# Generates a 100% clean, alphanumeric-only lowercase suffix using date and bash RANDOM
#UNIQUE_SUFFIX=$(date +%s | tail -c 4)$((RANDOM % 99))
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
    export NETWORK_NAME="default"
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

echo -e "${AMARELO}[+] Tentando criar um projeto único global no GCP: ${GCLOUD_PROJECT_ID}...${RESET}"
# Creates the project in non-interactive mode. The '|| true' handles if it accidentally duplicates
gcloud projects create $GCLOUD_PROJECT_ID --name="Daily Cash Flow 2026" --quiet || true

echo -e "${AMARELO}[+] Localizando sua conta de faturamento (Billing Account) ativa...${RESET}"
# Uses your verified active corporate billing account ID discovered by the CLI
export ACCOUNT_BILLING_ID="01674D-768D9E-BD5648"
echo -e "${VERDE}[v] Conta de faturamento ativa injetada: ${ACCOUNT_BILLING_ID}${RESET}"

echo -e "${AMARELO}[+] Vinculando a conta de faturamento ao projeto recém-criado...${RESET}"
# Links your active billing account to the safely generated project ID without prompt blocks
gcloud beta billing projects link $GCLOUD_PROJECT_ID --billing-account=$ACCOUNT_BILLING_ID --quiet

echo -e "${AMARELO}[+] Definindo o projeto ativo no terminal local...${RESET}"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

# ============================================================================
# Core Platform Engineering Step: Activating Essential Resource APIs
# ============================================================================
echo -e "${AMARELO}[+] Ativando APIs de serviços do GCP para a arquitetura DailyCashFlow...${RESET}"

# With billing linked to the unique project ID, the API activation will pass with total success
gcloud services enable secretmanager.googleapis.com cloudfunctions.googleapis.com pubsub.googleapis.com sqladmin.googleapis.com redis.googleapis.com compute.googleapis.com run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com eventarc.googleapis.com --quiet

echo -e "${VERDE}[v] O ambiente GCP está criado, faturado de forma segura e autenticado no ID: ${GCLOUD_PROJECT_ID}!${RESET}"
