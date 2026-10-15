# ============================================================================
# 📥 SEÇÃO 1: ARMAZENAMENTO DURÁVEL (DURABLE STORAGE - MYSQL 8.0)
# ============================================================================

# 1.1. Instância Mestre de Escrita (Primary Writer) - Consumida pelo CommandContext
echo -e "${AMARELO}[+] Garantindo Instância Mestre MySQL Privada (daily-cashflow-mysql)...${RESET}"
gcloud sql instances create daily-cashflow-mysql \
    --database-version=MYSQL_8_0 \
    --tier=db-custom-2-4096 \
    --region=us-central1 \
    --root-password="StrongPassword123!" \
    --no-assign-ip \
    --network=default \
    --enable-bin-log \
    --backup-start-time="03:00" \
    --quiet || true

# FIX FORCADO: Se a instância mestre já existia sem logs binários, este patch liga o recurso obrigatoriamente
echo -e "${AMARELO}[+] Aplicando patch de ativação de Bin-Log e Backups na Instância Mestre...${RESET}"
gcloud sql instances patch daily-cashflow-mysql \
    --enable-bin-log \
    --backup-start-time="03:00" \
    --quiet

# 1.2. Instância de Réplica de Leitura (Read Replica) - Consumida pelo QueryContext
echo -e "${AMARELO}[+] Criando Réplica de Leitura MySQL Privada (daily-cashflow-mysql-replica)...${RESET}"
# Agora que o mestre possui obrigatoriamente os logs binários ativos, a replicação passará com sucesso total
gcloud sql instances create daily-cashflow-mysql-replica \
    --master-instance-name=daily-cashflow-mysql \
    --tier=db-custom-2-4096 \
    --region=us-central1 \
    --no-assign-ip \
    --network=default \
    --quiet || true
