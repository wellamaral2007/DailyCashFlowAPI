#!/bin/bash

echo "==> Compilando e Publicando Cloud Functions..."

# Exit immediately if a command exits with a non-zero status
set -e

# 1. Define paths relative to the deploy folder
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$SCRIPT_DIR/.."
PROJECT_FRAMEWORK_DIR="$SOLUTION_ROOT/src/BaseFramework"
PROJECT_DIR_1="$SOLUTION_ROOT/src/GenerateDailyCashFlowMS"
PROJECT_DIR_2="$SOLUTION_ROOT/src/ProcessDailyCashFlow"
PROJECT_DIR_3="$SOLUTION_ROOT/src/DailyCashFlowBalance"
PUBLISH_FRAMEWORK_DIR_1="$PROJECT_FRAMEWORK_DIR/bin/Release/net8.0/publish" # Update net8.0 to your exact version
PUBLISH_DIR_1="$PROJECT_DIR_1/bin/Release/net8.0/publish" # Update net8.0 to your exact version
PUBLISH_DIR_2="$PROJECT_DIR_2/bin/Release/net8.0/publish" # Update net8.0 to your exact version
PUBLISH_DIR_3="$PROJECT_DIR_3/bin/Release/net8.0/publish" # Update net8.0 to your exact version



echo "🛠️ Compiling framework project to DLL..."
cd "$PROJECT_FRAMEWORK_DIR"
dotnet publish -c Release


# 3. Deploy to Google Cloud Functions
echo "🚀 Build and Deploying DLL artifacts to GCP..."

# GenerateDailyCashFlow
# 3.1 Navigate to project directory and build/publish the DLLs
echo "🛠️ Compiling framework project to DLL..."

cd "$PROJECT_DIR_1"
cp "$PUBLISH_FRAMEWORK_DIR_1/BaseFramework.dll" "$PUBLISH_DIR_1/BaseFramework/"
dotnet publish -c Release
gcloud functions deploy generate-daily-cashflow \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point="GenerateDailyCashFlowMS.CashFlow.Controller.GenerateDailyCashFlowController" \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --trigger-http \
    --allow-unauthenticated \
    --source="$PUBLISH_DIR_1" \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT

# ProcessDailyCashFlow
echo "🛠️ Compiling project to DLL..."
cd "$PROJECT_DIR_2"
cp "$PUBLISH_FRAMEWORK_DIR_1/BaseFramework.dll" "$PUBLISH_DIR_2/BaseFramework/"
dotnet publish -c Release
gcloud functions deploy process-daily-cashflow \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point="ProcessDailyCashFlow.CashFlow.Controller.ProcessDailyCashFlowController" \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --trigger-topic=DailyCashFlowEntry \
    --source="$PUBLISH_DIR_2" \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT

# DailyCashFlowBalance
echo "🛠️ Compiling project to DLL..."
cd "$PROJECT_DIR_3"
cp "$PUBLISH_FRAMEWORK_DIR_1/BaseFramework.dll" "$PUBLISH_DIR_3/BaseFramework/"
dotnet publish -c Release
gcloud functions deploy daily-cashflow-balance \
    --gen2 \
    --runtime=dotnet8 \
    --region=us-central1 \
    --entry-point="DailyCashFlowBalance.CashFlow.Controller.DailyCashFlowBalanceController" \
    --vpc-connector=projects/$GOOGLE_CLOUD_PROJECT/locations/us-central1/connectors/cashflow-vpc-connector \
    --source="$PUBLISH_DIR_3" \
    --trigger-http \
    --allow-unauthenticated \
    --set-env-vars GCP_PROJECT=$GOOGLE_CLOUD_PROJECT


