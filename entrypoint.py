#!/usr/bin/env python3
import os
import time
import requests
import json

ZABBIX_URL = os.environ.get('ZABBIX_URL')  # ex: http://zabbix-web:8080
ZABBIX_USER = os.environ.get('ZABBIX_USER')
ZABBIX_PASS = os.environ.get('ZABBIX_PASS')
TEMPLATE_PATH = '/template.xml'

# Aguarda o Zabbix Web estar disponível
print(f"⏳ Aguardando Zabbix Web subir em {ZABBIX_URL}...")
while True:
    try:
        # Testa a home web, não a API aqui
        response = requests.get(ZABBIX_URL)
        if response.status_code == 200:
            break
    except requests.exceptions.ConnectionError:
        pass
    time.sleep(10)
print("✅ Zabbix Web está online!")

# Autentica na API do Zabbix
print("🔐 Autenticando na API do Zabbix...")
auth_payload = {
    "jsonrpc": "2.0",
    "method": "user.login",
    "params": {
        "user": ZABBIX_USER,
        "password": ZABBIX_PASS
    },
    "id": 1,
    "auth": None
}
auth_response = requests.post(f"{ZABBIX_URL}/api_jsonrpc.php", json=auth_payload)
auth_result = auth_response.json()

token = auth_result.get('result')
if not token:
    print("❌ Falha na autenticação.")
    print(json.dumps(auth_result.get('error', {}), indent=2))
    exit(1)
print("🔑 Token obtido com sucesso!")

# Lê o conteúdo do template
if not os.path.isfile(TEMPLATE_PATH):
    print(f"❌ Template {TEMPLATE_PATH} não encontrado.")
    exit(1)
with open(TEMPLATE_PATH, 'r') as file:
    template_content = file.read()

# Importa o template
print("📦 Importando template...")
import_payload = {
    "jsonrpc": "2.0",
    "method": "configuration.import",
    "params": {
        "format": "xml",
        "rules": {
            "applications": {"createMissing": True, "updateExisting": True},
            "discoveryRules": {"createMissing": True, "updateExisting": True},
            "graphs": {"createMissing": True, "updateExisting": True},
            "groups": {"createMissing": True},
            "hosts": {"createMissing": True, "updateExisting": True},
            "images": {"createMissing": True, "updateExisting": True},
            "items": {"createMissing": True, "updateExisting": True},
            "maps": {"createMissing": True, "updateExisting": True},
            "screens": {"createMissing": True, "updateExisting": True},
            "templateLinkage": {"createMissing": True},
            "templates": {"createMissing": True, "updateExisting": True},
            "triggers": {"createMissing": True, "updateExisting": True}
        },
        "source": template_content
    },
    "auth": token,
    "id": 2
}
import_response = requests.post(f"{ZABBIX_URL}/api_jsonrpc.php", json=import_payload)
import_result = import_response.json()
if 'error' in import_result:
    print("❌ Erro ao importar template:")
    print(json.dumps(import_result['error'], indent=2))
    exit(1)

print("✅ Template importado com sucesso!")
