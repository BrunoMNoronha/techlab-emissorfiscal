using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Danfe;

public sealed record DanfeDocument(DanfeModel Model, string Text, QrMatrix QrCode, string QrSvg);

// Fachada: nfeProc autorizado -> modelo, texto 48 colunas e QR Code (matriz/SVG).
// Nenhum arquivo, impressora, PDF ou transporte: a saída é só texto.
public static class DanfeRenderer
{
    public static DanfeDocument Render(string processedXml, XsdValidator validator)
    {
        var model = DanfeModel.FromProcessed(processedXml, validator);
        var text = DanfeText.Render(model);
        var qr = QrEncoder.Encode(model.QrCodeText);
        return new DanfeDocument(model, text, qr, qr.ToSvg());
    }
}
