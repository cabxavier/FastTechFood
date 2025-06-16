import requests
import json
import time
import os
import sys

ZABBIX_URL = os.getenv("ZABBIX_URL", "http://zabbix-web:8080/api_jsonrpc.php")
ZABBIX_USER = os.getenv("ZABBIX_USER", "Admin")
ZABBIX_PASS = os.getenv("ZABBIX_PASS", "zabbix")
MAX_RETRIES = 60
WAIT_SECONDS = 5

headers = {"Content-Type": "application/json-rpc"}


def wait_for_zabbix():
    print("⏳ Aguardando Zabbix frontend ficar disponível...")
    for i in range(MAX_RETRIES):
        try:
            r = requests.get(ZABBIX_URL, timeout=3)
            if r.status_code == 200:
                print("✅ Zabbix frontend disponível.")
                return
        except Exception as e:
            print(f"⚠️ Tentativa {i + 1}/{MAX_RETRIES} falhou: {e}")
        print(f"🔁 Tentativa {i + 1}/{MAX_RETRIES} - Aguardando {WAIT_SECONDS}s...")
        time.sleep(WAIT_SECONDS)
    print("❌ Zabbix frontend não está acessível após várias tentativas.")
    sys.exit(1)


def zabbix_api(method, params, auth=None):
    payload = {
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": 1,
        "auth": auth,
    }
    try:
        response = requests.post(ZABBIX_URL, headers=headers, data=json.dumps(payload), timeout=10)
        response.raise_for_status()
    except requests.RequestException as e:
        print(f"❌ Erro na requisição API Zabbix ({method}): {e}")
        sys.exit(1)

    try:
        result = response.json()
    except json.JSONDecodeError:
        print("❌ Resposta da API Zabbix não é um JSON válido.")
        sys.exit(1)

    if "error" in result:
        print(f"❌ Erro da API Zabbix ({method}): {result['error']}")
        sys.exit(1)

    return result.get("result")


def main():
    wait_for_zabbix()

    print("🔐 Autenticando no Zabbix API...")
    auth = zabbix_api("user.login", {"user": ZABBIX_USER, "password": ZABBIX_PASS})

    # Grupo de hosts
    group_name = "Linux servers"
    print(f"🔍 Buscando grupo de hosts '{group_name}'...")
    groups = zabbix_api("hostgroup.get", {"filter": {"name": [group_name]}}, auth)
    if groups:
        group_id = groups[0]["groupid"]
        print(f"✔ Grupo encontrado com ID {group_id}")
    else:
        print(f"➕ Grupo não encontrado, criando grupo '{group_name}'...")
        group_id = zabbix_api("hostgroup.create", {"name": group_name}, auth)["groupids"][0]
        print(f"✔ Grupo criado com ID {group_id}")

    # Template
    template_name = "Template OS Linux by Zabbix agent active"
    print(f"🔍 Buscando template '{template_name}'...")
    templates = zabbix_api("template.get", {"filter": {"host": [template_name]}}, auth)
    if not templates:
        print(f"❌ Template '{template_name}' não encontrado.")
        sys.exit(1)
    template_id = templates[0]["templateid"]
    print(f"✔ Template encontrado com ID {template_id}")

    # Host
    host_name = os.getenv("ZBX_HOSTNAME", "zabbix-agent2")
    host_dns = host_name
    print(f"🔍 Buscando host '{host_name}'...")
    hosts = zabbix_api("host.get", {"filter": {"host": [host_name]}}, auth)
    if not hosts:
        print(f"➕ Host '{host_name}' não encontrado, criando host...")
        zabbix_api("host.create", {
            "host": host_name,
            "name": host_name,
            "interfaces": [{
                "type": 1,
                "main": 1,
                "useip": 0,
                "ip": "",
                "dns": host_dns,
                "port": "10050"
            }],
            "groups": [{"groupid": group_id}],
            "templates": [{"templateid": template_id}]
        }, auth)
        print(f"✅ Host '{host_name}' criado com template com sucesso.")
    else:
        print(f"ℹ️ Host '{host_name}' já existe.")

if __name__ == "__main__":
    main()
