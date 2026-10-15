#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase G - Infrastructure Governance (Identity & Access)
# CONCEPTS:        Service Account Provisioning & RBAC Least Privilege Binding
# ============================================================================
set -e

# Definição de cores para uma saída limpa e visual no terminal
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado das etapas anteriores do pipeline
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Configurando Contas de Serviço e Permissões IAM para: ${GCLOUD_PROJECT_ID}...${RESET}"

# 1. Criar a Service Account dedicada para o ecossistema DailyCashFlow
echo -e "${AMARELO}[+] Criando Service Account (sa-dailycashflow)...${RESET}"
gcloud iam service-accounts create sa-dailycashflow \
    --description="Service Account para as Cloud Functions do DailyCashFlow" \
    --display-name="SA DailyCashFlow" --quiet || true

# FIXED: Reconstructed the Service Account corporate email formatting mapping string
export SA_EMAIL="sa-dailycashflow@${GCLOUD_PROJECT_ID}.iam.gserviceaccount.com"
echo -e "${VERDE}[v] E-mail da Service Account estruturado: ${SA_EMAIL}${RESET}"

# 2. Conceder acesso para ler segredos no Secret Manager
echo -e "${AMARELO}[+] Concedendo permissão de Secret Accessor (Cofre)...${RESET}"
gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/secretmanager.secretAccessor" --quiet

# 3. Conceder acesso para publicar e assinar tópicos no Pub/Sub (EDA Boundary)
echo -e "${AMARELO}[+] Concedendo permissões de Publisher e Subscriber no Pub/Sub...${RESET}"
gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/pubsub.publisher" --quiet

gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/pubsub.subscriber" --quiet

# 4. Conceder acesso para se conectar de forma privada ao Cloud SQL (MySQL 8.0)
echo -e "${AMARELO}[+] Concedendo permissão de Cloud SQL Client (Banco Durável)...${RESET}"
gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/cloudsql.client" --quiet

echo -e "${VERDE}==> [SUCESSO] Contas de Serviço e Permissões IAM vinculadas com sucesso total!${RESET}"
