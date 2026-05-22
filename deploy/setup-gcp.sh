#!/bin/bash

# Cores para saída no terminal
VERMELHO='\e[31m'
VERDE='\e[32m'
AMARELO='\e[33m'
RESET='\e[0m'

echo -e "${AMARELO}[+] Verificando se o Google Cloud CLI está instalado...${RESET}"

# Verifica se o comando gcloud existe
if ! command -v gcloud &> /dev/null; then
    echo -e "${VERMELHO}[x] gcloud não encontrado. Iniciando a instalação...${RESET}"
    
    # Adiciona o repositório e instala no Debian/Ubuntu
    echo -e "${AMARELO}[+] Adicionando repositório do Google Cloud...${RESET}"
    sudo apt-get install apt-transport-https ca-certificates gnupg curl -y
    
    # Adiciona a chave oficial e o repositório do Cloud SDK
    echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee -a /etc/apt/sources.list.d/google-cloud-sdk.list
    curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg
    
    # Atualiza pacotes e instala a CLI
    echo -e "${AMARELO}[+] Instalando o google-cloud-cli...${RESET}"
    sudo apt-get update && sudo apt-get install google-cloud-cli -y
else
    echo -e "${VERDE}[v] Google Cloud CLI já está instalado.${RESET}"
fi

# Autenticação
echo -e "${AMARELO}[+] Iniciando autenticação no Google Cloud...${RESET}"

# Executa o login interativo. 
# Se rodar em um servidor sem interface gráfica ou remoto, utilize: gcloud auth login --no-launch-browser
gcloud auth login

echo -e "${VERDE}[v] Autenticação concluída! O ambiente está pronto para rodar comandos.${RESET}"
