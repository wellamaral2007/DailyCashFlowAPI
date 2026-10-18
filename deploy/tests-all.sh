#!/usr/bin/env bash

# Definição de cores para o terminal (ANSI Escape Codes)
GREEN='\033[0;32m'
RED='\033[0;31m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m' # Sem Cor (Reset)

echo -e "${YELLOW}==================================================${NC}"
echo -e "${YELLOW}    CASH FLOW API - AUTOMATED TEST SUITE          ${NC}"
echo -e "${YELLOW}==================================================${NC}\n"

# 1. Restaura as dependências do NuGet e compila a solução consolidada
echo -e "${CYAN}[1/4] Restoring packages and building solution...${NC}"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✔ Solution compiled successfully in Release mode!${NC}"
else
    echo -e "${RED}✘ Compilation failed! Aborting test pipeline.${NC}"
    exit 1
fi

# 2. Executa os Testes Unitários de Domínio
echo -e "\n${CYAN}[2/4] Running Unit Tests...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~UnitTests"; then
    echo -e "${GREEN}✔ Unit Tests passed successfully!${NC}"
else
    echo -e "${RED}✘ Unit Tests failed!${NC}"
    exit 1
fi

# 3. Executa os Testes de Integração de DAL (Mapeamentos EF Core / Sockets)
echo -e "\n${CYAN}[3/4] Running Integration Tests...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests"; then
    echo -e "${GREEN}✔ Integration Tests passed successfully!${NC}"
else
    echo -e "${RED}✘ Integration Tests failed!${NC}"
    exit 1
fi

# 4. Executa os Testes de Carga (NBomber) - Valida o alvo de 50 RPS e SLA do GCP FaaS
echo -e "\n${CYAN}[4/4] Running NBomber Load Tests (Target: 50 RPS)...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~LoadTests"; then
    echo -e "${GREEN}✔ Load Tests passed! Loss rate is within the 5% SLA threshold.${NC}"
else
    echo -e "${RED}✘ Load Tests failed! Loss rate exceeded 5% or GCP FaaS performance degraded.${NC}"
    exit 1
fi

echo -e "\n${GREEN}==================================================${NC}"
echo -e "${GREEN}    ✔ ALL PIPELINE TESTS PASSED SUCCESSFULLY!     ${NC}"
echo -e "${GREEN}==================================================${NC}"

exit 0
