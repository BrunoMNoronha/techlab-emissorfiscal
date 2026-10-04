# Fixtures técnicas

`nfce-unsigned.xml` é um molde estrutural sintético, sem suplemento/assinatura.
A CLI e os testes adicionam QR 3 e assinam com certificado sintético criado em
memória. O PKCS#12 e a chave privada nunca são gravados. Os retornos positivos
e `nfeProc` são construídos em memória com o digest da assinatura corrente.

NCM `00000000`, IE, CRT, CFOP, CSOSN/CST e valores são material de teste XSD.
Não definem operações/regimes permitidos, enquadramento, credenciamento,
titularidade de A1 ou aceite tributário. Não transmitir estas fixtures.

Os negativos incluem DTD/XXE e resposta truncada em arquivos; os testes geram
as demais mutações (escopo, tipos, assinatura, IDs, referência, protocolos,
hashes/dependências do manifest) em cópias isoladas/in-memory.
