# MCP do Design System AZZAS 2154

Servidor MCP local que expõe a documentação, tokens e componentes textuais em
`docs/design-system`.

## Execução

Na raiz do repositório:

```bash
python3 mcp/design-system/server.py
```

O processo usa `stdio`, portanto normalmente é iniciado pelo cliente MCP, não
por um terminal interativo.

## Configuração do cliente

Use este trecho como base na configuração do cliente MCP:

```json
{
  "mcpServers": {
    "azzas-design-system": {
      "command": "python3",
      "args": [
        "/CAMINHO/ABSOLUTO/SOMA-BlueprintOS/mcp/design-system/server.py"
      ]
    }
  }
}
```

## Ferramentas disponíveis

- `design_system_overview`: regras gerais e fundamentos da marca.
- `list_design_system_files`: inventário dos arquivos textuais.
- `read_design_system_file`: leitura segura de um arquivo específico.
- `search_design_system`: busca por conceito, token ou componente.

O servidor não acessa a conta do Claude. Para isso, seria necessário uma API ou
exportação autorizada do conteúdo do projeto do Claude. Esta primeira versão
usa o design system que já está versionado neste repositório.
