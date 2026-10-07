namespace Infrastructure.Services;

public sealed class SmsProviderOptions
{
    public const string SectionName = "SmsProvider";

    public string Endpoint { get; set; } = string.Empty;
    public string UserNameField { get; set; } = "username";
    public string ApiKeyField { get; set; } = "api_key";
    public string SenderField { get; set; } = "sender";
    public string RecipientsField { get; set; } = "numbers";
    public string MessageField { get; set; } = "message";
    public string ResponseFormatField { get; set; } = "return";
    public string ResponseFormat { get; set; } = "json";
    public string RecipientSeparator { get; set; } = ",";
    public string CountryCode { get; set; } = "966";
}
