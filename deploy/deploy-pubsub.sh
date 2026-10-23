#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Messaging Ingestion)
# CONCEPTS:        Google Cloud Pub/Sub - Event Driven Architecture (EDA)
# ============================================================================
set -e

# Definição de cores para saída limpa no terminal
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado das etapas anteriores do pipeline
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Configurando Barramento de Mensageria Pub/Sub...${RESET}"

TOPIC_NAME="DailyCashFlowEntry"
SUBSCRIPTION_NAME="DailyCashFlowEntrySub"

# ----------------------------------------------------------------------------
# 📥 1. PROVISIONAMENTO: Tópico de Eventos (Pub/Sub Topic)
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Criando Tópico de Eventos: ${TOPIC_NAME}...${RESET}"
# O '|| true' e o '--quiet' garantem que o script seja idempotente e silencioso
gcloud pubsub topics create $TOPIC_NAME --quiet || true

# ----------------------------------------------------------------------------
# 📥 2. PROVISIONAMENTO: Assinatura de Consumo (Pub/Sub Subscription)
# ----------------------------------------------------------------------------
echo -e "${AMARELO}[+] Criando Assinatura de Escuta: ${SUBSCRIPTION_NAME}...${RESET}"
# Cria uma assinatura do tipo Pull associada ao tópico para o nosso Event Consumer
gcloud pubsub subscriptions create $SUBSCRIPTION_NAME \
    --topic=$TOPIC_NAME \
    --ack-deadline=60 \
    --quiet || true

echo -e "${VERDE}==> [SUCESSO] Tópicos e Assinaturas do Pub/Sub configurados com êxito!${RESET}"
