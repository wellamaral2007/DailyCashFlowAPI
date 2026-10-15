#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Vault Ingestion)
# CONCEPTS:        Google Secret Manager V1 - MySQL 8.0 Connection String Patch
# ============================================================================
set -e

# Definição de cores para saída limpa e visual no terminal corporativo
AMARELO='\e[33m'
VERDE='\e[32m'
RESET='\e[0m'

# Garante o contexto do projeto correto herdado das etapas anteriores do pipeline
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> Atualizando Secret Manager com as Connection Strings oficiais do MySQL 8.0...${RESET}"

# ----------------------------------------------------------------------------
# 📥 1. Connection String de Escrita (MySQL Master / Primary Writer)
# ----------------------------------------------------------------------------
SECRET_WRITE_DB="db-write-connection"
# Format standardized for MySQL ADO/.NET providers targeting the unique private master instance DNS
CONN_STR_WRITE="Server=mysql-master-17852.c.daily-cashflow-sol-17852.internal;Port=3306;Database=DailyCashDb;Uid=root;Pwd=StrongPassword123!;SslMode=None;"

echo -e "${AMARELO}[+] Injetando segredo de Escrita: ${SECRET_WRITE_DB}...${RESET}"
gcloud secrets create $SECRET_WRITE_DB --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_WRITE" | gcloud secrets versions add $SECRET_WRITE_DB --data-file=- --quiet

# ----------------------------------------------------------------------------
# 📤 2. Connection String de Leitura (MySQL Read Replica - CQRS Read Path)
# ----------------------------------------------------------------------------
SECRET_READ_DB="db-read-connection"
# Pointing securely to the dedicated MySQL replica instance to offload heavy aggregate read queries
CONN_STR_READ="Server=mysql-replica-17852.c.daily-cashflow-sol-17852.internal;Port=3306;Database=DailyCashDb;Uid=root;Pwd=StrongPassword123!;SslMode=None;"

echo -e "${AMARELO}[+] Injetando segredo de Leitura: ${SECRET_READ_DB}...${RESET}"
gcloud secrets create $SECRET_READ_DB --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_READ" | gcloud secrets versions add $SECRET_READ_DB --data-file=- --quiet

# ----------------------------------------------------------------------------
# 🧠 3. String de Conexão do Cache de Acesso Rápido (Redis Cluster)
# ----------------------------------------------------------------------------
SECRET_REDIS="redis-connection"
# Captura dinamicamente o IP interno privado da sua instância ativa do Cloud Memorystore Redis
echo -e "${AMARELO}[+] Capturando o IP interno da instância Redis (daily-cashflow-redis)...${RESET}"
REDIS_IP=$(gcloud redis instances describe daily-cashflow-redis --region=us-central1 --format="value(host)" 2>/dev/null || echo "10.0.0.3")

# Formato de connection string exigido pela biblioteca StackExchange.Redis
CONN_STR_REDIS="${REDIS_IP}:6379,ssl=false,abortConnect=false"

echo -e "${AMARELO}[+] Injetando segredo do Cache com IP dinâmico: ${SECRET_REDIS}...${RESET}"
gcloud secrets create $SECRET_REDIS --replication-policy="automatic" --quiet || true
echo -n "$CONN_STR_REDIS" | gcloud secrets versions add $SECRET_REDIS --data-file=- --quiet

echo -e "${VERDE}==> [SUCESSO] Secret Manager atualizado e sincronizado com os alvos do MySQL e Redis!${RESET}"
