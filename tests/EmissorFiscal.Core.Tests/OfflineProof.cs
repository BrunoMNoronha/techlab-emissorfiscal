using EmissorFiscal.Core.Xsd;
using EmissorFiscal.Samples;

namespace EmissorFiscal.Core.Tests;

public sealed class OfflineProof
{
    public XsdValidator Validator { get; } = OfflineFixtures.Validator();
    public string Signed { get; } = OfflineFixtures.SignedInvoice();
    public string AlphaSigned { get; } = OfflineFixtures.SignedInvoice(alphanumeric: true);
}
