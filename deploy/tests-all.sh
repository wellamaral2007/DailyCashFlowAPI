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

# 🚀 NAVEGAÇÃO DINÂMICA: Detecta o diretório do script (deploy) e acha a raiz da solução
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Guarda a pasta onde você estava antes para te devolver para ela no final
ORIGINAL_DIR="$(pwd)"

echo -e "${CYAN}Executing from: $SCRIPT_DIR${NC}"
echo -e "${CYAN}Solution root detected at: $SOLUTION_ROOT${NC}\n"

# Entra na raiz da solução para que o dotnet enxergue o arquivo .sln global
cd "$SOLUTION_ROOT" || { echo -e "${RED}✘ Erro ao acessar a raiz da solução! Abortando.${NC}"; exit 1; }

# 1. Restaura as dependências do NuGet e compila a solução consolidada
echo -e "${CYAN}[1/4] Restoring packages and building solution...${NC}"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✔ Solution compiled successfully in Release mode!${NC}"
else
    echo -e "${RED}✘ Compilation failed! Aborting test pipeline.${NC}"
    cd "$ORIGINAL_DIR" # Retorna para a sua pasta original antes de sair
    exit 1
fi

# 2. Executa os Testes Unitários de Domínio (Mocks e Regras de Negócio)
echo -e "\n${CYAN}[2/4] Running Unit Tests (DailyCashFlowApiClientUnitTests)...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~DailyCashFlowApiClientUnitTests"; then
    echo -e "${GREEN}✔ Unit Tests passed successfully!${NC}"
else
    echo -e "${RED}✘ Unit Tests failed!${NC}"
    cd "$ORIGINAL_DIR"
    exit 1
fi

# 3. Executa os Testes de Integração de DAL e Fluxo Consolidado (Banco/Redis/Timezone)
echo -e "\n${CYAN}[3/4] Running Integration Tests (DailyCashFlowIntegrationTests)...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~DailyCashFlowIntegrationTests"; then
    echo -e "${GREEN}✔ Integration Tests passed successfully!${NC}"
else
    echo -e "${RED}✘ Integration Tests failed!${NC}"
    cd "$ORIGINAL_DIR"
    exit 1
fi

# 4. Executa os Testes de Carga (NBomber) - Valida o alvo de 50 RPS e valores pt-BR com vírgula
echo -e "\n${CYAN}[4/4] Running NBomber Load Tests (CashFlowLoadTestsRunner - Target: 50 RPS)...${NC}"
if dotnet test --configuration Release --no-build --no-restore --filter "FullyQualifiedName~CashFlowLoadTestsRunner"; then
    echo -e "${GREEN}✔ Load Tests passed! Loss rate is within the 5% SLA threshold.${NC}"
else
    echo -e "${RED}✘ Load Tests failed! Loss rate exceeded 5% or GCP FaaS performance degraded.${NC}"
    cd "$ORIGINAL_DIR"
    exit 1
fi

echo -e "\n${GREEN}==================================================${NC}"
echo -e "${GREEN}    ✔ ALL PIPELINE TESTS PASSED SUCCESSFULLY!     ${NC}"
echo -e "${GREEN}==================================================${NC}"

# Retorna você exatamente para onde você estava no terminal (pasta deploy)
cd "$ORIGINAL_DIR"
exit 0
