# Portal +Compras — Roteiro de Demonstração Executiva

> Base funcional: Portal +Compras (frontend), commit `8ee8f4e`, branch `feature/a13-procurement-vertical-slice`.
> Este roteiro cobre apenas o que está realmente implementado e conectado ao backend: o módulo **Fornecedores**. Os demais módulos do portal (Pedidos, Negociações, Indicadores, Agentes IA, Configurações) são telas demonstrativas e devem ser apresentados como tal — não simulam persistência real.

## Objetivo

Demonstrar, em sequência, a experiência completa do módulo de fornecedores dentro do novo portal +Compras:

- visão geral do portal (navegação, identidade visual AZZAS 2154 / GDT Design System);
- cadastro de fornecedor;
- consulta CNPJ em fonte externa (BrasilAPI);
- enriquecimento inteligente (comparação campo a campo entre o cadastro atual e o dado retornado pela consulta);
- aprovação humana seletiva das divergências (nunca automática);
- integração com o ERP (dados de origem ERP, quando aplicável, e proteção do campo `NomeFantasia` controlado pelo Linx).

## Fluxo da apresentação

```
Dashboard → Fornecedores → Cadastro → Consulta CNPJ → Comparação → Aprovação → Fornecedor salvo
```

1. **Dashboard (AppShell)**
   Abrir o portal pela navegação principal (AppShell / shell de navegação React Router). Mostrar rapidamente os módulos disponíveis no menu, deixando claro que apenas "Fornecedores" está conectado à API real; os demais são visão de roadmap.

2. **Fornecedores → Cadastro (FornecedoresPage / CadastroFornecedor.tsx)**
   Abrir a tela de cadastro de fornecedor. Explicar que o formulário aceita `Cnpj_Cpf` alfanumérico (até 14 caracteres, compatível com o padrão Linx/CGC_CPF).

3. **Consulta CNPJ (CnpjSearch)**
   Informar um documento e disparar a consulta externa (BrasilAPI). Mostrar o retorno bruto da fonte externa: razão social, nome fantasia, endereço, situação cadastral, dados de contato.

4. **Comparação (SupplierComparison.tsx)**
   Mostrar a tela de divergências campo a campo entre o que já está cadastrado (ou o rascunho criado) e o que veio da consulta. Destacar que `NomeFantasia` e `Cnpj_Cpf` aparecem como campos protegidos e não são pré-selecionados para aprovação automática.

5. **Aprovação (ApprovalPanel.tsx)**
   Selecionar os campos divergentes desejados e demonstrar os dois caminhos: **Aceitar** (grava apenas os campos selecionados) e **Rejeitar** (descarta a sugestão para os campos escolhidos). Se o CNPJ consultado retornar situação cadastral `Baixada`, `Suspensa` ou `Inapta`, mostrar o alerta e a caixa de confirmação obrigatória antes de liberar a decisão.

6. **Fornecedor salvo**
   Voltar à listagem/detalhe do fornecedor e mostrar que apenas os campos aprovados foram persistidos, com o registro de auditoria (fonte, data/hora, `CorrelationId`).

## Massa de dados recomendada para a demo

### Cenário 1 — Fornecedor novo

Informar um `Cnpj_Cpf` de um CNPJ real e ativo → consultar → preencher/revisar os dados retornados → cadastrar.

> Exemplo **ilustrativo apenas de formato** (não usar como dado real de apresentação sem validar antes): `00.000.000/0001-91`. Prefira um CNPJ real, ativo e público (ex.: de uma empresa conhecida) para garantir retorno de dados coerente da BrasilAPI durante a demo.

### Cenário 2 — Fornecedor existente com divergência

Selecionar um fornecedor já cadastrado no +Compras cujos dados no ERP/cadastro atual divirjam do que a consulta CNPJ externa retorna (ex.: endereço desatualizado, telefone diferente) → executar a consulta → abrir a tela de comparação → aprovar alguns campos divergentes e rejeitar outros, para evidenciar o controle humano seletivo.

### Cenário 3 — Fornecedor com situação cadastral irregular

Utilizar um CNPJ cuja situação cadastral retornada pela consulta seja `Baixada`, `Suspensa` ou `Inapta` → mostrar que o alerta de atenção aparece na tela de aprovação → demonstrar que o usuário pode marcar a confirmação explícita e seguir com o cadastro mesmo assim (a regra alerta, mas não bloqueia).

**Importante:** não é necessário criar dados no banco antes da demo. Os três cenários acima podem ser executados ao vivo durante a apresentação, desde que o backend esteja no ar e a consulta externa (BrasilAPI) esteja acessível.

## Pontos de atenção durante a demo

- O ambiente do frontend precisa ter `VITE_API_BASE_URL` apontando para a API correta (ou depender do proxy do Vite em desenvolvimento, conforme `vite.config.ts`).
- O backend precisa estar em execução com CORS liberado para a origem usada pelo navegador da demo (ver `Cors:AllowedOrigins` em `appsettings.Development.json` / variável de ambiente equivalente em outros ambientes — a origem é configurável, nunca fixa em produção).
- A consulta CNPJ depende do serviço externo `BrasilApiCnpjProvider` (BrasilAPI) — validar conectividade com a internet/whitelist de rede antes da apresentação, para evitar falha ao vivo por timeout ou indisponibilidade da fonte externa.
- O build e os testes do frontend foram validados neste ciclo (`tsc` + `vite build`, 4/4 testes). O backend (`dotnet build`/`dotnet test`) não foi validado neste ciclo por ausência do SDK .NET no ambiente de revisão — recomenda-se rodar a suíte localmente antes da apresentação para confirmar que nada regrediu no lado servidor.
