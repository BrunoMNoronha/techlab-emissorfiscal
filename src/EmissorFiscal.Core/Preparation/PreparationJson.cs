using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using EmissorFiscal.Core.Domain;

namespace EmissorFiscal.Core.Preparation;

public static class PreparationJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        MaxDepth = 32
    };

    public static PreparationSale ParseSale(string json)
    {
        var value = Parse<PreparationSale>(json);
        PreparationValidation.ValidateSale(value);
        return value;
    }

    public static PreparationEmitterProfile ParseEmitter(string json)
    {
        var value = Parse<PreparationEmitterProfile>(json);
        PreparationValidation.ValidateEmitter(value);
        return value;
    }

    public static PreparationTestContext ParseContext(string json)
    {
        var value = Parse<PreparationTestContext>(json);
        PreparationValidation.ValidateContext(value);
        return value;
    }

    private static T Parse<T>(string json) where T : class
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > SafeLimit) throw new InvalidDataException("JSON excede o limite local de 1.048.576 caracteres.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("A raiz JSON deve ser um objeto.");
            RejectAmbiguity(document.RootElement);
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new InvalidDataException("Documento JSON ausente.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("JSON inválido para nfce-preparation/0.1: " + exception.Message, exception);
        }
    }

    private const int SafeLimit = 1024 * 1024;

    private static void RejectAmbiguity(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
            throw new InvalidDataException("Null explícito não é suportado; omita apenas campos opcionais.");
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException("Propriedade JSON duplicada: " + property.Name);
                RejectAmbiguity(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectAmbiguity(child);
    }
}

// Restrições de representação e do subconjunto implementado; não calcula,
// classifica ou atesta regra fiscal/MOC/NT. O XML assinado ainda exige XSD.
internal static class PreparationValidation
{
    private const string MoneyPattern = @"^(0|[1-9][0-9]{0,12})\.[0-9]{2}$";
    private const string OptionalMoneyPattern = @"^(0\.[0-9][1-9]|0\.[1-9][0-9]|[1-9][0-9]{0,12}\.[0-9]{2})$";
    private const string RatePattern = @"^(0|0\.[0-9]{2,4}|[1-9][0-9]{0,2}(\.[0-9]{2,4})?)$";

    public static void Validate(PreparationSale sale, PreparationEmitterProfile emitter, PreparationTestContext context)
    {
        ValidateSale(sale);
        ValidateEmitter(emitter);
        ValidateContext(context);
        if (sale.EmitterProfileId != emitter.ProfileId) Fail("Perfil da venda não corresponde ao perfil fornecido.");
        foreach (var item in sale.Items)
        {
            if (emitter.Crt == "1" && item.Taxes.Icms.Group != "ICMSSN102")
                Fail("CRT1: este subconjunto prepara somente ICMSSN102/CSOSN102.");
            if (emitter.Crt == "3" && item.Taxes.Icms.Group != "ICMS00")
                Fail("CRT3: este subconjunto prepara somente ICMS00/CST00.");
            if (emitter.Crt == "3" && item.Taxes.IbsCbs is null)
                Fail("CRT3: IBSCBS explícito é obrigatório neste recorte de homologação RTC.");
            if (emitter.Crt == "1" && item.Taxes.IbsCbs is not null)
                Fail("IBSCBS no CRT1 não está mapeado neste subconjunto local.");
        }
        var hasRtc = sale.Items.Any(item => item.Taxes.IbsCbs is not null);
        if (hasRtc && (sale.Totals.IbsCbs is null || sale.Totals.InvoiceTotal is null ||
                       sale.Items.Any(item => item.ItemTotal is null)))
            Fail("IBSCBS exige IBSCBSTot, vNFTot e vItem declarados; nenhum total será calculado.");
        if (!hasRtc && (sale.Totals.IbsCbs is not null || sale.Totals.InvoiceTotal is not null ||
                        sale.Items.Any(item => item.ItemTotal is not null)))
            Fail("Totais RTC sem IBSCBS dos itens não são suportados.");
    }

