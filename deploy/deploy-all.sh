#!/bin/bash
set -e

#export GOOGLE_CLOUD_PROJECT=$(gcloud config get-value project)

export GCLOUD_PROJECT_ID="finance-solution-2026"
gcloud config set project $GCLOUD_PROJECT_ID

echo "Start deploying Finance in project: $GOOGLE_CLOUD_PROJECT"

chmod +x setup-gcp.sh
chmod +x deploy-*.sh

./setup-gcp.sh

# Execução modularizada das camadas
./deploy-secrets.sh
./deploy-network.sh
./deploy-databases.sh
./deploy-pubsub.sh
./deploy-iam.sh
./deploy-cloud-functions.sh
./deploy-apigee.sh

echo "==> GCP Services Deployed with success!"
