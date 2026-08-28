# Domain Principles

## Objetivo

Registrar os princípios permanentes do domínio de negócio do +Compras — regras que se aplicam a qualquer Onda, qualquer módulo e qualquer implementação futura, independentemente de quando ou por qual Work Order foram aprovadas. Este documento não registra histórico, não explica o raciocínio por trás de uma decisão e não copia o conteúdo de nenhuma ADR — apenas enuncia a regra permanente resultante.

## Hierarquia Documental

Em caso de conflito entre este documento e uma ADR (`.ai/DECISIONS.md`), **prevalece a ADR**. Este documento é uma consolidação de leitura rápida dos princípios já aprovados; a ADR é a fonte de decisão. Em caso de conflito entre este documento e `docs/product/ComprasFuncional.md`/`ComprasUX.md`/`ComprasDataModel.md`, os documentos de produto especificam o comportamento de tela/dado; este documento especifica a regra de domínio que esse comportamento deve respeitar — nenhum dos dois substitui o outro. Este documento não substitui `.ai/ARCHITECTURE.md` (arquitetura técnica de código) nem `docs/architecture/Architecture.md` (arquitetura de camadas/módulos) — trata exclusivamente de regras de domínio de negócio.

## Princípios da Plataforma

- O +Compras opera sempre no contexto de exatamente uma Unidade de Negócio por sessão (`UnidadeNegocioId`).
- Toda configuração e todo cadastro administrativo são multiempresa por padrão, mesmo quando uma única Unidade de Negócio está ativa em produção.
- Nenhuma funcionalidade crítica de operação (cadastrar ou selecionar fornecedor/item, criar pedido, enviá-lo ao ERP, acompanhar a integração) pode depender de disponibilidade de IA. IA acelera e orienta; nunca é pré-requisito.
- Nenhuma funcionalidade é considerada concluída sem evidência de implementação, validação e documentação correspondente — capacidade planejada, parcial e implementada nunca são tratadas como sinônimos.

## Dados Mestres

- O ERP é a fonte canônica de todo dado corporativo por ele controlado. Nenhum dado sincronizado do ERP é criado, alterado ou removido a partir do +Compras.
- O +Compras pode armazenar exclusivamente metadados locais que não pertencem ao ERP (ex.: descrição própria, flag de ativação local). Um metadado local nunca substitui, oculta ou reinterpreta o dado oficial do ERP.
- Toda tela que exiba um dado integrado do ERP distingue, de forma explícita e simultânea: o código de origem ERP, a descrição de origem ERP e a descrição própria do +Compras (quando existir).
- Ativar ou inativar um registro integrado no +Compras é uma operação exclusivamente local — nunca se propaga para o ERP.
- Classificações gerenciais que não existem no ERP (ex.: agrupamento de despesa por marca, corporativo ou localidade) são modeladas como conceito próprio do +Compras, nunca disfarçadas de cadastro de empresa jurídica.

## Administração

- Toda configuração administrativa e todo motor de regra de negócio (workflow, alçada, orçamento) pertencem ao domínio de uma Unidade de Negócio — nunca são globais por padrão.
- Configuração técnica/infraestrutural do ambiente (identidade federada, feature flags, integrações, observabilidade) é distinta de configuração de negócio da Unidade — as duas nunca compartilham a mesma área de navegação.
- Preferência pessoal do usuário autenticado (conta, tema, idioma) nunca é motor de regra de negócio da Unidade — é sempre escopo exclusivamente individual.
- Relacionamentos administrativos que restringem seleção (ex.: quais classificações gerenciais um centro de custo aceita) são sempre explícitos e configurados — nunca implícitos ou deduzidos em tempo de uso.
- Autorização de acesso de um usuário a um recurso operacional (ex.: a quais centros de custo ele pode operar) é sempre um vínculo separado do cadastro mestre desse recurso; conceder ou revogar esse acesso nunca altera o cadastro mestre.

## Cadastros Integrados

