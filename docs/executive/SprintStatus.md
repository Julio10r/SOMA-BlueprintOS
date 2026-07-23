# Status da Sprint

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 15:26:33 UTC
- **Última atualização:** 2026-07-23

---

## Status da sprint mais recente

## Sprint A9 — Publication Engine

**Status:** Concluída

**Escopo:** Implementação do Publication Engine: geração automática de três documentos profissionais para apresentação (Relatório Executivo, Guia do Cliente, Guia de Engenharia), cada um em Markdown, HTML e PDF, publicados em `dist/{executive,client,engineering}/`. O conteúdo reaproveita integralmente os 19 geradores de documentação da Sprint A8 (nenhum dado fabricado); o Relatório Executivo acrescenta indicadores reais de build/testes coletados em tempo real (`dotnet build` + contagem de `[Fact]`/`[Theory]`) e dívidas técnicas/próximos passos extraídos diretamente de `.ai/memory/known_issues.md` e `.ai/ROADMAP.md`.

**Entregas:**
- Módulo `Publication` em `backend/src/BlueprintOS.Core/Publication/` (Contracts + Models, incluindo o modelo comum `ContentBlock`/`InlineSpan` e o modelo rico `PublicationMetadata`/`PublicationAssets`/`PublicationTheme`) e `backend/src/BlueprintOS.Infrastructure/Publication/` (Content + Rendering + Publishers + orquestrador), seguindo o mesmo padrão dos módulos `Documentation`/`Knowledge`.
- Modelo comum (ViewModel) único por documento: Markdown bruto dos geradores é convertido uma única vez em `ContentBlock`s (`MarkdownContentParser`); os três renderizadores (`MarkdownRenderer`, `HtmlRenderer`, `PdfRenderer` via `QuestPDF`) consomem exatamente os mesmos blocos, sem duplicar lógica de interpretação — nenhum deriva HTML→PDF.
- `PublicationDocument` evoluído para documentos ricos: `Metadata` (autor, empresa, classificação, tags, histórico de revisões), `Assets` (imagens, logos, ícones SVG, gráficos, Mermaid, anexos, QR Codes, selos — cada um com suporte nativo de renderização nos três formatos) e `Theme` (paleta de cores por tipo de documento: executivo/cliente/engenharia). Suporte nativo funcional implementado para imagens/logos/ícones embutidos, anexos copiados para `dist/{categoria}/attachments/`, QR Codes gerados em tempo real (`QRCoder`, sem `System.Drawing`) e selos de build/testes/warnings renderizados localmente a partir de `QualityMetrics` real.
- Três publicadores de relatório (`ExecutivePublisher`, `ClientPublisher`, `EngineeringPublisher`), orquestrados por `PublicationService`; novos formatos (Word, PowerPoint, site estático) podem ser adicionados implementando apenas `IContentRenderer`, sem alterar os publicadores.
- `QualityMetricsProvider`, que coleta build status, warnings, erros e quantidade de testes em tempo real (sem valores fabricados), agora exibidos também como selos na capa do Relatório Executivo.
- Ponto único de entrada `dotnet run -- publish` em `backend/src/BlueprintOS.Api/Program.cs`, que resolve a raiz do repositório via `.git` para funcionar independente do diretório de execução.
- ADR-0007 (modelo comum de renderização) e ADR-0008 (documento rico: Metadata/Assets/Appendix/Theme) registradas em `.ai/DECISIONS.md`. `dist/` adicionado ao `.gitignore` (artefato gerado, não versionado).
- Suíte de testes unitários (xUnit, fakes manuais) cobrindo o parser de conteúdo, o parser de ênfase inline, os três renderizadores (incluindo blocos de imagem, selos, apêndice e anexos), o gerador de QR Code, o modelo de assets, o orquestrador e a coleta de indicadores.

**Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando (167 testes unitários + 1 teste de integração); `dotnet run -- publish` executado com sucesso, gerando os 9 arquivos esperados em `dist/`, com selos, QR Code e histórico de versões visíveis nos três formatos do Relatório Executivo.
