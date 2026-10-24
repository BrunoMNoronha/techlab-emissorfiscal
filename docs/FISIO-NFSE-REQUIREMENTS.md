# Requisitos de emissão fiscal — clínica de fisioterapia (DF, particular + convênios)

Documento preparado em 17/10/2026 (America/Sao_Paulo). Consolida, como requisitos documentais, o que uma clínica de fisioterapia estabelecida no Distrito Federal precisa para cumprir a emissão fiscal dos seus serviços, atendendo pacientes particulares e operadoras de plano de saúde. Complementa o [mapeamento TISS → NFS-e](TISS-NFSE-FIELD-MAP.md) e segue suas convenções. **Não publica DTO, JSON Schema, rotas ou persistência; não implementa emissão; não substitui aconselhamento tributário do contador responsável.**

## 1. Escopo e advertências

**Caso coberto:** clínica de fisioterapia no DF, serviços prestados a pessoas físicas (particular) e a operadoras de plano de saúde (convênios, faturados via TISS). **Fora do escopo:** clínicas em outros municípios (exige verificação própria de adesão ao padrão nacional), venda de mercadorias, outros serviços de saúde.

**Documento fiscal do serviço: NFS-e** (ISS). Fisioterapia não gera NF-e nem NFC-e. Se a clínica passar a **vender produtos** (faixas, órteses, cosméticos etc.), a venda de mercadoria exigirá NFC-e/NF-e em paralelo — esse é o recorte atual deste repositório ([ROADMAP](ROADMAP.md)), que este documento não altera.

