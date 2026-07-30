# Documentação de API (Cliente)

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 21:04:12 UTC
- **Última atualização:** 2026-07-30

---

## API para clientes e integradores

A API pública do BlueprintOS ainda está em estágio inicial. Além do endpoint
de saúde, há um fluxo consultivo de recomendação de negociação:

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | Retorna o status de saúde da aplicação. |
| POST | `/api/v1/negociacoes/recomendacoes` | Retorna recomendação consultiva; não altera estado e exige decisão humana. |

A identidade temporária só é aceita em Development; fora desse ambiente a operação
falha de forma segura. Não há persistência, ERP ou execução automática de compras.
