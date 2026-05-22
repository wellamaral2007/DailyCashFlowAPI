#!/bin/bash
echo "==> Configurando Contas de Serviço e Permissões IAM..."

# 1. Criar a Service Account dedicada para o ecossistema DailyCashFlow
gcloud iam service-accounts create sa-dailycashflow \
    --description="Service Account para as Cloud Functions do DailyCashFlow" \
    --display-name="SA DailyCashFlow"

export SA_EMAIL="sa-dailycashflow@$GOOGLE_CLOUD_://gserviceaccount.com"

# 2. Conceder acesso para ler segredos no Secret Manager
gcloud projects add-iam-policy-binding $GOOGLE_CLOUD_PROJECT \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/secretmanager.secretAccessor"

# 3. Conceder acesso para publicar e assinar tópicos no Pub/Sub
gcloud projects add-iam-policy-binding $GOOGLE_CLOUD_PROJECT \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/pubsub.publisher"

gcloud projects add-iam-policy-binding $GOOGLE_CLOUD_PROJECT \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/pubsub.subscriber"

# 4. Conceder acesso para se conectar ao Cloud SQL (SQL Server)
gcloud projects add-iam-policy-binding $GOOGLE_CLOUD_PROJECT \
    --member="serviceAccount:$SA_EMAIL" \
    --role="roles/cloudsql.client"
