#!/bin/bash
echo "==> Compilando e Publicando Cloud Functions..."

# GenerateDailyCashFlow
gcloud functions deploy generate-daily-cashflow \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point=DailyCashFlow.GenerateDailyCashFlow.Function \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --trigger-http \
    --allow-unauthenticated \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT

# ProcessDailyCashFlow
gcloud functions deploy process-daily-cashflow \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point=DailyCashFlow.ProcessDailyCashFlow.Function \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --trigger-topic=DailyCashFlowEntry \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT

# DailyCashFlowBalance
gcloud functions deploy daily-cashflow-balance \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point=DailyCashFlow.DailyCashFlowBalance.Function \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --trigger-http \
    --allow-unauthenticated \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT
