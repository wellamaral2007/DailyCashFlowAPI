#!/bin/bash
echo "==> Criando Instância SQL Server..."
gcloud sql instances create cashflow-sqlserver \
    --database-version=SQLSERVER_2019_STANDARD \
    --tier=db-custom-2-3750 \
    --region=us-central1 \
    --root-password="StrongPassword123!"


echo "==> Criando Banco de Dados Inicial..."
gcloud sql databases create DailyCashDb --instance=cashflow-sqlserver

echo "==> Criando Banco de Dados e Tabela... Será executado o script de criação de tabela no SQL Server no codigo do projeto."
# Nota: O gcloud não executa instruções DDL internas para SQL Server diretamente de forma nativa sem proxy,
# simulamos a query via sqlcmd caso haja conectividade externa ativa, ou utilize o Cloud SQL Studio.
# Comando SQL para execução:
# CREATE DATABASE DailyCashDb;
# USE DailyCashDb;
# CREATE TABLE DailyCashFlow (Id INT IDENTITY(1,1) PRIMARY KEY, Value DECIMAL(18,2) NOT NULL, Date DATE NOT NULL);

echo "==> Criando Redis Cache..."
gcloud redis instances create cashflow-redis \
    --size=1 \
    --region=us-central1 \
    --zone=us-central1-a \
    --redis-version=redis_6_x

echo "==> Configuração de bancos de dados concluída!"



