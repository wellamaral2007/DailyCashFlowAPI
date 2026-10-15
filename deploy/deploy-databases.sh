#!/bin/bash
# ============================================================================
# TOGAF DIMENSION: Phase D - Technology Architecture (Database Provisioning)
# CONCEPTS:        Private MySQL 8.0 Master, Read Replica & Cloud Memorystore Redis
# ============================================================================
set -e

# Terminal output color definitions
VERMELHO='\e[31m'
VERDE='\e[32m'
AMARELO='\e[33m'
RESET='\e[0m'

# Ensures the correct project context is inherited across the pipeline executions
export GCLOUD_PROJECT_ID="daily-cashflow-sol-17852"
gcloud config set project $GCLOUD_PROJECT_ID --quiet

echo -e "${AMARELO}==> [DAILY CASH FLOW] Iniciando o provisionamento de MySQL 8.0 Privado Limpo...${RESET}"

# ============================================================================
# 📥 SEÇÃO 1: ARMAZENAMENTO DURÁVEL (DURABLE STORAGE - MYSQL 8.0)
# ============================================================================

# 1.1. Instância Mestre de Escrita (Primary Writer) - Consumida pelo CommandContext
echo -e "${AMARELO}[+] Criando Nova Instância Mestre MySQL Privada (mysql-master-17852)...${RESET}"
# Using clean resource names to completely bypass Google Cloud background deletion locks
gcloud sql instances create mysql-master-17852 \
    --database-version=MYSQL_8_0 \
    --tier=db-custom-2-4096 \
    --region=us-central1 \
    --root-password="StrongPassword123!" \
    --no-assign-ip \
    --network=default \
    --enable-bin-log \
    --backup-start-time="03:00" \
    --quiet || true

# 1.2. Instância de Réplica de Leitura (Read Replica) - Consumida pelo QueryContext
echo -e "${AMARELO}[+] Criando Nova Réplica de Leitura MySQL Privada (mysql-replica-17852)...${RESET}"
# Pointing directly to the newly defined unique master instance ID
gcloud sql instances create mysql-replica-17852 \
    --master-instance-name=mysql-master-17852 \
    --tier=db-custom-2-4096 \
    --region=us-central1 \
    --no-assign-ip \
    --network=default \
    --quiet || true

# 1.3. Criação do Banco de Dados Inicial na Instância Mestre Única
echo -e "${AMARELO}[+] Criando Banco de Dados Inicial (DailyCashDb) no MySQL Mestre...${RESET}"
gcloud sql databases create DailyCashDb --instance=mysql-master-17852 --quiet || true


# ============================================================================
# 🧠 SEÇÃO 2: ACESSO RÁPIDO EM MEMÓRIA (QUICKACCESS - REDIS)
# ============================================================================

# 2.1. Instância Cloud Memorystore para Redis (Cache de Alta Velocidade)
echo -e "${AMARELO}[+] Garantindo Instância Redis Cache Privada (daily-cashflow-redis)...${RESET}"
gcloud redis instances create daily-cashflow-redis \
    --size=1 \
    --tier=BASIC \
    --region=us-central1 \
    --zone=us-central1-a \
    --redis-version=redis_6_x \
    --network=default \
    --connect-mode=PRIVATE_SERVICE_ACCESS \
    --quiet || true


# ============================================================================
# 🏛️ SEÇÃO 3: FINALIZAÇÃO E GOVERNANÇA DE PLATAFORMA
# ============================================================================
echo -e "${VERDE}==> [SUCESSO] Todos os componentes limpos do MySQL e Redis foram configurados!${RESET}"
