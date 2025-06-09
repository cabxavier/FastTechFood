#!/bin/sh

METRIC_NAME=$1
METRICS_URL="http://fasttechfoodapi:5000/metrics"

# Valida se o nome da métrica foi fornecido
if [ -z "$METRIC_NAME" ]; then
  echo "No metric name provided"
  exit 1
fi

# Faz o curl para o endpoint /metrics e extrai o valor da métrica
curl -s "$METRICS_URL" | grep "^$METRIC_NAME" | head -n 1 | awk '{print $2}'
