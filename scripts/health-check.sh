#!/usr/bin/env bash
# Verifica o estado atual do backend (API .NET) e do frontend (Vite),
# sem iniciar ou parar nada. Uso: ./scripts/health-check.sh
set -uo pipefail

BACKEND_HEALTH_URL="http://localhost:5262/health"
FRONTEND_URL="http://127.0.0.1:5173"

log() { printf '[health-check] %s\n' "$1"; }

EXIT_CODE=0

log "Verificando backend em $BACKEND_HEALTH_URL..."
if body="$(curl -sS "$BACKEND_HEALTH_URL" 2>/dev/null)"; then
    if printf '%s' "$body" | grep -q '"status":"Healthy"'; then
        log "Backend: OK (200, status Healthy)"
    else
        log "Backend: respondeu, mas sem status Healthy no corpo: $body"
        EXIT_CODE=1
    fi
else
    log "Backend: sem resposta em $BACKEND_HEALTH_URL (API nao esta rodando ou porta 8080 indisponivel)."
    EXIT_CODE=1
fi

log "Verificando frontend em $FRONTEND_URL..."
if status="$(curl -sS -o /dev/null -w '%{http_code}' "$FRONTEND_URL" 2>/dev/null)"; then
    if [ "$status" = "200" ]; then
        log "Frontend: OK (200)"
    else
        log "Frontend: respondeu com HTTP $status"
        EXIT_CODE=1
    fi
else
    log "Frontend: sem resposta em $FRONTEND_URL (nao esta rodando ou porta 5173 indisponivel)."
    EXIT_CODE=1
fi

exit "$EXIT_CODE"
