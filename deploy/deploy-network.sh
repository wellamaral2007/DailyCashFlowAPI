#!/bin/bash
echo "==> Configurando Rede Privada e Conector VPC..."

# 1. Habilitar as APIs de redes necessárias
gcloud services enable ://googleapis.com ://googleapis.com

# 2. Criar uma subnet dedicada ou usar o Serverless VPC Access Connector na rede 'default'
gcloud compute networks vpc-access connectors create cashflow-vpc-connector \
    --region=us-central1 \
    --network=default \
    --range=10.8.0.0/28

echo "==> Configurando Acesso Privado a Serviços (Para o Cloud SQL e Redis corporativos)"
# Aloca um bloco de IP interno para conexões privadas do GCP
gcloud compute addresses create google-managed-services-default \
    --global \
    --purpose=VPC_PEERING \
    --addresses=10.10.0.0 \
    --prefix-length=16 \
    --network=default

# Cria o emparelhamento (Peering) de rede com os serviços internos do Google
gcloud services vpc-peerings connect \
    --service=://googleapis.com \
    --ranges=google-managed-services-default \
    --network=default
echo "==> Rede Privada e Conector VPC configurados com sucesso!"