    public static void ValidateSale(PreparationSale sale)
    {
        RequireMembers(sale);
        Version(sale.Version);
        Text(sale.Reference, 1, 100, "reference");
        Text(sale.EmitterProfileId, 1, 60, "emitterProfileId");
        IssuedAt(sale.IssuedAt);
        Text(sale.Operation.Nature, 1, 60, "natOp");
        Equal(sale.Operation.InvoiceType, "1", "tpNF");
        Equal(sale.Operation.Destination, "1", "idDest");
        Equal(sale.Operation.Purpose, "1", "finNFe");
        Equal(sale.Operation.FinalConsumer, "1", "indFinal");
        Equal(sale.Operation.Presence, "1", "indPres");
        Equal(sale.Operation.FreightMode, "9", "modFrete");
        if (sale.Recipient is not null) Recipient(sale.Recipient);
        if (sale.Items.Length is < 1 or > 990) Fail("items deve conter de 1 a 990 itens.");
        if (sale.Items.Select(item => item.Number).Distinct(StringComparer.Ordinal).Count() != sale.Items.Length)
            Fail("nItem deve ser único.");
        for (var index = 0; index < sale.Items.Length; index++)
        {
            var item = sale.Items[index];
            Equal(item.Number, (index + 1).ToString(CultureInfo.InvariantCulture), "nItem sequencial na ordem recebida");
            Item(item);
            if (index == 0) Equal(item.Description, NfcePreparation.HomologationProduct, "xProd do primeiro item em homologação");
        }
        foreach (var property in typeof(PreparationIcmsTotals).GetProperties())
            Money((string)property.GetValue(sale.Totals.Icms)!, "ICMSTot/" + property.Name);
        if (sale.Totals.InvoiceTotal is not null) Money(sale.Totals.InvoiceTotal, "vNFTot");
        if (sale.Totals.IbsCbs is not null) RtcTotals(sale.Totals.IbsCbs);
        if (sale.Payments.Length is < 1 or > 100) Fail("payments deve conter de 1 a 100 pagamentos.");
        foreach (var payment in sale.Payments)
        {
            Match(payment.Code, @"^[0-9]{2}$", "tPag");
            Money(payment.Value, "vPag");
            if (payment.Indicator is not null) OneOf(payment.Indicator, "indPag", "0", "1");
            OptionalText(payment.Description, 2, 60, "xPag");
            if (payment.Card is not null)
            {
                OneOf(payment.Card.Integration, "tpIntegra", "1", "2");
                if (payment.Card.Cnpj is not null) AccessKey.ValidateCnpj(payment.Card.Cnpj);
                if (payment.Card.RecipientCnpj is not null) AccessKey.ValidateCnpj(payment.Card.RecipientCnpj);
                if (payment.Card.Brand is not null) Match(payment.Card.Brand, @"^[0-9]{2}$", "tBand");
                OptionalText(payment.Card.Authorization, 1, 128, "cAut");
                OptionalText(payment.Card.Terminal, 1, 40, "idTermPag");
            }
        }
        if (sale.Change is not null) Money(sale.Change, "vTroco");
        OptionalText(sale.AdditionalInfo, 1, 5000, "infCpl");
    }

    public static void ValidateEmitter(PreparationEmitterProfile emitter)
    {
        RequireMembers(emitter);
        Version(emitter.Version);
        Text(emitter.ProfileId, 1, 60, "profileId");
        AccessKey.ValidateCnpj(emitter.Cnpj);
        Text(emitter.Name, 2, 60, "emit/xNome");
        OptionalText(emitter.FantasyName, 1, 60, "xFant");
        Match(emitter.StateRegistration, @"^[0-9]{2,14}$", "IE");
        OneOf(emitter.Crt, "CRT suportado", "1", "3");
        Match(emitter.Series, @"^(0|[1-9][0-9]{0,2})$", "serie");
        Match(emitter.FiscalMunicipality, @"^53[0-9]{5}$", "cMunFG");
        Text(emitter.ProcessVersion, 1, 20, "verProc");
        Address(emitter.Address);
        Equal(emitter.Address.Uf, "DF", "enderEmit/UF");
        if (emitter.Address.PostalCode is null) Fail("CEP do emitente é obrigatório no XSD fixado.");
    }

