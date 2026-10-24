#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Serverless Deployment)
# CONCEPTS:        Google Cloud Functions Gen2 / Unified Monorepo Source Build
# ============================================================================
set -e

# Definição de cores para saídas limpas no terminal corporativo
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado das etapas anteriores do pipeline
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Iniciando Publicação das Cloud Functions no Google Cloud Functions Gen2...${RESET}"

# 1. Define caminhos lógicos relativos com base no diretório atual do script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$SCRIPT_DIR/.."
SRC_ROOT="$SOLUTION_ROOT/src" # 📌 Raiz que engloba BaseFramework, DailyCashFlowBalanceMS e o novo projeto daily

# Identidades e topologias de segurança de rede e IAM da companhia
SA_EMAIL="sa-dailycashflow@${GCLOUD_PROJECT_ID}.iam.gserviceaccount.com"
VPC_CONNECTOR="projects/${GCLOUD_PROJECT_ID}/locations/us-central1/connectors/cashflow-vpc-connector"
TOPIC_NAME="DailyCashFlowEntry"

cd ../src/DailyCashFlowBalanceMS

# Remove de forma agressiva todos os binários antigos locais
rm -rf bin/ obj/
rm -rf ../BaseFramework/bin/ ../BaseFramework/obj/
rm -rf ../Tests/bin/ ../Tests/obj/

cd ../../deploy

# ============================================================================
# 🚀 PASSO DE DEPLOY: SHIPPANDO CODIGO FONTE PARA O GOOGLE CLOUD BUILD
# ============================================================================

echo -e "${AMARELO}[+] Enviando a malha de fontes do src/ para o Cloud Build...${RESET}"

gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/secretmanager.secretAccessor"

# ----------------------------------------------------------------------------
# 1️⃣ FUNCTION: daily-cashflow-balance (Gatilho HTTP POST/GET)
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Publicando FaaS: daily-cashflow-balance (HTTP Ingress/Egress)...${RESET}"
gcloud functions deploy daily-cashflow-balance \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point="DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Inbound.API.Controller.DailyCashFlowBalanceController" \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,GCP_PROJECT=daily-cashflow-sol-17852,GCP_TOPIC_ID=DailyCashFlowEntry" \
    --set-secrets="DB_WRITE_CONNECTION=db-write-connection:latest,DB_READ_CONNECTION=db-read-connection:latest,REDIS_CONNECTION=redis-connection:latest" \
    --vpc-connector="$VPC_CONNECTOR" \
    --ingress-settings=all \
    --egress-settings=all \
    --timeout=120s \
    --trigger-http \
    --source="$SRC_ROOT" \
    --service-account="$SA_EMAIL" \
    --set-build-env-vars GOOGLE_BUILDABLE="DailyCashFlowBalanceMS/DailyCashFlowBalanceMS.csproj" \
    --quiet

# 1️⃣ Vincula o Proxy seguro do Cloud SQL à instância da sua Function [11.21]
gcloud run services update daily-cashflow-balance \
    --add-cloudsql-instances="$GCLOUD_PROJECT_ID:us-central1:finance-mysql-master" \
    --region="us-central1" \
    --project=$GCLOUD_PROJECT_ID

# 2️⃣ Garante o roteamento interno de todo o tráfego pela malha da VPC (Para enxergar o IP do Redis) [11.11, 11.21]
gcloud run services update daily-cashflow-balance \
    --vpc-connector="cashflow-vpc-connector" \
    --vpc-egress="all-traffic" \
    --region="us-central1" \
    --project=$GCLOUD_PROJECT_ID

# ----------------------------------------------------------------------------
# 2️⃣ FUNCTION: process-daily-cashflow (Gatilho Reativo de Tópico Pub/Sub - EDA)
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Publicando FaaS: process-daily-cashflow (Pub/Sub Consumer)...${RESET}"
# FIXED: GOOGLE_BUILDABLE pointed to the new consolidated "daily/daily.csproj" workspace path
gcloud functions deploy process-daily-cashflow \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point="DailyCashFlowBalanceMS.CashFlow.Technology.Middleware.Inbound.EDA.Controller.ProcessDailyCashFlowController" \
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production,GCP_PROJECT=daily-cashflow-sol-17852,GCP_TOPIC_ID=DailyCashFlowEntry" \
    --set-secrets="DB_WRITE_CONNECTION=db-write-connection:latest,DB_READ_CONNECTION=db-read-connection:latest,REDIS_CONNECTION=redis-connection:latest" \
    --vpc-connector="$VPC_CONNECTOR" \
    --egress-settings=all \
    --timeout=120s \
    --trigger-topic="$TOPIC_NAME" \
    --source="$SRC_ROOT" \
    --service-account="$SA_EMAIL" \
    --set-build-env-vars GOOGLE_BUILDABLE="DailyCashFlowBalanceMS/DailyCashFlowBalanceMS.csproj" \
    --quiet

# 1️⃣ Vincula o Proxy seguro do Cloud SQL à instância da sua Function 
gcloud run services update daily-cashflow-balance \
    --add-cloudsql-instances="$GCLOUD_PROJECT_ID:us-central1:finance-mysql-master" \
    --region="us-central1" \
    --project=$GCLOUD_PROJECT_ID

# 2️⃣ Garante o roteamento interno de todo o tráfego pela malha da VPC (Para enxergar o IP do Redis) 
gcloud run services update process-daily-cashflow \
    --vpc-connector="cashflow-vpc-connector" \
    --vpc-egress="all-traffic" \
    --region=us-central1 \
    --project=$GCLOUD_PROJECT_ID

# 🚀 Autoriza o agente oficial do Pub/Sub do projeto a invocar funções protegidas do Cloud Run [20]
gcloud projects add-iam-policy-binding $GCLOUD_PROJECT_ID \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/run.invoker"

gcloud run services add-iam-policy-binding process-daily-cashflow \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/run.invoker" \
    --region="us-central1" \
    --project=$GCLOUD_PROJECT_ID

echo -e "${VERDE}==> [SUCESSO] Ambas as Google Cloud Functions publicadas e interligadas com sucesso total!${RESET}"
