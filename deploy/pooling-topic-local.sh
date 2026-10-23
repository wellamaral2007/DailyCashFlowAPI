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
echo -e "${YELLOW}    GCP PUB/SUB BATCH PUSH EMULATOR (MAX 100 MSG / LOOP)       ${NC}"
echo -e "${YELLOW}================================================================${NC}\n"

# Verifica se o utilitário jq está instalado, crucial para processar lotes JSON
if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}⚠ Erro: O utilitário 'jq' é obrigatório para processar mensagens em lote.${NC}"
    echo -e "Instale rodando: sudo apt-get install jq"
    exit 1
fi

while true; do
    echo -e "${CYAN}[$(date +%T)] Polling for batch (max 100 messages) from GCP Pub/Sub...${NC}"

    # 1. Faz o pull estrito de até 100 mensagens limpando a fila com auto-ack automático
    RAW_MESSAGE_JSON=$(gcloud pubsub subscriptions pull "$SUBSCRIPTION" \
        --auto-ack \
        --limit=1000 \
        --format="json" \
        --project="$PROJECT_ID" 2>/dev/null)

    # Verifica se o GCP retornou um array de dados de fila válido
    if [ ! -z "$RAW_MESSAGE_JSON" ] && [ "$RAW_MESSAGE_JSON" != "[]" ]; then
        
        # Conta quantas mensagens vieram no lote atual usando o jq
        MSG_COUNT=$(echo "$RAW_MESSAGE_JSON" | jq '. | length')
        echo -e "${GREEN}📩 Lote capturado com $MSG_COUNT mensagem(ns)! Iniciando processamento...${NC}"

        # 2. Utiliza o loop do jq para iterar sobre cada mensagem capturada no lote
        # O jq -c extrai cada objeto por linha para podermos ler no laço 'while read'
        echo "$RAW_MESSAGE_JSON" | jq -c '.[]' | while read -r item; do
            
            # Extrai a string Base64 do campo data da mensagem atual
            BASE64_DATA=$(echo "$item" | jq -r '.message.data')

            if [ ! -z "$BASE64_DATA" ] && [ "$BASE64_DATA" != "null" ]; then
                
                echo -e "\n${GREEN}----------------------------------------------------------------${NC}"
                echo -e "${GREEN}📩 PROCESSING MESSAGE CONTENT:${NC}"
                echo -e "${GREEN}----------------------------------------------------------------${NC}"
                echo "$BASE64_DATA" | base64 --decode | json_pp 2>/dev/null || echo "$BASE64_DATA" | base64 --decode
                echo -e "${GREEN}----------------------------------------------------------------${NC}"

                # Gera metadados dinâmicos imitando o robô do Google Cloud de produção
                MESSAGE_ID="msg-$(cat /proc/sys/kernel/random/uuid | cut -c1-8)"
                PUBLISH_TIME=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")

                # 3. Monta o envelope oficial de infraestrutura do GCP para a mensagem do lote
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
                
                # 4. Dispara o POST HTTP/1.1 contra a sua porta de debug
                curl -s -X POST "$TARGET_FUNCTION_URL" \
                     -H "Content-Type: application/json" \
                     -d "$GCP_PUSH_ENVELOPE" &

                echo -e "${GREEN}✔ Event synchronized with local Controller!${NC}"
            fi
        done
        wait # Aguarda todos os curls terminarem antes de continuar o loop
        
        echo -e "${GREEN}✔ Todo o lote de $MSG_COUNT mensagens foi processado com sucesso!${NC}\n"
    else
        echo -e "✔ Queue is empty. Sleeping... [11.21]"
    fi

    # 5. Tempo de descanso rápido de 0.2 segundos antes de varrer as próximas 100 mensagens
    #echo -e "Sleeping for 0.2 seconds...\n"
    #sleep 0.2
done
