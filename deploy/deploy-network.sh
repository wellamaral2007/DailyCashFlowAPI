#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Network Topology)
# CONCEPTS:        Serverless VPC Access Connector & Private Services Peering
# ============================================================================
set -e

# Definição de cores para o terminal
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado das etapas anteriores
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Configurando Rede Privada e Conector VPC para: ${GCLOUD_PROJECT_ID}...${RESET}"

# 1. Habilitar as APIs de redes e conexões necessárias no ecossistema
echo -e "${AMARELO}[+] Ativando APIs de VPC e Acesso de Serviços...${RESET}"
gcloud services enable vpcaccess.googleapis.com servicenetworking.googleapis.com --quiet

# 2. Criar o Serverless VPC Access Connector na rede 'default'
# Permite que as Cloud Functions ganhem um IP interno dentro da rede VPC
echo -e "${AMARELO}[+] Criando Serverless VPC Access Connector (cashflow-vpc-connector)...${RESET}"
gcloud compute networks vpc-access connectors create cashflow-vpc-connector \
    --region=us-central1 \
    --network=default \
    --range=10.8.0.0/28 --quiet || true

echo -e "${AMARELO}==> Configurando Acesso Privado a Serviços (Para Cloud SQL e Redis)...${RESET}"

# 3. Aloca um bloco de IP interno exclusivo para a infraestrutura gerenciada do Google
echo -e "${AMARELO}[+] Alocando faixa de IPs internos para VPC Peering...${RESET}"
gcloud compute addresses create google-managed-services-default \
    --global \
    --purpose=VPC_PEERING \
    --addresses=10.10.0.0 \
    --prefix-length=16 \
    --network=default --quiet || true

# 4. Cria o emparelhamento (Peering) de rede com o ecossistema interno da Google
echo -e "${AMARELO}[+] Estabelecendo conexão de VPC Peering com o Service Networking...${RESET}"
gcloud services vpc-peerings connect \
    --service=servicenetworking.googleapis.com \
    --ranges=google-managed-services-default \
    --network=default --quiet || true

echo -e "${VERDE}==> [SUCESSO] Rede Privada e Conector VPC configurados com sucesso!${RESET}"