- Um cadastro integrado do ERP nunca é criado, editado ou excluído fisicamente a partir do +Compras — apenas ativado/inativado para uso local.
- Toda chave usada pelo ERP para identificar um registro (código, identificador externo) é persistida no +Compras exatamente como recebida, sem reinterpretação.
- Um cadastro integrado pode se relacionar com um conceito próprio do +Compras (ex.: classificação gerencial); essa relação é sempre modelada no +Compras, nunca escrita de volta no ERP.
- Nomes funcionais de telas de cadastro integrado descrevem gestão de disponibilidade local, nunca criação de dado mestre — a nomenclatura de uma tela nunca pode sugerir uma capacidade que ela não possui.

## Segurança

- Todo acesso de um usuário a uma permissão do sistema é mediado exclusivamente por Perfis. Um usuário nunca recebe permissão individual ou exceção direta.
- As permissões efetivas de um usuário são sempre a união das permissões de todos os Perfis a ele vinculados.
- Toda necessidade de comportamento de acesso diferente do já existente é resolvida pela criação de um novo Perfil — nunca por uma exceção pontual anexada a um usuário.
- Autenticação é sempre desacoplada do domínio de negócio: o mecanismo de login pode mudar ou coexistir com outro sem exigir alteração de regra de negócio.
- Uma Unidade de Negócio pode ter mais de um provedor de identidade simultâneo; a escolha de qual usar em cada autenticação é sempre configuração explícita, nunca implícita.
- Um modo de inicialização de ambiente sem nenhum administrador cadastrado (bootstrap) só pode existir enquanto essa condição for verdadeira; uma vez criado o primeiro administrador, esse modo é encerrado de forma permanente e não reaberto por perda de acesso subsequente.
- Toda funcionalidade de autenticação exige revisão de segurança dedicada antes de ser implementada e validação de segurança dedicada depois de implementada; nenhuma funcionalidade de autenticação é considerada concluída sem essas duas revisões.

## Frontend

- O Frontend é organizado por domínio de negócio, nunca por tipo técnico — não existe pasta horizontal de topo que agrupe `pages`, `components`, `hooks`, `services` ou `models` de toda a aplicação.
- Cada Vertical Slice possui autonomia funcional: os artefatos técnicos de um domínio (`pages`, `components`, `hooks`, `services`, `routes`, `models`, `types`, `tests`) permanecem agrupados dentro da própria fatia desse domínio.
- Um novo módulo funcional do negócio nasce como uma nova Vertical Slice, seguindo a mesma estrutura interna das slices já existentes — nunca como uma estrutura ad hoc própria.
- Componentes e utilitários genuinamente compartilhados entre múltiplos domínios permanecem exclusivamente em áreas de escopo explícito (`shared`, `design-system`) — nunca dentro de um domínio específico nem espalhados por conveniência.
- O domínio de negócio prevalece sobre a tecnologia na organização do código: uma decisão de onde colocar um arquivo é sempre respondida por "a qual domínio isso pertence", nunca por "que tipo de arquivo é este".
- Frontend e Backend permanecem arquiteturalmente alinhados: ambos crescem por domínio de negócio, ainda que com técnicas próprias de cada camada (Vertical Slice no frontend; Modular Monolith + Clean Architecture + DDD pragmático no backend).

## Evolução do Domínio

- Nenhum princípio deste documento pode ser alterado, adicionado ou removido diretamente aqui — toda mudança de princípio nasce como decisão em uma ADR (`.ai/DECISIONS.md`) e só depois é refletida neste documento.
- Este documento é atualizado por reconciliação documental após a aceitação ou atualização de uma ADR que introduza um princípio permanente de domínio — nunca antecipadamente.
- Um princípio permanece neste documento enquanto a ADR que o originou permanecer aceita; se uma ADR for substituída, este documento é atualizado na mesma reconciliação.

---

Em caso de conflito entre este documento e uma ADR, prevalece a ADR.
