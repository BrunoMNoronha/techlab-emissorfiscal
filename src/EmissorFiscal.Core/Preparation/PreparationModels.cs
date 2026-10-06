using System.Text.Json.Serialization;
using EmissorFiscal.Core.Domain;

namespace EmissorFiscal.Core.Preparation;

// Contrato local de preparação com dados sintéticos. Não é DTO da API simulada
// nem autorização para transmissão. Strings mantêm códigos, escala e zeros.
public static class PreparationVersion
{
    public const string Current = "nfce-preparation/0.1";
}

public sealed record PreparedNfce(AccessKey AccessKey, string UnsignedXml);

public sealed record PreparationSale
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    [JsonPropertyName("reference")]
    public required string Reference { get; init; }
    [JsonPropertyName("emitterProfileId")]
    public required string EmitterProfileId { get; init; }
    [JsonPropertyName("issuedAt")]
    public required string IssuedAt { get; init; }
    [JsonPropertyName("operation")]
    public required PreparationOperation Operation { get; init; }
    [JsonPropertyName("recipient")]
    public PreparationRecipient? Recipient { get; init; }
    [JsonPropertyName("items")]
    public required PreparationItem[] Items { get; init; }
    [JsonPropertyName("totals")]
    public required PreparationTotals Totals { get; init; }
    [JsonPropertyName("payments")]
    public required PreparationPayment[] Payments { get; init; }
    [JsonPropertyName("change")]
    public string? Change { get; init; }
    [JsonPropertyName("additionalInfo")]
    public string? AdditionalInfo { get; init; }
}

public sealed record PreparationEmitterProfile
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    [JsonPropertyName("profileId")]
    public required string ProfileId { get; init; }
    [JsonPropertyName("cnpj")]
    public required string Cnpj { get; init; }
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("fantasyName")]
    public string? FantasyName { get; init; }
    [JsonPropertyName("stateRegistration")]
    public required string StateRegistration { get; init; }
    [JsonPropertyName("crt")]
    public required string Crt { get; init; }
    [JsonPropertyName("series")]
    public required string Series { get; init; }
    [JsonPropertyName("fiscalMunicipality")]
    public required string FiscalMunicipality { get; init; }
    [JsonPropertyName("processVersion")]
    public required string ProcessVersion { get; init; }
    [JsonPropertyName("address")]
    public required PreparationAddress Address { get; init; }
}

public sealed record PreparationTestContext
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    [JsonPropertyName("syntheticOnly")]
    public required bool SyntheticOnly { get; init; }
    [JsonPropertyName("number")]
    public required string Number { get; init; }
    [JsonPropertyName("numericCode")]
    public required string NumericCode { get; init; }
}

public sealed record PreparationOperation
{
    [JsonPropertyName("natOp")]
    public required string Nature { get; init; }
    [JsonPropertyName("tpNF")]
    public required string InvoiceType { get; init; }
    [JsonPropertyName("idDest")]
    public required string Destination { get; init; }
    [JsonPropertyName("finNFe")]
    public required string Purpose { get; init; }
    [JsonPropertyName("indFinal")]
    public required string FinalConsumer { get; init; }
    [JsonPropertyName("indPres")]
    public required string Presence { get; init; }
    [JsonPropertyName("modFrete")]
    public required string FreightMode { get; init; }
}

public sealed record PreparationAddress
{
    [JsonPropertyName("street")]
    public required string Street { get; init; }
    [JsonPropertyName("number")]
    public required string Number { get; init; }
    [JsonPropertyName("complement")]
    public string? Complement { get; init; }
    [JsonPropertyName("district")]
    public required string District { get; init; }
    [JsonPropertyName("municipalityCode")]
    public required string MunicipalityCode { get; init; }
    [JsonPropertyName("municipalityName")]
    public required string MunicipalityName { get; init; }
    [JsonPropertyName("uf")]
    public required string Uf { get; init; }
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; init; }
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }
    [JsonPropertyName("countryName")]
    public string? CountryName { get; init; }
    [JsonPropertyName("phone")]
    public string? Phone { get; init; }
}

