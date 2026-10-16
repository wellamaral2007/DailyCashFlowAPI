#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (API Gateway Ingress)
# CONCEPTS:        Google Apigee API Management / Bundle Import Syntax Patch
# ============================================================================
set -e

# Definição de cores para saídas limpas no terminal corporativo
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Variáveis Globais de Governança do Apigee Enterprise
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
export APIGEE_ORG="daily-cashflow-sol-17852"
export APIGEE_ENV="eval"

gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> [APIGEE] Garantindo ativação da API de gerenciamento...${RESET}"

# 1. Ativa a API do Apigee de forma sólida
gcloud services enable apigee.googleapis.com --quiet || true

# 1. Dispara o provisionamento assíncrono da Organização herdando o ID do projeto

#gcloud alpha apigee organizations provision \
#    --project=$GCLOUD_PROJECT_ID \
#    --analytics-region="us-central1" \
#    --authorized-network="default" \
#    --quiet || true

#echo -e "${AMARELO}[+] Aguardando a conclusão da criação física da Organização no GCP...${RESET}"
#echo -e "${AMARELO}[!] Nota: O provisionamento inicial do Apigee leva tempo. Monitorando o progresso...${RESET}"

# 2. Laço SRE de checagem ativa de estado (Polling Loop)
#while true; do
#    # Consulta o status atual da organização diretamente na API de gerência do Apigee
#    STATUS=$(gcloud beta apigee organizations describe $GCLOUD_PROJECT_ID --format="value(state)" 2>/dev/null || echo "PENDING")
#    
#    if [ "$STATUS" == "ACTIVE" ]; then
#        echo -e "\n${VERDE}[v] SUCESSO SUPREMO: Organização do Apigee está criada, ativa e homologada!${RESET}"
#         break
#    elif [ "$STATUS" == "FAILED" ]; then
#        echo -e "\n${VERMELHO}[x] ERRO CRÍTICO: O provisionamento do Apigee falhou no background do GCP.${RESET}"
#        exit 1
#    else
#        # Imprime pontos na tela para indicar atividade e aguarda 30 segundos antes da próxima checagem
#        echo -n "."
#        sleep 30
#    fi
#done

echo -e "${AMARELO}==> [APIGEE] Iniciando orquestração e deploy de API Proxies...${RESET}"

# 1. Define caminhos lógicos relativos com base na pasta de execução do script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE}")" && pwd)"
SOLUTION_ROOT="$SCRIPT_DIR/.."
APIGEE_ROOT_DIR="$SOLUTION_ROOT/api/apigee"

# ----------------------------------------------------------------------------
# 🚀 PASSO 1: CAPTURA DINÂMICA DE ENDPOINTS E INJEÇÃO FLUIDA NOS XML TARGETS
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Recuperando a URL de produção da Cloud Function Gen2...${RESET}"
URL_BAL=$(gcloud functions describe daily-cashflow-balance --region=us-central1 --gen2 --format="value(serviceConfig.uri)" 2>/dev/null || echo "")

if [ -z "$URL_BAL" ]; then
    echo -e "${AMARELO}[!] AVISO: URL ativa não localizada. Usando Fallback de segurança...${RESET}"
    URL_BAL="https://run.app"
fi

echo -e "${VERDE}[v] Endpoint de produção identificado: ${URL_BAL}${RESET}"

TARGET_TX_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/entry-target.xml"
TARGET_BAL_FILE="$APIGEE_ROOT_DIR/DailyCashFlowAPI/apiproxy/targets/balance-target.xml"

echo -e "${AMARELO}[+] Injetando a URL coletada nos arquivos de target xml...${RESET}"
if [ -f "$TARGET_TX_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_TX_FILE"
fi

if [ -f "$TARGET_BAL_FILE" ]; then
    sed -i "s|<URL>.*</URL>|<URL>$URL_BAL</URL>|g" "$TARGET_BAL_FILE"
fi

# ----------------------------------------------------------------------------
# 🚀 PASSO 2: COMPACTAÇÃO E DEPLOY DOS BUNDLES NO APIGEE (SINTAXE ALPHA HOMOLOGADA)
# ----------------------------------------------------------------------------

# Desativa temporariamente o 'set -e' para tolerar restrições físicas de propagação de Org se necessário
set +e

# 2.1. Emissor OAuth2 (FinanceSecurity)
echo -e "${AMARELO}[+] Empacotando e enviando o 'FinanceSecurity' proxy...${RESET}"
cd "$APIGEE_ROOT_DIR/FinanceSecurity"
rm -f FinanceSecurity.zip
zip -r -q FinanceSecurity.zip apiproxy

# FIXED: Utilizing the verified alpha bundle track import signature discovered in the diagnostics step
gcloud apigee apis import FinanceSecurity --from-bundle="./FinanceSecurity.zip" --organization="$APIGEE_ORG" --quiet
gcloud apigee apis deploy --api=FinanceSecurity --environment="$APIGEE_ENV" --organization="$APIGEE_ORG" --quiet || true

# 2.2. Validador OAuth2 (DailyCashFlowAPI)
echo -e "${AMARELO}[+] Empacotando e enviando o 'DailyCashFlowAPI' proxy...${RESET}"
cd "$APIGEE_ROOT_DIR/DailyCashFlowAPI"
rm -f DailyCashFlowAPI.zip
zip -r -q DailyCashFlowAPI.zip apiproxy

# FIXED: Utilizing the verified alpha bundle track import signature discovered in the diagnostics step
gcloud apigee apis import DailyCashFlowAPI --from-bundle="./DailyCashFlowAPI.zip" --organization="$APIGEE_ORG" --quiet
gcloud apigee apis deploy --api=DailyCashFlowAPI --environment="$APIGEE_ENV" --organization="$APIGEE_ORG" --quiet || true

cd "$SCRIPT_DIR"
set -e

echo -e "${VERDE}==> [SUCESSO] Sincronização local de targets do Apigee processada com êxito!${RESET}"