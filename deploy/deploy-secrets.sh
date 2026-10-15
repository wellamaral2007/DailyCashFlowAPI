#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Vault Ingestion)
# CONCEPTS:        Google Secret Manager V1 - Non-interactive Connection Injection
# ============================================================================
set -e

# Definição de cores para saída no terminal
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado do setup anterior
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Configurando Secret Manager para o projeto: ${GCLOUD_PROJECT_ID}...${RESET}"

# ----------------------------------------------------------------------------
# 📥 1. Connection String de Escrita (Mestre/Writer) - Consumida pelo CommandContext
# ----------------------------------------------------------------------------
SECRET_WRITE_DB="db-write-connection"
CONN_STR_WRITE="Server=GCP_CLOUD_SQL\\SQLEXPRESS;Database=DailyCashDb;User Id=sa;Password=StrongPassword123!;Trusted_Connection=True;TrustServerCertificate=True;"

echo -e "${AMARELO}[+] Criando e alimentando segredo: ${SECRET_WRITE_DB}...${RESET}"
gcloud secrets create $SECRET_WRITE_DB --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_WRITE" | gcloud secrets versions add $SECRET_WRITE_DB --data-file=- --quiet

# ----------------------------------------------------------------------------
# 📤 2. Connection String de Leitura (Réplica/ReadOnly) - Consumida pelo QueryContext
# ----------------------------------------------------------------------------
SECRET_READ_DB="db-read-connection"
CONN_STR_READ="Server=GCP_CLOUD_SQL_REPLICA\\SQLEXPRESS;Database=DailyCashDb;User Id=sa;Password=StrongPassword123!;Trusted_Connection=True;TrustServerCertificate=True;"

echo -e "${AMARELO}[+] Criando e alimentando segredo: ${SECRET_READ_DB}...${RESET}"
gcloud secrets create $SECRET_READ_DB --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_READ" | gcloud secrets versions add $SECRET_READ_DB --data-file=- --quiet

# ----------------------------------------------------------------------------
# 🧠 3. String de Conexão do Cache de Acesso Rápido - Consumida pela StackExchange.Redis
# ----------------------------------------------------------------------------
SECRET_REDIS="redis-connection"
CONN_STR_REDIS="10.0.0.3:6379,ssl=false,abortConnect=false"

echo -e "${AMARELO}[+] Criando e alimentando segredo: ${SECRET_REDIS}...${RESET}"
gcloud secrets create $SECRET_REDIS --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_REDIS" | gcloud secrets versions add $SECRET_REDIS --data-file=- --quiet

echo -e "${VERDE}==> [SUCESSO] Secret Manager configurado e populado com os dados informados!${RESET}"
