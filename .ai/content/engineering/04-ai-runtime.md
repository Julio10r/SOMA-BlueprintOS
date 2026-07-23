# Runtime de Inteligência Artificial

O runtime de Inteligência Artificial é a camada responsável por executar solicitações de agentes junto a provedores de modelos de linguagem, de forma desacoplada de qualquer provedor específico.

Essa abstração permite que a plataforma troque ou combine provedores de IA ao longo do tempo, sem que os agentes que consomem o runtime precisem ser alterados. O provedor atualmente utilizado atende a solicitações de conversação, retornando conteúdo de resposta, consumo de recursos e informações sobre o motivo de encerramento da geração.

Os agentes especializados da plataforma (por exemplo, o agente de consulta a conhecimento) utilizam esse runtime como base para receber contexto, formular solicitações e processar as respostas geradas pelo modelo.

A plataforma já prevê, em sua arquitetura, a capacidade de os agentes utilizarem ferramentas externas durante a execução — permitindo que ações estruturadas sejam disparadas a partir de uma solicitação em linguagem natural. Essa capacidade está prevista como direção de evolução do runtime.

Credenciais de acesso a provedores de IA são configuradas de forma segura, fora do código-fonte, seguindo os padrões corporativos de gestão de segredos.
