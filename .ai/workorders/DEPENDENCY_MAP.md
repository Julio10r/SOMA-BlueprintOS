# Mapa de Dependências

## Entre fases

Foundation habilita Sourcing e Negotiation. Sourcing fornece dados para Negotiation, Risk e Analytics. Integration Framework habilita integrações externas. Identity, autorização e observabilidade são transversais e obrigatórias antes de produção.

## Caminho crítico proposto

A1 → A2 → A3 → B1 → B2 → B3 → B4 → B6 → C1 → C2 → C5 → C3 → C4 → C7 → H1 → H2 → H3 → H4 → H5 → H6 → H7.

## Trabalhos paralelizáveis

Após B1/B2/B3, B5, E1 e F1 podem ser desenhadas em paralelo. Após G1, G2 a G6 podem avançar em paralelo, sujeitos a credenciais e contratos aprovados.

## Decisões bloqueadoras

- Modelo de dados e persistência para B1.
- Identidade/autorização para operações corporativas.
- ERPs, plataforma jurídica e fontes de risco específicas.
- Contratos e limites de autonomia para C3.
- Estratégia de cloud, CI/CD e observabilidade para H4-H7.

## Dependências obrigatórias

B2→B1; B3→B1/B2; B4→B3; C1→B1/B3/B4/B6; C2→C1; C3→C2/C5; C4→C3; D1 é base jurídica; E2→E1; F2-F5→F1; G2-G6→G1; H2→H1; H3→H1/H2; H7→capacidades de produção.
