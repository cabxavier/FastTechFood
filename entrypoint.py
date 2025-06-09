import requests
import json
import time

ZABBIX_URL = "http://zabbix-web:8080/api_jsonrpc.php"
ZABBIX_USER = "Admin"
ZABBIX_PASS = "zabbix"
MAX_RETRIES = 60  # Até 5 minutos
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
        except Exception:
            pass
        print(f"🔁 Tentativa {i + 1}/{MAX_RETRIES} - Aguardando {WAIT_SECONDS}s...")
        time.sleep(WAIT_SECONDS)
    raise Exception("❌ Zabbix frontend não está acessível após várias tentativas.")


def zabbix_api(method, params, auth=None):
    payload = {
        "jsonrpc": "2.0",
        "method": method,
        "params": params,
        "id": 1,
        "auth": auth,
    }
    response = requests.post(ZABBIX_URL, headers=headers, data=json.dumps(payload))
    result = response.json()
    if "error" in result:
        raise Exception(f"Zabbix API error: {result['error']}")
    return result["result"]


wait_for_zabbix()

auth = zabbix_api("user.login", {"user": ZABBIX_USER, "password": ZABBIX_PASS})

# Host group
group_name = "Linux servers"
groups = zabbix_api("hostgroup.get", {"filter": {"name": [group_name]}}, auth)
group_id = groups[0]["groupid"] if groups else zabbix_api("hostgroup.create", {"name": group_name}, auth)["groupids"][0]

# Template
template_name = "Template OS Linux by Zabbix agent active"
templates = zabbix_api("template.get", {"filter": {"host": [template_name]}}, auth)
if not templates:
    raise Exception("Template not found.")
template_id = templates[0]["templateid"]

# Host
host_name = "Zabbix_server"
host_dns = "zabbix-agent2"

hosts = zabbix_api("host.get", {"filter": {"host": [host_name]}}, auth)
if not hosts:
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
    print("✅ Host Zabbix_server criado com template com sucesso.")
else:
    print("ℹ️ Host Zabbix_server já existe.")
