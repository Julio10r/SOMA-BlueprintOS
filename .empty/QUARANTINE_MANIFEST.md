# QUARANTINE_MANIFEST.md

Inventário item a item do conteúdo movido para `.empty/` durante a
reorganização física do repositório (ver
`docs/repository/RepositoryReorganization-Final.md`). Nenhum item aqui foi
apagado — apenas isolado por evidência de obsolescência/duplicação.

| ORIGINAL_PATH | REASON | REPLACED_BY | REFERENCES_FOUND | SAFE_TO_DELETE_LATER? |
|---|---|---|---|---|
| `_staging/backend_full.tar.gz` | Backup/tarball órfão do backend, sem consumidor identificado no repositório. | `applications/mais-compras/backend/` (fonte viva) | Nenhuma referência ativa em código, scripts ou docs. | Provavelmente sim, após confirmação do dono do backend de que o tarball não é necessário para nenhum processo de disaster recovery externo ao git. |
| `.ai/local-output/mb_prod_extra_web/**` | Saída bruta de execução de integração (CSV/JSON de precheck, execução e verificação Wise/Linx), gerada localmente, não é fonte de conhecimento canônica. | `.ai/context/`, `docs/operations/` (documentação canônica das integrações) | Nenhuma referência ativa em código ou docs vivos; é artefato de execução pontual. | Provavelmente sim, após confirmação do dono da integração Linx/Wise de que os dados não têm valor de auditoria retido. |
| `....` (arquivo de 0 bytes, nome literal `....`) | Arquivo vazio sem função identificável, provável artefato acidental (ex: redirecionamento de shell mal formado). | — | Nenhuma. | **Removido definitivamente** em 2026-08-27 após revisão humana (Repository Cleanup v1 — Fase 2): confirmado 0 bytes, tracked, sem referências funcionais ativas. |
| `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` | Snapshot temporário de discovery de conhecimento Linx (domínio Fornecedor/CNPJ), criado como armazenamento provisório aguardando ingestão. Todo conhecimento reutilizável foi extraído e classificado (`sourceType`/`source_ref`) na fonte estruturada canônica; a tabela de comparação seção-a-seção confirmou `STILL_MISSING = 0`. | `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json` (28 unidades, consumido via `context_paths` por `linx-erp-specialist-agent` e `linx-database-specialist-agent`); ver `docs/repository/LinxKnowledgeFornecedor-Ingestion.md`. | Nenhuma referência ativa em `agent.yaml`/`memory_paths`/`context_paths` — os agents Linx apontam apenas para o artefato gerado, nunca para o snapshot. | Provavelmente sim, após um ciclo de observação confirmando que nenhum consumidor voltou a depender do snapshot; mantido por ora como registro histórico do discovery. |

Itens **não movidos** apesar de riscos identificados na auditoria (ver
`docs/repository/RepositoryReorganization-Audit.md`):

- `.myNotes` — sinalizado por conter possível credencial em texto claro.
  **Não foi movido nem tocado** por ser um problema de segurança fora do
  escopo desta reorganização; requer ação humana direta (rotação de
  credencial + remoção/gitignore), não uma decisão de "para onde mover".
- `backend/backend/` (diretório aninhado duplicado, identificado na
  auditoria) — não foi encontrado no momento da execução física; pode já
  ter sido resolvido em commit anterior ou nunca ter existido como
  descrito. Nenhuma ação necessária.