    public static void ValidateContext(PreparationTestContext context)
    {
        RequireMembers(context);
        Version(context.Version);
        if (!context.SyntheticOnly) Fail("A preparação aceita apenas contexto syntheticOnly=true.");
        Match(context.Number, @"^[1-9][0-9]{0,8}$", "nNF");
        Match(context.NumericCode, @"^[0-9]{8}$", "cNF");
    }

    internal static DateTimeOffset IssuedAt(string value)
    {
        Match(value, @"^20[0-9]{2}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}([+-](0[0-9]|10|11):00|\+12:00)$", "dhEmi com offset explícito do XSD");
        if (!DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result)) Fail("dhEmi inválida.");
        return result;
    }

    private static void Recipient(PreparationRecipient recipient)
    {
        OneOf(recipient.DocumentType, "documentType", "CPF", "CNPJ");
        if (recipient.DocumentType == "CNPJ") AccessKey.ValidateCnpj(recipient.Document);
        else Match(recipient.Document, @"^[0-9]{11}$", "CPF");
        Text(recipient.Name, 2, 60, "dest/xNome");
        Equal(recipient.Name, NfcePreparation.HomologationRecipient, "dest/xNome em homologação");
        // MOC E16a-10: modelo65 aceita indIEDest=9. Isso não define condição real.
        Equal(recipient.StateRegistrationIndicator, "9", "indIEDest de NFC-e");
        OneOf(recipient.CreditIntent, "creditIntent", "yes", "no", "unknown");
        if (recipient.Address is not null) Address(recipient.Address);
        OptionalText(recipient.Email, 1, 60, "email");
    }

    private static void Address(PreparationAddress address)
    {
        Text(address.Street, 2, 60, "xLgr");
        Text(address.Number, 1, 60, "nro");
        OptionalText(address.Complement, 1, 60, "xCpl");
        Text(address.District, 2, 60, "xBairro");
        Match(address.MunicipalityCode, @"^[0-9]{7}$", "cMun");
        Text(address.MunicipalityName, 2, 60, "xMun");
        Match(address.Uf, @"^[A-Z]{2}$", "UF");
        if (address.PostalCode is not null) Match(address.PostalCode, @"^[0-9]{8}$", "CEP");
        if (address.CountryCode is not null) Equal(address.CountryCode, "1058", "cPais neste subconjunto doméstico");
        if (address.CountryName is not null) OneOf(address.CountryName, "xPais", "Brasil", "BRASIL");
        if (address.Phone is not null) Match(address.Phone, @"^[0-9]{6,14}$", "fone");
    }

    private static void Item(PreparationItem item)
    {
        Match(item.Number, @"^[1-9][0-9]{0,2}$", "nItem");
        if (int.Parse(item.Number, CultureInfo.InvariantCulture) > 990) Fail("nItem excede 990.");
        Text(item.Code, 1, 60, "cProd");
        Gtin(item.Gtin, "cEAN");
        Text(item.Description, 1, 120, "xProd");
        Match(item.Ncm, @"^([0-9]{2}|[0-9]{8})$", "NCM");
        if (item.Cest is not null) Match(item.Cest, @"^[0-9]{7}$", "CEST");
        OptionalText(item.BenefitCode, 8, 10, "cBenef");
        Match(item.Cfop, @"^5[0-9]{3}$", "CFOP interno deste recorte");
        Text(item.CommercialUnit, 1, 6, "uCom");
        Match(item.CommercialQuantity, @"^(0|0\.[0-9]{1,4}|[1-9][0-9]{0,10}(\.[0-9]{1,4})?)$", "qCom");
        Match(item.CommercialUnitPrice, @"^(0|0\.[0-9]{1,10}|[1-9][0-9]{0,10}(\.[0-9]{1,10})?)$", "vUnCom");
        Money(item.ProductValue, "vProd");
        Gtin(item.TaxableGtin, "cEANTrib");
        Text(item.TaxableUnit, 1, 6, "uTrib");
        Match(item.TaxableQuantity, @"^(0|0\.[0-9]{1,4}|[1-9][0-9]{0,10}(\.[0-9]{1,4})?)$", "qTrib");
        Match(item.TaxableUnitPrice, @"^(0|0\.[0-9]{1,10}|[1-9][0-9]{0,10}(\.[0-9]{1,10})?)$", "vUnTrib");
        foreach (var (value, name) in new[] { (item.Freight, "vFrete"), (item.Insurance, "vSeg"), (item.Discount, "vDesc"), (item.OtherValue, "vOutro") })
            if (value is not null) Match(value, OptionalMoneyPattern, name);
        OneOf(item.TotalIndicator, "indTot", "0", "1");
        OptionalText(item.AdditionalInfo, 1, 500, "infAdProd");
        if (item.ItemTotal is not null) Money(item.ItemTotal, "vItem");
        Taxes(item.Taxes);
    }

    private static void Taxes(PreparationTaxes taxes)
    {
        Match(taxes.Icms.Origin, @"^[0-8]$", "orig");
        if (taxes.Icms.Group == "ICMS00")
        {
            Equal(taxes.Icms.Cst, "00", "CST de ICMS00");
            Absent(taxes.Icms.Csosn, "CSOSN em ICMS00");
            OneOf(taxes.Icms.BaseMode, "modBC", "0", "1", "2", "3");
            Money(taxes.Icms.Base, "vBC ICMS00");
            Rate(taxes.Icms.Rate, "pICMS");
            Money(taxes.Icms.Value, "vICMS");
        }
        else if (taxes.Icms.Group == "ICMSSN102")
        {
            Equal(taxes.Icms.Csosn, "102", "CSOSN deste subconjunto");
            Absent(taxes.Icms.Cst, "CST em ICMSSN102");
            Absent(taxes.Icms.BaseMode, "modBC em ICMSSN102");
            Absent(taxes.Icms.Base, "vBC em ICMSSN102");
            Absent(taxes.Icms.Rate, "pICMS em ICMSSN102");
            Absent(taxes.Icms.Value, "vICMS em ICMSSN102");
        }
        else Fail("Grupo ICMS não mapeado: " + taxes.Icms.Group);
        Contribution(taxes.Pis.Group, "PIS", taxes.Pis.Cst, taxes.Pis.Base, taxes.Pis.Rate, taxes.Pis.Value);
        Contribution(taxes.Cofins.Group, "COFINS", taxes.Cofins.Cst, taxes.Cofins.Base, taxes.Cofins.Rate, taxes.Cofins.Value);
        if (taxes.TotalTaxes is not null) Money(taxes.TotalTaxes, "vTotTrib");
        if (taxes.IbsCbs is not null)
        {
            Equal(taxes.IbsCbs.Cst, "000", "CST IBS/CBS suportado");
            Equal(taxes.IbsCbs.Classification, "000001", "cClassTrib suportado como hipótese de teste");
            var rtc = taxes.IbsCbs.Tax;
            Money(rtc.Base, "IBSCBS/vBC");
            Rate(rtc.Uf.Rate, "pIBSUF");
            Money(rtc.Uf.Value, "vIBSUF");
            Rate(rtc.Municipality.Rate, "pIBSMun");
            Money(rtc.Municipality.Value, "vIBSMun");
            Money(rtc.Ibs, "vIBS");
            Rate(rtc.Cbs.Rate, "pCBS");
            Money(rtc.Cbs.Value, "vCBS");
        }
    }

    private static void Contribution(string group, string prefix, string cst, string? basis, string? rate, string? value)
    {
        if (group == prefix + "Aliq")
        {
            OneOf(cst, prefix + "/CST", "01", "02");
            Money(basis, prefix + "/vBC");
            Rate(rate, prefix + "/alíquota");
            Money(value, prefix + "/valor");
        }
        else if (group == prefix + "NT")
        {
            OneOf(cst, prefix + "/CST", "04", "05", "06", "07", "08", "09");
            Absent(basis, prefix + "NT/vBC");
            Absent(rate, prefix + "NT/alíquota");
            Absent(value, prefix + "NT/valor");
        }
        else Fail("Grupo " + prefix + " não mapeado: " + group);
    }

    private static void RtcTotals(PreparationIbsCbsTotals totals)
    {
        Money(totals.Base, "IBSCBSTot/vBCIBSCBS");
        Money(totals.Ibs.Uf.Deferral, "IBSCBSTot/gIBSUF/vDif");
        Money(totals.Ibs.Uf.ReturnedTax, "IBSCBSTot/gIBSUF/vDevTrib");
        Money(totals.Ibs.Uf.Value, "IBSCBSTot/gIBSUF/vIBSUF");
        Money(totals.Ibs.Municipality.Deferral, "IBSCBSTot/gIBSMun/vDif");
        Money(totals.Ibs.Municipality.ReturnedTax, "IBSCBSTot/gIBSMun/vDevTrib");
        Money(totals.Ibs.Municipality.Value, "IBSCBSTot/gIBSMun/vIBSMun");
        Money(totals.Ibs.Value, "IBSCBSTot/vIBS");
        Money(totals.Ibs.PresumedCredit, "IBSCBSTot/gIBS/vCredPres");
        Money(totals.Ibs.ConditionalPresumedCredit, "IBSCBSTot/gIBS/vCredPresCondSus");
        Money(totals.Cbs.Deferral, "IBSCBSTot/gCBS/vDif");
        Money(totals.Cbs.ReturnedTax, "IBSCBSTot/gCBS/vDevTrib");
        Money(totals.Cbs.Value, "IBSCBSTot/vCBS");
        Money(totals.Cbs.PresumedCredit, "IBSCBSTot/gCBS/vCredPres");
        Money(totals.Cbs.ConditionalPresumedCredit, "IBSCBSTot/gCBS/vCredPresCondSus");
    }

    private static void RequireMembers(object? value)
    {
        if (value is null) Fail("Objeto obrigatório ausente.");
        if (value is string || value!.GetType().IsValueType) return;
        if (value is System.Collections.IEnumerable items)
        {
            foreach (var child in items) RequireMembers(child);
            return;
        }
        foreach (var property in value.GetType().GetProperties())
        {
            var member = property.GetValue(value);
            if (property.IsDefined(typeof(RequiredMemberAttribute)) && member is null)
                Fail("Campo obrigatório ausente/null: " + property.Name);
            if (member is not null) RequireMembers(member);
        }
    }

    private static void Version(string version) => Equal(version, PreparationVersion.Current, "version");
    private static void Money(string? value, string name) => Match(value, MoneyPattern, name);
    private static void Rate(string? value, string name) => Match(value, RatePattern, name);
    private static void Gtin(string value, string name) => Match(value, @"^(SEM GTIN|[0-9]{8}|[0-9]{12,14}|)$", name);
    private static void OptionalText(string? value, int min, int max, string name)
    {
        if (value is not null) Text(value, min, max, name);
    }
    private static void Text(string value, int min, int max, string name)
    {
        if (value.Length < min || value.Length > max || value != value.Trim())
            Fail("Texto fora da representação suportada: " + name);
        try { XmlConvert.VerifyXmlChars(value); }
        catch (XmlException exception) { throw new InvalidDataException("Caractere XML inválido: " + name, exception); }
    }
    private static void Match(string? value, string pattern, string name)
    {
        if (value is null || value != value.Trim() ||
            !Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
            Fail("Formato/precisão inválido ou campo ausente: " + name);
    }
    private static void Equal(string? value, string expected, string name)
    {
        if (value != expected) Fail(name + " deve ser " + expected + " neste subconjunto.");
    }
    private static void OneOf(string? value, string name, params string[] accepted)
    {
        if (value is null || !accepted.Contains(value, StringComparer.Ordinal)) Fail("Valor não suportado: " + name);
    }
    private static void Absent(string? value, string name)
    {
        if (value is not null) Fail("Campo fora do grupo selecionado: " + name);
    }
    [DoesNotReturn]
    private static void Fail(string message) => throw new InvalidDataException(message);
}
