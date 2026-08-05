#!/usr/bin/env bash
# Para o ambiente de desenvolvimento iniciado por scripts/start-dev.sh:
# encerra o backend (dotnet run) e o frontend (Vite), ambos em segundo plano.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_PID_FILE="$SCRIPT_DIR/.backend.pid"
FRONTEND_PID_FILE="$SCRIPT_DIR/.frontend.pid"
BACKEND_PORT=5262
FRONTEND_PORT=5173

log() { printf '[stop-dev] %s\n' "$1"; }

# "npm run dev"/"dotnet run" iniciam processos filhos: matar apenas o PID
# salvo deixa o processo real orfao, ainda ouvindo a porta. Por isso
# encerramos a arvore de processos inteira (filhos primeiro, depois o pai).
kill_tree() {
    local pid="$1"
    local child
    for child in $(pgrep -P "$pid" 2>/dev/null || true); do
        kill_tree "$child"
    done
    kill -TERM "$pid" 2>/dev/null || true
}

stop_pid_file() {
    local name="$1"
    local pid_file="$2"

    if [ -f "$pid_file" ]; then
        local pid
        pid="$(cat "$pid_file")"
        if kill -0 "$pid" 2>/dev/null; then
            log "Encerrando o $name (PID $pid) e seus processos filhos..."
            kill_tree "$pid"
            sleep 1
            kill -0 "$pid" 2>/dev/null && kill -9 "$pid" 2>/dev/null || true
        else
            log "Processo do $name (PID $pid) ja nao estava em execucao."
        fi
        rm -f "$pid_file"
    else
        log "Nenhum PID de $name registrado por start-dev.sh (se voce iniciou manualmente, pare pelo terminal onde roda)."
    fi
}

stop_pid_file "backend" "$BACKEND_PID_FILE"
stop_pid_file "frontend" "$FRONTEND_PID_FILE"

# Rede de seguranca: caso algum processo tenha sobrevivido (orfao) ainda
# ouvindo a porta do backend/frontend, encerra por porta em vez de deixar vazando.
if command -v lsof >/dev/null 2>&1; then
    for port in "$BACKEND_PORT" "$FRONTEND_PORT"; do
        leftover_pids="$(lsof -ti:"$port" 2>/dev/null || true)"
        if [ -n "$leftover_pids" ]; then
            log "Encerrando processo(s) orfao(s) ainda ouvindo a porta $port: $leftover_pids"
            # shellcheck disable=SC2086
            kill -9 $leftover_pids 2>/dev/null || true
        fi
    done
fi

log "Ambiente de desenvolvimento parado."
