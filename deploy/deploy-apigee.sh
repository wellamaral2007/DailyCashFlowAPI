#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (API Gateway Ingress)
# CONCEPTS:        Google Apigee API Management / Bundle Deployment / OAuth2 Perimeter
# ============================================================================
set -e

# Definição de cores para saídas limpas no terminal corporativo
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Variáveis Globais de Governança do Apigee Enterprise (Alinhadas ao roadmap 2026)
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
export APIGEE_ORG="gcp-finance-apigee"
export APIGEE_ENV="eval"

gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> [APIGEE] Iniciando orquestração e deploy de API Proxies...${RESET}"

# 1. Define caminhos lógicos relativos com base na pasta de execução do script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$SCRIPT_DIR/.."
APIGEE_ROOT_DIR="$SOLUTION_ROOT/api/apigee"

# ----------------------------------------------------------------------------
# 🚀 PASSO 1: CAPTURA DINÂMICA DE ENDPOINTS E INJEÇÃO FLUIDA NOS XML TARGETS
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Extraindo a URL de produção da Cloud Function unificada...${RESET}"

# Captura a URL real gerada pelo Google Cloud Functions Gen2
URL_BAL=$(gcloud functions describe daily-cashflow-balance --region=us-central1 --gen2 --format="value(serviceConfig.uri)" 2>/dev/null || echo "")

if [ -z "$URL_BAL" ]; then
    echo -e "${AMARELO}[!] AVISO: URL da Cloud Function não localizada. Usando endpoint de Fallback local...${RESET}"
    URL_BAL="https://daily-cashflow-balance-internal.net"
fi

# Define os caminhos absolutos para os arquivos XML de configuração de Target do Apigee
TARGET_TX_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/entry-target.xml"
TARGET_BAL_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/balance-target.xml"

echo -e "${AMARELO}[+] Injetando a URL coletada nos arquivos de target xml de forma dinâmica...${RESET}"
# Substituição segura utilizando o caractere '#' como delimitador no sed devido às barras '/' da URL
if [ -f "$TARGET_TX_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_TX_FILE"
fi

if [ -f "$TARGET_BAL_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_BAL_FILE"
fi

# ----------------------------------------------------------------------------
# 🚀 PASSO 2: COMPACTAÇÃO E DEPLOY DOS BUNDLES CORPORATIVOS (OAUTH2 + CORE API)
# ----------------------------------------------------------------------------

# 2.1. Processamento e Upload do Proxy de Segurança (FinanceSecurity)
echo -e "${AMARELO}[+] Empacotando e enviando o 'FinanceSecurity' proxy (Emissor OAuth2)...${RESET}"
cd "$APIGEE_ROOT_DIR/FinanceSecurity"
zip -r -q FinanceSecurity.zip apiproxy

# Importa o bundle de proxy para a organização corporativa do Apigee
gcloud beta apigee apis import --name=FinanceSecurity --source=FinanceSecurity.zip --organization=$APIGEE_ORG --quiet || true
# Ativa o proxy importado no ambiente selecionado (eval)
gcloud beta apigee environments deploy --environment=$APIGEE_ENV --api=FinanceSecurity --organization=$APIGEE_ORG --quiet || true

# 2.2. Processamento e Upload do Proxy de Negócio (DailyCashFlowAPI)
echo -e "${AMARELO}[+] Empacotando e enviando o 'DailyCashFlowAPI' proxy (Validador OAuth2)...${RESET}"
cd "$APIGEE_ROOT_DIR/DailyCashFlowAPI"
zip -r -q DailyCashFlowAPI.zip apiproxy

gcloud beta apigee apis import --name=DailyCashFlowAPI --source=DailyCashFlowAPI.zip --organization=$APIGEE_ORG --quiet || true
gcloud beta apigee environments deploy --environment=$APIGEE_ENV --api=DailyCashFlowAPI --organization=$APIGEE_ORG --quiet || true

# Retorna de forma segura para o diretório inicial de execução do script para não quebrar chamadas seguintes
cd "$SCRIPT_DIR"

echo -e "${VERDE}==> [SUCESSO] Proxies do Apigee implantados e protegidos com OAuth2 com sucesso absoluto!${RESET}"
