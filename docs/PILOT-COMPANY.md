# EF-06 — empresa piloto indicada: Ortosport

Indicação de Bruno em 24/10/2026 (America/Sao_Paulo): a **ORTOSPORT FISIOTERAPIA
ESPECIALIZADA LTDA** será a empresa piloto, no recorte **NFS-e padrão nacional**
(EF-22, issue [#48](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/48),
decisões H21/H22). Este documento registra os dados cadastrais **públicos** da
empresa, a fonte consultada, o que ainda precisa de comprovação e a decisão de
produto tomada. Ele não preenche ficha alguma por si: cada campo da futura ficha
do piloto NFS-e continua exigindo evidência primária e responsável, nos moldes
do [roteiro de seleção](PILOT-SELECTION.md).

## 1. Dados cadastrais públicos

Fonte consultada: página da empresa na Casa dos Dados, agregadora dos dados
abertos de CNPJ da Receita Federal, com última atualização em 13/09/2026
([referência](https://casadosdados.com.br/solucao/cnpj/ortosport-fisioterapia-especializada-ltda-66979142000150)).
É **fonte secundária**: cada dado abaixo deve ser reconferido no cartão CNPJ da
Receita Federal e, quando couber, no cadastro fiscal do DF antes de entrar na
ficha como `comprovado`.

| Campo | Valor na fonte secundária | Estado na ficha |
| --- | --- | --- |
| Razão social | ORTOSPORT FISIOTERAPIA ESPECIALIZADA LTDA | `em análise` |
| Nome fantasia | ORTOSPORT | `em análise` |
| CNPJ | 66.979.142/0001-50 (matriz) | `em análise` |
| Situação cadastral | ATIVA desde 18/05/2026 | `em análise` |
| Data de abertura | 18/05/2026 | `em análise` |
| Natureza jurídica | 2062 — Sociedade Empresária Limitada | `em análise` |
| MEI | Não | `em análise` |
| Simples Nacional | Sim, opção em 25/05/2026 | `em análise`; indica CRT 1, mas o CRT efetivo só entra na ficha com confirmação do responsável tributário |
| Capital social | R$ 80.000 | informativo |
| Endereço do estabelecimento | Avenida Segunda Avenida, Blocos 615 B, Loja 01, Núcleo Bandeirante, Brasília/DF, CEP 71710-525 | `em análise` |
| UF / código IBGE do município | DF / 5300108 (Brasília) | `em análise` |
| CNAE principal | 8650-0/04 — Atividades de fisioterapia | `em análise` |
| CNAEs secundários | 8690-9/03 — Atividades de acupuntura; 9313-1/00 — Atividades de condicionamento físico | `em análise` |
| Inscrição no CF/DF | não consta na fonte | `TBD` |
| Inscrição estadual para ICMS | não consta na fonte | `TBD`; indispensável para qualquer hipótese NFC-e |

Dados pessoais exibidos na fonte (nomes de sócios, e-mail e telefone) ficam
**fora do Git**, no registro privado da decisão T01. Para a ficha interessam o
vínculo e os poderes do autorizante e do responsável tributário, comprovados por
documento, não o nome publicado em agregador.

## 2. Compatibilidade com o recorte do projeto

A indicação precisa ser lida contra o que o repositório já define:

- **Recorte do MVP:** NFC-e modelo 65/DF em homologação, venda presencial de
  mercadoria com retirada para consumo próprio ([contrato 0.2](PILOT-CONTRACT.md)).
  Pressupõe emitente contribuinte do ICMS, com inscrição estadual e série de
  NFC-e.
- **Atividade da empresa:** fisioterapia, acupuntura e condicionamento físico
  são prestação de serviço sujeita ao ISS (LC 116/2003, itens 4.08, 4.05 e 6.04,
respectivamente). O
  documento fiscal desses serviços é a **NFS-e** no padrão nacional, obrigatória
  no DF desde 01/10/2026, como já descrito em [FISIO-NFSE-REQUIREMENTS](FISIO-NFSE-REQUIREMENTS.md).
  Fisioterapia não gera NFC-e.
- **Trilha já aberta:** a decisão H19 (17/10/2026) registra interessado real na
  trilha exploratória NFS-e saúde, com identidade no registro privado T01. Se a
  Ortosport for esse interessado, a indicação de hoje é a confirmação pública
  dele, e a trilha passa a ter empresa nomeada.

Não se infere classificação fiscal a partir de CNAE ou CNPJ. O que os dados
públicos permitem afirmar é apenas isto: a atividade principal declarada não
é venda de mercadoria. Se a clínica também vende produtos (órteses, faixas,
cosméticos), essa venda exigiria NFC-e em paralelo e aí sim caberia no recorte
atual, mas isso é fato a confirmar com a empresa, não suposição.

## 3. Decisão de produto

**Decidido em 24/10/2026 (H22):** caminho **A**. O piloto da Ortosport é
**NFS-e padrão nacional**; a trilha exploratória NFS-e saúde foi promovida ao
recorte EF-22, issue [#48](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/48).
O MVP NFC-e 65/DF segue sem empresa indicada, e a Ortosport não entra na
[ficha NFC-e](PILOT-REGISTRATION.md); o recorte EF-22 terá contrato, matriz
normativa e ficha próprios. Os caminhos avaliados ficam registrados abaixo como
histórico da decisão.

A indicação abriu a escolha, que pertencia a Bruno, não a este documento:

| Caminho | O que muda | O que não muda |
| --- | --- | --- |
| **A. Piloto NFS-e** — promover a trilha exploratória a recorte com issue própria, tendo a Ortosport como emitente | Novo domínio (ISS/DPS/Ambiente Nacional) passa a ter épico, matriz normativa e ficha próprias; o MVP NFC-e segue em preparação técnica sem empresa | Núcleo de numeração, ciclo de vida, idempotência e custódia continua reaproveitável; nenhum código NF-e 4.00 se aplica à DPS |
| **B. Piloto NFC-e** — manter o recorte 65/DF e usar a Ortosport apenas se houver venda de mercadoria comprovada | Ficha EF-06 exige inscrição estadual, série de NFC-e, caso de venda real e CRT comprovado | Serviços de fisioterapia ficam fora do piloto; a clínica precisaria de NFS-e por outro meio |
| **C. Ambos** — Ortosport como emitente dos dois documentos | Dois pilotos com gates independentes; prioridade e sequência a decidir | Cada documento tem contrato, ficha e aceite separados |

A Ortosport fica registrada como **empresa piloto indicada do recorte EF-22**,
com cadastro `em análise`. A decisão não autoriza certificado, credencial gov.br,
transmissão ou produção.

## 4. Próximas verificações

| Verificação | Como comprovar | Responsável |
| --- | --- | --- |
| Cartão CNPJ e situação | Consulta na Receita Federal, com data e cópia no registro privado | Bruno ou autorizante da empresa |
| Inscrição no CF/DF e eventual IE | Consulta ao cadastro fiscal do DF; se não houver IE, a hipótese NFC-e fica bloqueada | Autorizante da empresa com o contador |
| CRT e enquadramento no Simples | Confirmação do contador, incluindo Fator R para o Anexo III/V na hipótese NFS-e | Responsável tributário |
| Venda de mercadoria | Declaração da empresa sobre se vende produtos e em que volume | Autorizante da empresa |
| Interlocutor, autorizante e responsável tributário | Documento de vínculo e alcance, guardado no registro privado T01 | Bruno |
| Sistema cliente e histórico de emissão | Identificação do sistema de gestão da clínica e das notas já emitidas desde maio de 2026, em qualquer padrão | Autorizante da empresa |
| Caminho A, B ou C | Decidido: caminho A, H22 em [DECISIONS](evidence/ef06/DECISIONS.md) | Bruno (feito em 24/10/2026) |
| Canal de emissão (Emissor Web × API) e volume mensal | Declaração da clínica; decisão registrada em DECISIONS | Bruno com a empresa |

As verificações pendentes são critérios de aceite da issue
[#48](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/48). A
[ficha NFC-e](PILOT-REGISTRATION.md) não é afetada: a Ortosport não é emitente
daquele piloto, e a issue [#3](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/3)
permanece parcial e aberta, sem empresa indicada.
