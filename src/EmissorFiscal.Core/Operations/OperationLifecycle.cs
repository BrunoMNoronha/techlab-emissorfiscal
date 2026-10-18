using System.Security.Cryptography;
using System.Text;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Operations;

// Único caminho de escrita do ciclo de vida: exige lease atual (fencing),
// valida a transição e grava um evento com controle otimista. Não faz I/O
// fiscal; recebe o XML de resposta do chamador, como NfceProtocol.
public sealed class OperationLifecycle(IOperationStore store, ILeaseRegistry leases, IClock clock, XsdValidator validator)
{
    public Operation RecordIntent(Lease lease, NumberReservation reservation, string baselineId)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(baselineId);
        Guard(lease);
        if (lease.OperationId != reservation.OperationId)
            throw new InvalidDataException("Lease e reserva referem operações diferentes.");
        if (store.Load(lease.OperationId) is not null)
            throw new InvalidTransitionException(lease.OperationId, OperationState.Intended, "nova intenção");
        return store.Append(lease.OperationId,
            new IntentRecorded(lease.FencingToken, clock.UtcNow, reservation, baselineId), 0);
    }

    public Operation FreezeXml(Lease lease, string signedXml)
    {
        ArgumentNullException.ThrowIfNull(signedXml);
        Guard(lease);
        var operation = Require(lease.OperationId);
        if (operation.State != OperationState.Intended)
            throw new InvalidTransitionException(operation.OperationId, operation.State,
                "congelar XML (correção/nova revisão indisponíveis — H13)");
        // Chave do Id (com DV) conferida contra a reserva antes de qualquer criptografia.
        var document = SafeXml.Load(signedXml);
        var info = SafeXml.Child(document.DocumentElement ?? throw new InvalidDataException("XML vazio."), "infNFe");
        var key = AccessKey.Parse(info.GetAttribute("Id") is { Length: 47 } id && id.StartsWith("NFe", StringComparison.Ordinal)
            ? id[3..] : throw new InvalidDataException("Id de infNFe inválido."));
        var reservation = operation.Reservation!;
        if (key.Value.Substring(22, 3) != reservation.Key.Series || key.Value.Substring(25, 9) != reservation.PaddedNumber)
            throw new InvalidDataException("Série/nNF do XML assinado não correspondem à reserva da operação.");
        validator.RequireValid("nfe", signedXml);
        var proof = A1Signer.Verify(signedXml);
        if (proof.Key != key) throw new InvalidDataException("Chave assinada difere do Id de infNFe.");
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signedXml)));
        return store.Append(operation.OperationId,
            new XmlFrozen(lease.FencingToken, clock.UtcNow, signedXml, sha256, proof.DigestBase64, proof.Key.Value),
            operation.Version);
    }

    // Registra a intenção de enviar antes do I/O. Só a primeira tentativa é
    // permitida aqui: depois dela, dúvida se resolve por consulta.
    public Operation RecordSubmission(Lease lease)
    {
        Guard(lease);
        var operation = Require(lease.OperationId);
        if (operation.State != OperationState.XmlFrozen)
            throw new InvalidTransitionException(operation.OperationId, operation.State,
                "novo envio (sem reenvio automático; consultar ou reconciliar)");
        return store.Append(operation.OperationId,
            new SubmissionRecorded(lease.FencingToken, clock.UtcNow, operation.Attempts + 1), operation.Version);
    }

    public Operation RecordResponse(Lease lease, string responseXml)
    {
        ArgumentNullException.ThrowIfNull(responseXml);
        Guard(lease);
        var operation = Require(lease.OperationId);
        if (operation.State != OperationState.Submitted)
            throw new InvalidTransitionException(operation.OperationId, operation.State, "resposta de envio");
        var result = NfceProtocol.Interpret(operation.SignedXml!, responseXml, validator);
        return store.Append(operation.OperationId,
            new ResponseRecorded(lease.FencingToken, clock.UtcNow, operation.Attempts, responseXml, result),
            operation.Version);
    }

    public Operation RecordConsultation(Lease lease, string responseXml)
    {
        ArgumentNullException.ThrowIfNull(responseXml);
        Guard(lease);
        var operation = Require(lease.OperationId);
        if (operation.State is not (OperationState.Submitted or OperationState.Processing
            or OperationState.Unknown or OperationState.NotFound))
            throw new InvalidTransitionException(operation.OperationId, operation.State, "consulta");
        var result = NfceProtocol.Interpret(operation.SignedXml!, responseXml, validator);
        return store.Append(operation.OperationId,
            new ConsultationRecorded(lease.FencingToken, clock.UtcNow, operation.Consultations + 1, responseXml, result),
            operation.Version);
    }

    private void Guard(Lease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (!leases.IsCurrent(lease)) throw new FencingException(lease.OperationId, lease.FencingToken);
    }

    private Operation Require(string operationId) =>
        store.Load(operationId) ?? throw new InvalidTransitionException(operationId, OperationState.Intended, "evento sem intenção registrada");
}