**Padrão vigente no DF:** NFS-e **padrão nacional** (leiaute DPS), obrigatório desde **01/10/2026**; o padrão ABRASF 2.04 do ISSNet deixou de ser aceito para emissão. Fontes: [comunicado GDF de 04/11/2025](https://static.fazenda.df.gov.br/arquivos/servico-1202/comunicado_NFSe_padrao_nacional_04-11-2025.pdf) e [notícia COAD](https://coad.com.br/home/noticias-detalhe/139146/governo-do-distrito-federal-informa-sobre-o-ambiente-de-producao-da-nfs-e-no-padrao-nacional). Gate: reconfirmar o ambiente vigente no portal da Secretaria de Economia do DF antes de implementar.

## 2. Enquadramento do serviço

| Aspecto | Valor | Fonte e condição |
| --- | --- | --- |
| Item da lista de serviços (LC 116/2003) | **4.08** — terapia ocupacional, fisioterapia e fonoaudiologia | Lista anexa à LC 116; conferir texto no [Planalto](https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp116.htm) |
| Código de tributação nacional (`cTribNac`) | **04.08.02** (fisioterapia) indicativo; desdobros do item 4.08: 04.08.01 terapia ocupacional, 04.08.03 fonoaudiologia | Tabela de códigos do Sistema Nacional NFS-e ([referência](https://buscadorncm.com.br/nfse/ctribnac/40801)); confirmação final é do contador |
| Código de tributação municipal (`cTribMun`) | `TBD` | Lista do DF, dependente do `cTribNac` |
| CNAE principal | **8650-0/04** — atividades de fisioterapia ([referência](https://www.numerando.com.br/cnae/atividades-de-fisioterapia)) | Conferir no cartão CNPJ da empresa; CNAEs secundários (8650-0/05, 8650-0/06) se houver TO/fono |
| Local de incidência do ISS | Município do estabelecimento prestador (DF, IBGE `5300108`) | Regra geral da LC 116 para o item 4.08 |
| Alíquota de ISS no DF | `TBD` (entre 2% e 5%, limites dos arts. 8º-A e 8º, II da LC 116) | Legislação do DF para o item 4.08; parametrização, nunca constante de código |

## 3. Canais de emissão

| Canal | Quando usar | Requisitos de acesso |
| --- | --- | --- |
| **Emissor Web Nacional** ([www.nfse.gov.br](https://www.nfse.gov.br), portal e app) | Volume baixo/médio, emissão manual nota a nota | Cadastro da empresa no sistema nacional; login gov.br do responsável; sem integração |
| **API do Sistema Nacional NFS-e** (leiaute DPS) | Automação a partir do sistema de gestão da clínica; necessário para importar lotes TISS e emitir em escala | Certificado digital ICP-Brasil (e1/A1 ou superior) da empresa; homologação da integração; é o leiaute mapeado em [TISS-NFSE-FIELD-MAP.md](TISS-NFSE-FIELD-MAP.md) |
| ISSNet/ABRASF 2.04 (DF) | **Indisponível para emissão desde 01/10/2026** | Mantido só para consulta de notas antigas |

A escolha entre portal e API é operacional (`TBD`: volume mensal de notas da clínica). Nada impede começar pelo Emissor Web e automatizar depois — o enquadramento tributário é o mesmo.

## 4. Pré-requisitos cadastrais e credenciais

1. **CNPJ ativo** com CNAE compatível (8650-0/04 principal) — empresa piloto do recorte EF-22 (issue [#48](https://github.com/BrunoMNoronha/techlab-emissorfiscal/issues/48)), indicada em 24/10/2026: Ortosport Fisioterapia Especializada Ltda, ativa, CNAE principal 8650-0/04 e secundários 8690-9/03 (acupuntura) e 9313-1/00 (condicionamento físico) por fonte secundária ([PILOT-COMPANY.md](PILOT-COMPANY.md)); reconferir no cartão CNPJ. Acupuntura (item 4.05) e condicionamento físico (item 6.04) têm itens próprios na lista da LC 116, com `cTribNac` distintos de 04.08.02, a confirmar com o contador se forem faturados.
2. **Inscrição no CF/DF** (cadastro fiscal do DF) do estabelecimento.
3. **Acesso gov.br** (emissor web) e/ou **certificado digital ICP-Brasil** (API); custódia da credencial fora do conteúdo das notas e de logs.
4. **Cadastro das operadoras tomadoras**: tabela registro ANS → CNPJ/razão social/endereço/IM, como definido na seção 4.1 do [mapeamento TISS](TISS-NFSE-FIELD-MAP.md); `TBD` por operadora conveniada.
5. Registro no **CREFITO** (pessoa jurídica e responsável técnico) — exigência profissional, não fiscal; citada por completude, fora do escopo do emissor.

## 5. Regime tributário (decisões do contador — tudo `TBD`)

- **Simples Nacional**: fisioterapia transita entre **Anexo V** e **Anexo III** conforme o **Fator R** — folha de salários ≥ 28% da receita bruta (12 meses) leva ao Anexo III, com alíquotas menores. O enquadramento é recalculado mensalmente; impacto direto no custo e em obrigações acessórias.
- **Lucro presumido/real**: alternativa conforme porte; habilita retenções de IR/CSRF pelos tomadores PJ (seção 6).
- Em qualquer regime, a parametrização (CRT/opção pelo Simples no grupo `prest/regTrib` da DPS, alíquotas, retenções) vem de evidência do regime efetivo, nunca de default.

## 6. Fluxos por tipo de tomador

| Fluxo | Documento(s) | Características |
| --- | --- | --- |
| **Paciente particular (PF)** | NFS-e por atendimento, sessão ou pacote | Tomador PF (CPF; obrigatoriedade de identificação conforme leiaute/exigência do caso); sem retenções; discriminação do serviço sem dados clínicos |
| **Operadora de plano (PJ conveniada)** | Faturamento **TISS** (lotes de guias SP-SADT — cobrança, não fiscal) + **NFS-e por lote** (granularidade configurável) | De-para completo no [TISS-NFSE-FIELD-MAP.md](TISS-NFSE-FIELD-MAP.md), que vale igualmente para a clínica; retenções usuais: ISS retido pelo tomador conforme lei do DF, IR e CSRF (PIS/COFINS/CSLL) quando o regime não for Simples |

**Diferenças da clínica frente ao caso do laboratório** (mesmo mapeamento, atenção extra):

- Os procedimentos TUSS são de **sessões de fisioterapia**, com guias que tipicamente agrupam **múltiplas sessões em datas distintas** (série de tratamento): o risco de **competências mistas** num mesmo lote (regra 3 da seção 6 do mapeamento) é maior e a política de competência deve ser definida antes da automação.
- `quantidadeExecutada` > 1 por item é comum (número de sessões); a conferência aritmética por item do mapeamento se aplica sem alteração.
- A ressalva de privacidade é idêntica: **dados do beneficiário e do profissional não entram na NFS-e** (LGPD, art. 11).

## 7. Requisitos consolidados para futura automação

Requisitos documentais mínimos de um sistema emissor para este caso (sem design de API):

1. Cadastro do prestador com evidência (CNPJ, CF/DF, regime efetivo/CRT, certificado) e dos tomadores PF e PJ (operadoras por registro ANS).
2. Parametrização tributária por serviço e por tomador (`cTribNac` 04.08.xx, `cTribMun`, alíquota, retenções ISS/IR/CSRF), com responsável e vigência registrados.
3. Importação de lotes TISS com as validações bloqueantes da seção 6 do [mapeamento](TISS-NFSE-FIELD-MAP.md) (aritmética, duplicidade/idempotência, competências mistas, prestador coerente, versão do padrão).
4. Granularidade configurável (nota por lote × por guia) como escolha explícita do emitente.
5. Emissão NFS-e padrão nacional (DPS) nos dois fluxos (PF avulso e convênio por lote), com numeração/série controladas pelo sistema.
6. Trilha de conciliação lote TISS ↔ NFS-e ↔ pagamento da operadora (glosas como evolução futura, fora deste recorte).

## 8. Pendências (`TBD`)

| Pendência | Responsável | Bloqueia |
| --- | --- | --- |
| Confirmação de `cTribNac` 04.08.02, `cTribMun` e alíquota do DF | Contador | Parametrização e primeira emissão |
| Regime efetivo e acompanhamento do Fator R | Contador | Retenções e grupo `regTrib` |
| Cadastro das operadoras conveniadas (ANS→CNPJ) | Clínica/administrativo | Fluxo de convênios |
| Volume mensal de notas | Clínica | Escolha portal × API |
| Venda de produtos (sim/não) | Clínica | Necessidade futura de NFC-e |
| Reconfirmação do ambiente nacional vigente no DF e do Anexo I do leiaute DPS | Executor da implementação | Qualquer código |
