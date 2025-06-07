#!/bin/bash

ZABBIX_URL="http://localhost:8080/api_jsonrpc.php"
ZABBIX_USER="Admin"
ZABBIX_PASS="zabbix"

# Função para autenticar e obter token
get_auth_token() {
  response=$(curl -s -X POST -H 'Content-Type: application/json' -d '{
    "jsonrpc":"2.0",
    "method":"user.login",
    "params":{
      "user":"'"${ZABBIX_USER}"'",
      "password":"'"${ZABBIX_PASS}"'"
    },
    "id":1,
    "auth":null
  }' ${ZABBIX_URL})

  # Verifica se a resposta contém erro
  if [[ $response == *"error"* ]]; then
    echo "Erro na autenticação: $response" >&2
    exit 1
  fi

  # Extrai o token
  echo $response | grep -oP '(?<="result":")[^"]+'
}

# Função para importar template
import_template() {
  local AUTH_TOKEN=$1
  local TEMPLATE_FILE="/zabbix-importer/template_fasttechfood_prometheus.xml"
  
  # Verifica se o arquivo de template existe
  if [ ! -f "$TEMPLATE_FILE" ]; then
    echo "Arquivo de template não encontrado: $TEMPLATE_FILE" >&2
    exit 1
  fi

  # Lê e formata o conteúdo do template
  local TEMPLATE_CONTENT=$(sed 's/"/\\"/g' "$TEMPLATE_FILE" | tr -d '\n')

  response=$(curl -s -X POST -H 'Content-Type: application/json' -d '{
    "jsonrpc":"2.0",
    "method":"configuration.import",
    "params":{
      "format":"xml",
      "rules":{
        "templates":{
          "createMissing":true,
          "updateExisting":true
        },
        "items":{
          "createMissing":true,
          "updateExisting":true
        },
        "discoveryRules":{
          "createMissing":true,
          "updateExisting":true
        }
      },
      "source":"'"${TEMPLATE_CONTENT}"'"
    },
    "auth":"'"${AUTH_TOKEN}"'",
    "id":2
  }' ${ZABBIX_URL})

  echo "$response"
}

echo "Aguardando Zabbix Web estar disponível..."
until curl -s --head --fail ${ZABBIX_URL} > /dev/null; do
  echo "Zabbix Web não disponível ainda, aguardando 5s..."
  sleep 5
done

echo "✅ Zabbix Web está online!"

echo "Autenticando no Zabbix..."
TOKEN=$(get_auth_token)

if [ -z "$TOKEN" ]; then
  echo "❌ Erro ao autenticar no Zabbix."
  exit 1
fi

echo "✅ Autenticação bem-sucedida."

echo "Importando template..."
RESULT=$(import_template "$TOKEN")

if [[ $RESULT == *"error"* ]]; then
  echo "❌ Erro na importação do template: $RESULT" >&2
  exit 1
else
  echo "✅ Template importado com sucesso: $RESULT"
fi