#!/usr/bin/env bash
# Sobe o ambiente de desenvolvimento do SOMA-BlueprintOS: API .NET local
# (banco corporativo MAISCOMPRAS/SOMA_DESENV via VPN, SQL Server externo,
# sem Docker) e o frontend (Vite) em segundo plano.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKEND_DIR="$ROOT_DIR/backend"
FRONTEND_DIR="$ROOT_DIR/frontend/web"
BACKEND_PID_FILE="$SCRIPT_DIR/.backend.pid"
BACKEND_LOG_FILE="$SCRIPT_DIR/.backend.log"
FRONTEND_PID_FILE="$SCRIPT_DIR/.frontend.pid"
FRONTEND_LOG_FILE="$SCRIPT_DIR/.frontend.log"
BACKEND_HEALTH_URL="http://localhost:5262/health"
FRONTEND_URL="http://127.0.0.1:5173"

log() { printf '[start-dev] %s\n' "$1"; }
fail() { printf '[start-dev] ERRO: %s\n' "$1" >&2; exit 1; }

log "Raiz do projeto: $ROOT_DIR"

if [ -f "$BACKEND_PID_FILE" ] && kill -0 "$(cat "$BACKEND_PID_FILE")" 2>/dev/null; then
    log "Backend ja esta em execucao (PID $(cat "$BACKEND_PID_FILE")). Nada a fazer."
else
    log "Iniciando o backend em segundo plano (dotnet run, perfil http, porta 5262)..."
    (cd "$BACKEND_DIR" && nohup dotnet run --project src/BlueprintOS.Api --launch-profile http > "$BACKEND_LOG_FILE" 2>&1 &
     echo $! > "$BACKEND_PID_FILE")
fi

log "Aguardando a API responder em $BACKEND_HEALTH_URL..."
BACKEND_OK=false
for _ in $(seq 1 30); do
    if response="$(curl -sS -o /dev/null -w '%{http_code}' "$BACKEND_HEALTH_URL" 2>/dev/null)"; then
        if [ "$response" = "200" ]; then
            BACKEND_OK=true
            break
        fi
    fi
    sleep 2
done

if [ "$BACKEND_OK" != "true" ]; then
    log "A API nao respondeu 200 em $BACKEND_HEALTH_URL a tempo. Ultimas linhas do log:"
    tail -n 40 "$BACKEND_LOG_FILE" 2>/dev/null || true
    fail "Backend nao subiu corretamente. Verifique VPN corporativa e as ConnectionStrings (user-secrets) do SQL Server externo (MaisComprasConnection/ErpConnection)."
fi

log "Backend saudavel (HTTP 200) em $BACKEND_HEALTH_URL."

if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
    log "node_modules nao encontrado em $FRONTEND_DIR. Executando npm install..."
    (cd "$FRONTEND_DIR" && npm install)
fi

if [ -f "$FRONTEND_PID_FILE" ] && kill -0 "$(cat "$FRONTEND_PID_FILE")" 2>/dev/null; then
    log "Frontend ja esta em execucao (PID $(cat "$FRONTEND_PID_FILE")). Nada a fazer."
else
    log "Iniciando o frontend em segundo plano (npm run dev)..."
    (cd "$FRONTEND_DIR" && nohup npm run dev > "$FRONTEND_LOG_FILE" 2>&1 &
     echo $! > "$FRONTEND_PID_FILE")
    log "Frontend iniciado (PID $(cat "$FRONTEND_PID_FILE")). Log em $FRONTEND_LOG_FILE."
fi

log ""
log "Ambiente de desenvolvimento no ar:"
log "  Backend:  $BACKEND_HEALTH_URL"
log "  Frontend: $FRONTEND_URL (pode levar alguns segundos para responder)"
log ""
log "Para parar tudo: ./scripts/stop-dev.sh"
log "Para checar o status a qualquer momento: ./scripts/health-check.sh"