public sealed record PreparationRecipient
{
    [JsonPropertyName("documentType")]
    public required string DocumentType { get; init; }
    [JsonPropertyName("document")]
    public required string Document { get; init; }
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("indIEDest")]
    public required string StateRegistrationIndicator { get; init; }
    [JsonPropertyName("creditIntent")]
    public required string CreditIntent { get; init; }
    [JsonPropertyName("address")]
    public PreparationAddress? Address { get; init; }
    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public sealed record PreparationItem
{
    [JsonPropertyName("nItem")]
    public required string Number { get; init; }
    [JsonPropertyName("cProd")]
    public required string Code { get; init; }
    [JsonPropertyName("cEAN")]
    public required string Gtin { get; init; }
    [JsonPropertyName("xProd")]
    public required string Description { get; init; }
    [JsonPropertyName("NCM")]
    public required string Ncm { get; init; }
    [JsonPropertyName("CEST")]
    public string? Cest { get; init; }
    [JsonPropertyName("cBenef")]
    public string? BenefitCode { get; init; }
    [JsonPropertyName("CFOP")]
    public required string Cfop { get; init; }
    [JsonPropertyName("uCom")]
    public required string CommercialUnit { get; init; }
    [JsonPropertyName("qCom")]
    public required string CommercialQuantity { get; init; }
    [JsonPropertyName("vUnCom")]
    public required string CommercialUnitPrice { get; init; }
    [JsonPropertyName("vProd")]
    public required string ProductValue { get; init; }
    [JsonPropertyName("cEANTrib")]
    public required string TaxableGtin { get; init; }
    [JsonPropertyName("uTrib")]
    public required string TaxableUnit { get; init; }
    [JsonPropertyName("qTrib")]
    public required string TaxableQuantity { get; init; }
    [JsonPropertyName("vUnTrib")]
    public required string TaxableUnitPrice { get; init; }
    [JsonPropertyName("vFrete")]
    public string? Freight { get; init; }
    [JsonPropertyName("vSeg")]
    public string? Insurance { get; init; }
    [JsonPropertyName("vDesc")]
    public string? Discount { get; init; }
    [JsonPropertyName("vOutro")]
    public string? OtherValue { get; init; }
    [JsonPropertyName("indTot")]
    public required string TotalIndicator { get; init; }
    [JsonPropertyName("taxes")]
    public required PreparationTaxes Taxes { get; init; }
    [JsonPropertyName("additionalInfo")]
    public string? AdditionalInfo { get; init; }
    [JsonPropertyName("vItem")]
    public string? ItemTotal { get; init; }
}

public sealed record PreparationTaxes
{
    [JsonPropertyName("ICMS")]
    public required PreparationIcms Icms { get; init; }
    [JsonPropertyName("PIS")]
    public required PreparationPis Pis { get; init; }
    [JsonPropertyName("COFINS")]
    public required PreparationCofins Cofins { get; init; }
    [JsonPropertyName("IBSCBS")]
    public PreparationIbsCbs? IbsCbs { get; init; }
    [JsonPropertyName("vTotTrib")]
    public string? TotalTaxes { get; init; }
}

public sealed record PreparationIcms
{
    [JsonPropertyName("group")]
    public required string Group { get; init; }
    [JsonPropertyName("orig")]
    public required string Origin { get; init; }
    [JsonPropertyName("CST")]
    public string? Cst { get; init; }
    [JsonPropertyName("CSOSN")]
    public string? Csosn { get; init; }
    [JsonPropertyName("modBC")]
    public string? BaseMode { get; init; }
    [JsonPropertyName("vBC")]
    public string? Base { get; init; }
    [JsonPropertyName("pICMS")]
    public string? Rate { get; init; }
    [JsonPropertyName("vICMS")]
    public string? Value { get; init; }
}

public sealed record PreparationPis
{
    [JsonPropertyName("group")]
    public required string Group { get; init; }
    [JsonPropertyName("CST")]
    public required string Cst { get; init; }
    [JsonPropertyName("vBC")]
    public string? Base { get; init; }
    [JsonPropertyName("pPIS")]
    public string? Rate { get; init; }
    [JsonPropertyName("vPIS")]
    public string? Value { get; init; }
}

public sealed record PreparationCofins
{
    [JsonPropertyName("group")]
    public required string Group { get; init; }
    [JsonPropertyName("CST")]
    public required string Cst { get; init; }
    [JsonPropertyName("vBC")]
    public string? Base { get; init; }
    [JsonPropertyName("pCOFINS")]
    public string? Rate { get; init; }
    [JsonPropertyName("vCOFINS")]
    public string? Value { get; init; }
}

public sealed record PreparationIbsCbs
{
    [JsonPropertyName("CST")]
    public required string Cst { get; init; }
    [JsonPropertyName("cClassTrib")]
    public required string Classification { get; init; }
    [JsonPropertyName("gIBSCBS")]
    public required PreparationIbsCbsTax Tax { get; init; }
}

public sealed record PreparationIbsCbsTax
{
    [JsonPropertyName("vBC")]
    public required string Base { get; init; }
    [JsonPropertyName("gIBSUF")]
    public required PreparationIbsUf Uf { get; init; }
    [JsonPropertyName("gIBSMun")]
    public required PreparationIbsMunicipality Municipality { get; init; }
    [JsonPropertyName("vIBS")]
    public required string Ibs { get; init; }
    [JsonPropertyName("gCBS")]
    public required PreparationCbs Cbs { get; init; }
}

public sealed record PreparationIbsUf
{
    [JsonPropertyName("pIBSUF")]
    public required string Rate { get; init; }
    [JsonPropertyName("vIBSUF")]
    public required string Value { get; init; }
}

public sealed record PreparationIbsMunicipality
{
    [JsonPropertyName("pIBSMun")]
    public required string Rate { get; init; }
    [JsonPropertyName("vIBSMun")]
    public required string Value { get; init; }
}

public sealed record PreparationCbs
{
    [JsonPropertyName("pCBS")]
    public required string Rate { get; init; }
    [JsonPropertyName("vCBS")]
    public required string Value { get; init; }
}

