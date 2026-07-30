# Documentação de API (Cliente)

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 18:24:18 UTC
- **Última atualização:** 2026-07-30

---

## API para clientes e integradores

A API pública do BlueprintOS ainda está em estágio inicial. Além da verificação
de saúde, há um primeiro fluxo consultivo de negociação do +COMPRAS:

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/health` | Retorna o status de saúde da aplicação. |
| POST | `/api/v1/negotiations/history` | Registra uma negociação concluída no histórico transitório. |
| GET | `/api/v1/negotiations/suppliers/{supplierId}` | Consulta o histórico consolidado de um fornecedor. |
| POST | `/api/v1/negotiations/recommendations` | Produz recomendação explicável; exige decisão humana. |

O histórico é perdido ao reiniciar a aplicação. Os endpoints não executam compras,
não integram ERP e ainda não possuem autenticação ou autorização.
