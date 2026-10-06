using System.Security.Cryptography;
using System.Text.Json;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Samples;
using EmissorFiscal.Cli;

try
{
    if (args.Length == 5 && args[0] == "prepare")
        return PreparationCommand.Run(args[1], args[2], args[3], args[4]);
    if (args.Length != 1 || args[0] is not ("demo" or "schemas"))
    {
        Console.Error.WriteLine("Uso: dotnet run --project src/EmissorFiscal.Cli -- [demo|schemas]");
        Console.Error.WriteLine("Modelo: prepare <venda.json> <emitente.json> <contexto-teste.json> <diretorio-novo>");
        Console.Error.WriteLine("Exclusivamente offline; não recebe A1 real, senha, CSC ou endpoint.");
        return 2;
    }
    var validator = OfflineFixtures.Validator();
    if (args[0] == "schemas")
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            baseline = validator.Manifest.BaselineId,
            manifestSha256 = validator.Manifest.ManifestSha256,
            compiledEntrypoints = validator.CompiledEntrypoints,
            fiscalIo = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
    var original = OfflineFixtures.SignedInvoice(alphanumeric: true);
    validator.RequireValid("nfe", original);
    var proof = A1Signer.Verify(original);
    var submission = NfceMessages.BuildSubmission(original, "1", validator);
    var consultation = NfceMessages.BuildConsultation(proof.Key, validator);
    var response = OfflineFixtures.Response(original, "120", alerts: true);
    var result = NfceProtocol.Interpret(original, response, validator);
    if (!result.IsAuthorized || result.State != FiscalState.AuthorizedWithAlert || result.Messages.Count != 2)
        throw new InvalidDataException("Demonstração sintética de protocolo falhou.");
    var processed = NfceProtocol.BuildProcessed(original, response, validator);
    var consulted = NfceProtocol.Interpret(original, OfflineFixtures.Response(original, consultation: true), validator);
    if (!consulted.IsAuthorized) throw new InvalidDataException("Demonstração sintética de consulta falhou.");
    var proofInEnvelope = A1Signer.Verify(submission);
    var proofInProcessed = A1Signer.Verify(processed);
    if (proofInEnvelope.DigestBase64 != proof.DigestBase64 || proofInProcessed.DigestBase64 != proof.DigestBase64)
        throw new CryptographicException("Digest mudou no envelope/nfeProc.");
    validator.RequireValid("consSitNFe", consultation);
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        status = "PROVA LOCAL SINTETICA - SEM AUTORIZACAO FISCAL REAL",
        stack = Environment.Version.ToString(), baseline = validator.Manifest.BaselineId,
        compiledEntrypoints = validator.CompiledEntrypoints,
        key = proof.Key.Value, environment = "2", uf = "53", model = "65", emissionType = "1",
        pfx = "sintético, gerado/importado em memória, chave privada não gravada",
        signature = "RSA-SHA1 / SHA1 / C14N conforme MOC 7",
        xmlSha256 = proof.XmlSha256, fiscalDigestBase64 = proof.DigestBase64,
        syntheticProtocol = result.ProtocolNumber, syntheticState = result.State.ToString(), alerts = result.Messages,
        signatureAfterSerialization = true, signatureInEnvelope = true, signatureInProcessed = true,
        fiscalIo = false, taxpayerCertificate = false, fiscalHomologationObtained = false
    }, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error) when (error is InvalidDataException or System.Xml.XmlException or
    System.Xml.Schema.XmlSchemaException or CryptographicException or IOException or JsonException)
{
    Console.Error.WriteLine($"Prova offline falhou: {error.GetType().Name}: {error.Message}");
    return 1;
}
