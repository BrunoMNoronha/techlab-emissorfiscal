using System.Text;
using EmissorFiscal.Core.Protocols;

namespace EmissorFiscal.Core.Danfe;

// DANFE NFC-e em texto monoespaçado para bobina de 80 mm (48 colunas), com as
// divisões do Manual de Padrões Técnicos do DANFE-NFC-e e QR Code (revisão 6.0,
// fixada em TECHNICAL-SCOPE.md): cabeçalho do emitente, detalhe da venda,
// totais e pagamentos, mensagem fiscal, consulta por chave, consumidor,
// identificação/protocolo, QR Code e mensagem do contribuinte. Saída ASCII
// nos rótulos; os dados do XML passam sem alteração. Sem imagem embutida: o
// QR vai em arquivo próprio (QrMatrix.ToSvg) e a URL fica aqui para conferência.
public static class DanfeText
{
    public const int Columns = 48;
    public const string HomologationBanner = "EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
    private static readonly string Double = new('=', Columns);
    private static readonly string Single = new('-', Columns);

    // Tabela tPag do leiaute 4.00 (MOC 7, Anexo I, grupo YA). Código fora da
    // tabela imprime o próprio código.
    private static readonly IReadOnlyDictionary<string, string> PaymentLabels = new Dictionary<string, string>
    {
        ["01"] = "Dinheiro", ["02"] = "Cheque", ["03"] = "Cartao de Credito", ["04"] = "Cartao de Debito",
        ["05"] = "Credito Loja", ["10"] = "Vale Alimentacao", ["11"] = "Vale Refeicao", ["12"] = "Vale Presente",
        ["13"] = "Vale Combustivel", ["15"] = "Boleto Bancario", ["16"] = "Deposito Bancario",
        ["17"] = "Pagamento Instantaneo (PIX) - Dinamico", ["18"] = "Transferencia bancaria",
        ["19"] = "Programa de fidelidade", ["20"] = "Pagamento Instantaneo (PIX) - Estatico",
        ["21"] = "Credito em Loja", ["22"] = "Pagamento Eletronico nao Informado", ["90"] = "Sem pagamento",
        ["99"] = "Outros"
    };