public sealed record PreparationTotals
{
    [JsonPropertyName("ICMSTot")]
    public required PreparationIcmsTotals Icms { get; init; }
    [JsonPropertyName("IBSCBSTot")]
    public PreparationIbsCbsTotals? IbsCbs { get; init; }
    [JsonPropertyName("vNFTot")]
    public string? InvoiceTotal { get; init; }
}

public sealed record PreparationIcmsTotals
{
    [JsonPropertyName("vBC")]
    public required string vBC { get; init; }
    [JsonPropertyName("vICMS")]
    public required string vICMS { get; init; }
    [JsonPropertyName("vICMSDeson")]
    public required string vICMSDeson { get; init; }
    [JsonPropertyName("vFCP")]
    public required string vFCP { get; init; }
    [JsonPropertyName("vBCST")]
    public required string vBCST { get; init; }
    [JsonPropertyName("vST")]
    public required string vST { get; init; }
    [JsonPropertyName("vFCPST")]
    public required string vFCPST { get; init; }
    [JsonPropertyName("vFCPSTRet")]
    public required string vFCPSTRet { get; init; }
    [JsonPropertyName("vProd")]
    public required string vProd { get; init; }
    [JsonPropertyName("vFrete")]
    public required string vFrete { get; init; }
    [JsonPropertyName("vSeg")]
    public required string vSeg { get; init; }
    [JsonPropertyName("vDesc")]
    public required string vDesc { get; init; }
    [JsonPropertyName("vII")]
    public required string vII { get; init; }
    [JsonPropertyName("vIPI")]
    public required string vIPI { get; init; }
    [JsonPropertyName("vIPIDevol")]
    public required string vIPIDevol { get; init; }
    [JsonPropertyName("vPIS")]
    public required string vPIS { get; init; }
    [JsonPropertyName("vCOFINS")]
    public required string vCOFINS { get; init; }
    [JsonPropertyName("vOutro")]
    public required string vOutro { get; init; }
    [JsonPropertyName("vNF")]
    public required string vNF { get; init; }
}

public sealed record PreparationIbsCbsTotals
{
    [JsonPropertyName("vBCIBSCBS")]
    public required string Base { get; init; }
    [JsonPropertyName("gIBS")]
    public required PreparationIbsTotals Ibs { get; init; }
    [JsonPropertyName("gCBS")]
    public required PreparationCbsTotals Cbs { get; init; }
}

public sealed record PreparationIbsTotals
{
    [JsonPropertyName("gIBSUF")]
    public required PreparationIbsUfTotals Uf { get; init; }
    [JsonPropertyName("gIBSMun")]
    public required PreparationIbsMunicipalityTotals Municipality { get; init; }
    [JsonPropertyName("vIBS")]
    public required string Value { get; init; }
    [JsonPropertyName("vCredPres")]
    public required string PresumedCredit { get; init; }
    [JsonPropertyName("vCredPresCondSus")]
    public required string ConditionalPresumedCredit { get; init; }
}

public sealed record PreparationIbsUfTotals
{
    [JsonPropertyName("vDif")]
    public required string Deferral { get; init; }
    [JsonPropertyName("vDevTrib")]
    public required string ReturnedTax { get; init; }
    [JsonPropertyName("vIBSUF")]
    public required string Value { get; init; }
}

public sealed record PreparationIbsMunicipalityTotals
{
    [JsonPropertyName("vDif")]
    public required string Deferral { get; init; }
    [JsonPropertyName("vDevTrib")]
    public required string ReturnedTax { get; init; }
    [JsonPropertyName("vIBSMun")]
    public required string Value { get; init; }
}

public sealed record PreparationCbsTotals
{
    [JsonPropertyName("vDif")]
    public required string Deferral { get; init; }
    [JsonPropertyName("vDevTrib")]
    public required string ReturnedTax { get; init; }
    [JsonPropertyName("vCBS")]
    public required string Value { get; init; }
    [JsonPropertyName("vCredPres")]
    public required string PresumedCredit { get; init; }
    [JsonPropertyName("vCredPresCondSus")]
    public required string ConditionalPresumedCredit { get; init; }
}

public sealed record PreparationPayment
{
    [JsonPropertyName("tPag")]
    public required string Code { get; init; }
    [JsonPropertyName("vPag")]
    public required string Value { get; init; }
    [JsonPropertyName("indPag")]
    public string? Indicator { get; init; }
    [JsonPropertyName("xPag")]
    public string? Description { get; init; }
    [JsonPropertyName("card")]
    public PreparationCard? Card { get; init; }
}

public sealed record PreparationCard
{
    [JsonPropertyName("tpIntegra")]
    public required string Integration { get; init; }
    [JsonPropertyName("CNPJ")]
    public string? Cnpj { get; init; }
    [JsonPropertyName("tBand")]
    public string? Brand { get; init; }
    [JsonPropertyName("cAut")]
    public string? Authorization { get; init; }
    [JsonPropertyName("CNPJReceb")]
    public string? RecipientCnpj { get; init; }
    [JsonPropertyName("idTermPag")]
    public string? Terminal { get; init; }
}
