#!/usr/bin/env bash

# Configurações do projeto e assinatura do GCP
PROJECT_ID="daily-cashflow-sol-17852"
SUBSCRIPTION="sub-debug-local"
TARGET_FUNCTION_URL="http://localhost:8081/"

# Cores para formatação de logs no terminal
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}================================================================${NC}"
echo -e "${YELLOW}    GCP PUB/SUB REALTIME PUSH EMULATOR (STRICT 2s LOOP)        ${NC}"
echo -e "${YELLOW}================================================================${NC}\n"

# Variável de controle para evitar a impressão de payloads idênticos em sequência imediata
LAST_PAYLOAD=""

while true; do
    echo -e "${CYAN}[$(date +%T)] Polling for single message from GCP Pub/Sub...${NC}"

    # 1. Faz o pull estrito de apenas 1 mensagem limpando a fila na mesma hora [11.21]
    RAW_MESSAGE_JSON=$(gcloud pubsub subscriptions pull "$SUBSCRIPTION" \
        --auto-ack \
        --limit=1 \
        --format="json" \
        --project="$PROJECT_ID" 2>/dev/null)

    # Verifica se o GCP retornou dados de fila válidos [11.21]
    if [ ! -z "$RAW_MESSAGE_JSON" ] && [ "$RAW_MESSAGE_JSON" != "[]" ]; then
        
        # 2. Extrai a string Base64 do campo data usando o sed nativo
        BASE64_DATA=$(echo "$RAW_MESSAGE_JSON" | sed -n 's/.*"data": *"\([^"]*\)".*/\1/p' | head -n 1)
        
        # Verifica se o payload é válido e se não é uma repetição fantasma imediata de buffer
        if [ ! -z "$BASE64_DATA" ] && [ "$BASE64_DATA" != "$LAST_PAYLOAD" ]; then
            LAST_PAYLOAD="$BASE64_DATA"

            echo -e "\n${GREEN}================================================================${NC}"
            echo -e "${GREEN}📩 ORIGINAL DECODED MESSAGE PAYLOAD (BUSINESS CONTENT):${NC}"
            echo -e "${GREEN}================================================================${NC}"
            echo "$BASE64_DATA" | base64 --decode | json_pp 2>/dev/null
            echo -e "${GREEN}================================================================${NC}\n"

            # Gera metadados dinâmicos imitando o robô do Google Cloud de produção
            MESSAGE_ID="msg-$(cat /proc/sys/kernel/random/uuid | cut -c1-8)"
            PUBLISH_TIME=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")

            # 3. Monta o envelope oficial de infraestrutura do GCP [11.11, 11.21]
            GCP_PUSH_ENVELOPE=$(cat <<EOF
{
  "subscription": "projects/$PROJECT_ID/subscriptions/$SUBSCRIPTION",
  "message": {
    "messageId": "$MESSAGE_ID",
    "publishTime": "$PUBLISH_TIME",
    "attributes": {},
    "data": "$BASE64_DATA"
  }
}
EOF
)

            echo -e "${YELLOW}Injecting envelope into local Kestrel Endpoint (Port 8081)...${NC}"
            
            # 4. Dispara o POST HTTP/1.1 contra a sua porta de debug [11.11]
            curl -s -X POST "$TARGET_FUNCTION_URL" \
                 -H "Content-Type: application/json" \
                 -d "$GCP_PUSH_ENVELOPE"

            echo -e "${GREEN}✔ Event synchronized with local Controller!${NC}\n"
        else
            # Se cair aqui, era uma mensagem repetida que o gcloud puxou do buffer antes do ack sincronizar
            echo -e "${YELLOW}⚠ Duplicate message skipped from buffer synchronization.${NC}"
        fi
    else
        echo -e "✔ Queue is empty. Sleeping... [11.21]"
    fi

    # 5. Força a trava obrigatória de tempo de descanso de 10 segundos antes do próximo ciclo
    echo -e "Sleeping for 2 seconds...\n"
    sleep 2
done