    public static string Render(DanfeModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (!model.IsHomologation) throw new InvalidDataException("Renderizador restrito ao ambiente 2.");
        var lines = new List<string>();
        Header(model, lines);
        Items(model, lines);
        Totals(model, lines);
        FiscalMessage(model, lines);
        Consultation(model, lines);
        Consumer(model, lines);
        Identification(model, lines);
        QrCode(model, lines);
        if (!string.IsNullOrWhiteSpace(model.AdditionalInfo))
        {
            lines.Add(Single);
            lines.AddRange(Wrap(model.AdditionalInfo));
        }
        lines.Add(Double);
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.Length > Columns) throw new InvalidOperationException($"Linha excede {Columns} colunas: {line}");
            text.Append(line).Append('\n');
        }
        return text.ToString();
    }

    private static void Header(DanfeModel model, List<string> lines)
    {
        var emitter = model.Emitter;
        lines.Add(Double);
        lines.AddRange(Wrap(emitter.Name));
        if (!string.IsNullOrWhiteSpace(emitter.FantasyName) && emitter.FantasyName != emitter.Name)
            lines.AddRange(Wrap(emitter.FantasyName));
        var registration = "CNPJ " + DanfeModel.Document("CNPJ", emitter.Document);
        if (emitter.StateRegistration is not null) registration += "  IE " + emitter.StateRegistration;
        lines.AddRange(Wrap(registration));
        foreach (var line in emitter.AddressLines) lines.AddRange(Wrap(line));
        lines.Add(Single);
        lines.AddRange(Wrap("DANFE NFC-e - Documento Auxiliar da Nota Fiscal de Consumidor Eletronica"));
        lines.Add("Nao permite aproveitamento de credito de ICMS");
        lines.Add(Double);
        lines.AddRange(Wrap(HomologationBanner));
        lines.Add(Double);
    }

    private static void Items(DanfeModel model, List<string> lines)
    {
        lines.Add(Pad("# CODIGO  DESCRICAO", "QTD UN x V.UNIT = V.TOTAL"));
        lines.Add(Single);
        foreach (var item in model.Items)
        {
            var number = item.Number.PadLeft(3, '0');
            lines.AddRange(Wrap($"{number} {item.Code} {item.Description}", "    "));
            var values = $"{DanfeModel.Quantity(item.Quantity)} {item.Unit} x {DanfeModel.Quantity(item.UnitPrice)}";
            lines.Add(Pad("    " + values, DanfeModel.Money(item.Total)));
            if (item.Discount is not null && DanfeModel.Money(item.Discount) != "0,00")
                lines.Add(Pad("    Desconto item", "-" + DanfeModel.Money(item.Discount)));
        }
        lines.Add(Single);
    }

    private static void Totals(DanfeModel model, List<string> lines)
    {
        var totals = model.Totals;
        lines.Add(Pad("QTD. TOTAL DE ITENS", totals.ItemCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        lines.Add(Pad("VALOR TOTAL R$", DanfeModel.Money(totals.Products)));
        AddIfNonZero(lines, "DESCONTO R$", totals.Discount, "-");
        AddIfNonZero(lines, "FRETE R$", totals.Freight, "+");
        AddIfNonZero(lines, "SEGURO R$", totals.Insurance, "+");
        AddIfNonZero(lines, "OUTRAS DESPESAS R$", totals.Other, "+");
        lines.Add(Pad("VALOR A PAGAR R$", DanfeModel.Money(totals.Invoice)));
        lines.Add(Pad("FORMA DE PAGAMENTO", "VALOR PAGO R$"));
        foreach (var payment in model.Payments)
        {
            var label = PaymentLabels.TryGetValue(payment.Code, out var known) ? known : "tPag " + payment.Code;
            if (payment.Code == "99" && !string.IsNullOrWhiteSpace(payment.Description)) label = payment.Description;
            lines.Add(Pad(label, DanfeModel.Money(payment.Value)));
        }
        if (model.Change is not null && DanfeModel.Money(model.Change) != "0,00")
            lines.Add(Pad("Troco R$", DanfeModel.Money(model.Change)));
        lines.Add(Single);
    }

    private static void FiscalMessage(DanfeModel model, List<string> lines)
    {
        if (model.Totals.ApproximateTaxes is not null)
        {
            lines.AddRange(Wrap("Tributos Totais Incidentes (Lei Federal 12.741/2012):"));
            lines.Add(Pad("Valor aproximado R$", DanfeModel.Money(model.Totals.ApproximateTaxes)));
        }
        lines.AddRange(Wrap(HomologationBanner));
        lines.Add(Single);
    }

    private static void Consultation(DanfeModel model, List<string> lines)
    {
        lines.Add("Consulte pela Chave de Acesso em");
        lines.AddRange(Wrap(model.ConsultationUrl));
        lines.Add("CHAVE DE ACESSO");
        // 11 blocos de 4 em duas linhas (6 + 5), como nos DANFE de bobina.
        var blocks = model.FormattedKey.Split(' ');
        lines.Add(string.Join(' ', blocks[..6]));
        lines.Add(string.Join(' ', blocks[6..]));
        lines.Add(Single);
    }

    private static void Consumer(DanfeModel model, List<string> lines)
    {
        if (model.Recipient is null)
        {
            lines.Add("CONSUMIDOR NAO IDENTIFICADO");
        }
        else
        {
            var recipient = model.Recipient;
            lines.AddRange(Wrap($"CONSUMIDOR - {recipient.DocumentLabel} {DanfeModel.Document(recipient.DocumentLabel, recipient.Document)}"));
            if (recipient.Name.Length > 0) lines.AddRange(Wrap(recipient.Name));
            foreach (var line in recipient.AddressLines) lines.AddRange(Wrap(line));
        }
        lines.Add(Single);
    }

    private static void Identification(DanfeModel model, List<string> lines)
    {
        lines.AddRange(Wrap($"NFC-e n. {model.Number.PadLeft(9, '0')} Serie {model.Series.PadLeft(3, '0')} {DanfeModel.Timestamp(model.IssuedAt)}"));
        lines.AddRange(Wrap($"Protocolo de Autorizacao: {model.Protocol.Number}"));
        lines.AddRange(Wrap($"Data de Autorizacao: {DanfeModel.Timestamp(model.Protocol.ReceivedAt)}"));
        if (model.State != FiscalState.Authorized)
        {
            lines.AddRange(Wrap($"cStat {model.Protocol.Code} - {model.Protocol.Reason}"));
            foreach (var alert in model.Alerts) lines.AddRange(Wrap($"Alerta {alert.Code}: {alert.Text}", "  "));
        }
        lines.Add(Single);
    }

    private static void QrCode(DanfeModel model, List<string> lines)
    {
        lines.Add("QR CODE (imagem em arquivo separado):");
        lines.AddRange(Wrap(model.QrCodeText));
    }

    private static void AddIfNonZero(List<string> lines, string label, string value, string sign)
    {
        var money = DanfeModel.Money(value);
        if (money != "0,00") lines.Add(Pad(label, sign + money));
    }

    private static string Pad(string left, string right)
    {
        var gap = Columns - left.Length - right.Length;
        if (gap >= 1) return left + new string(' ', gap) + right;
        return left[..Math.Max(0, Columns - right.Length - 1)] + " " + right;
    }

    // Quebra por palavra; palavras maiores que a largura (URLs) são cortadas.
    // Linhas de continuação recebem o recuo informado.
    public static IReadOnlyList<string> Wrap(string text, string indent = "")
    {
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var remaining = word;
            while (remaining.Length > 0)
            {
                var prefix = lines.Count == 0 ? string.Empty : indent;
                var width = Columns - prefix.Length;
                var separator = current.Length > 0 ? 1 : 0;
                if (current.Length + separator + remaining.Length <= width)
                {
                    if (separator == 1) current.Append(' ');
                    current.Append(remaining);
                    remaining = string.Empty;
                }
                else if (current.Length > 0)
                {
                    lines.Add(prefix + current);
                    current.Clear();
                }
                else
                {
                    lines.Add(prefix + remaining[..width]);
                    remaining = remaining[width..];
                }
            }
        }
        if (current.Length > 0 || lines.Count == 0) lines.Add((lines.Count == 0 ? string.Empty : indent) + current);
        return lines;
    }
}
