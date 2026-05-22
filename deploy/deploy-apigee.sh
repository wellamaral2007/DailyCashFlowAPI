#!/bin/bash
echo "==> Implantando APIs no Apigee..."
# Passos conceituais automatizados via Apigee Management API utilitária
# Como o CLI do gcloud interage nativamente via comandos apigee:

# 1. Importar Proxies baseados nos arquivos OpenAPI YAML compilados
gcloud apigee apis create daily-cashflow-proxy --api-spec=../api/openapi-dailycashflow.yaml
gcloud apigee apis create finance-security-proxy --api-spec=../api/openapi-financesecurity.yaml

# 2. Nota operacional sobre políticas internas que devem ser acopladas via XML no Bundle gerado:
# Para o proxy 'finance-security-proxy', injeta-se uma política <GenerateOAuthV2> validando os headers 'user' (apiUser) e 'password' (apiPassword).
# Para o proxy 'daily-cashflow-proxy', injeta-se a política <VerifyOAuthV2> apontando para o endpoint do emissor acima.

# 3. Deploy dos ambientes
gcloud apigee environments deploy --environment=eval --api=daily-cashflow-proxy
gcloud apigee environments deploy --environment=eval --api=finance-security-proxy
echo "==> APIs implantadas com sucesso no Apigee!"

#!/bin/bash
echo "==> Compactando e Implantando Bundles do Apigee..."

export APIGEE_ORG="gcp-finance-apigee"
export APIGEE_ENV="eval"

# 1. Implantar FinanceSecurity
cd ../api/apigee/FinanceSecurity
zip -r FinanceSecurity.zip apiproxy
gcloud apigee apis deploy --environment=$APIGEE_ENV --api=FinanceSecurity --file=FinanceSecurity.zip --org=$APIGEE_ORG
cd ../../

# 2. Implantar DailyCashFlowAPI
cd api/apigee/DailyCashFlowAPI
zip -r DailyCashFlowAPI.zip apiproxy
gcloud apigee apis deploy --environment=$APIGEE_ENV --api=DailyCashFlowAPI --file=DailyCashFlowAPI.zip --org=$APIGEE_ORG
cd ..

echo "==> Proxies do Apigee implantados e protegidos com OAuth2 com sucesso."
