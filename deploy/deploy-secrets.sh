#!/bin/bash
echo "==> Configurando Secret Manager..."
gcloud secrets create db-user --replication-policy="automatic"
gcloud secrets create db-password --replication-policy="automatic"
gcloud secrets create db-schema --replication-policy="automatic"
gcloud secrets create redis-connection --replication-policy="automatic"

# Mock de dados iniciais para exemplo
echo -n "sa" | gcloud secrets versions add db-user --data-file=-
echo -n "StrongPassword123!" | gcloud secrets versions add db-password --data-file=-
echo -n "DailyCashDb" | gcloud secrets versions add db-schema --data-file=-
echo -n "10.0.0.3:6379" | gcloud secrets versions add redis-connection --data-file=-
