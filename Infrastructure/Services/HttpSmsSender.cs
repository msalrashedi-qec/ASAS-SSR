using System.Net.Http.Headers;
using System.Text.Json;
using Core.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class HttpSmsSender(HttpClient httpClient, IOptions<SmsProviderOptions> options) : ISmsSender
{
    private readonly SmsProviderOptions _options = options.Value;

    public async Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            return new(false, 0, "The SMS provider endpoint is not configured.");

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
            return new(false, 0, "The SMS provider endpoint is invalid.");

        if (string.IsNullOrWhiteSpace(request.UserName)
            || string.IsNullOrWhiteSpace(request.ApiKey)
            || string.IsNullOrWhiteSpace(request.Sender))
            return new(false, 0, "The selected school's SMS credentials are incomplete.");

        if (string.IsNullOrWhiteSpace(request.Message))
            return new(false, 0, "The message cannot be empty.");

        var recipients = request.Recipients
            .Select(NormalizeMobile)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (recipients.Length == 0)
            return new(false, 0, "No valid recipient mobile numbers were supplied.");

        var form = new Dictionary<string, string>
        {
            [_options.UserNameField] = request.UserName,
            [_options.ApiKeyField] = request.ApiKey,
            [_options.SenderField] = request.Sender,
            [_options.RecipientsField] = string.Join(_options.RecipientSeparator, recipients),
            [_options.MessageField] = request.Message.Trim(),
            [_options.ResponseFormatField] = _options.ResponseFormat
        };

        // Mora accepts GET and POST. POST keeps the API key and message out of the URL.
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new(false, 0, $"The SMS provider rejected the request ({(int)response.StatusCode}).");

            var providerResult = ReadProviderResult(responseBody);
            if (providerResult.Code == 100)
                return new(true, recipients.Length);

            if (providerResult.Code is int errorCode)
                return new(false, 0, FormatProviderError(errorCode, providerResult.Message));

            return new(false, 0, "The SMS provider returned an invalid response.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, 0, "The SMS provider did not respond in time.");
        }
        catch (HttpRequestException)
        {
            return new(false, 0, "The SMS provider could not be reached.");
        }
    }

    private static ProviderResult ReadProviderResult(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return new(null, null);

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (TryGetProperty(root, "data", out var data))
            {
                var code = ReadInt(data, "code");
                if (code is not null)
                    return new(code, ReadString(data, "message"));
            }

            if (TryGetProperty(root, "status", out var status))
                return new(ReadInt(status, "code"), ReadString(status, "message"));

            return new(ReadInt(root, "code"), ReadString(root, "message"));
        }
        catch (JsonException)
        {
            return new(null, null);
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number,
            _ => null
        };
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string FormatProviderError(int code, string? providerMessage)
    {
        var message = code switch
        {
            105 => "Insufficient SMS balance / لا يوجد رصيد كافٍ",
            106 => "Sender name is unavailable / اسم المرسل غير متاح",
            107 => "Sender name is blocked / اسم المرسل محظور",
            108 => "No valid recipient numbers / لا توجد أرقام صالحة للإرسال",
            112 => "The message contains prohibited words / الرسالة تحتوي كلمات محظورة",
            114 => "The sending account is suspended / حساب الإرسال موقوف",
            115 => "The mobile number is not activated / رقم الجوال غير مفعل",
            116 => "The email address is not activated / البريد الإلكتروني غير مفعل",
            117 => "The message is empty / الرسالة فارغة",
            118 => "The sender name is empty / اسم المرسل فارغ",
            119 => "No recipient number was provided / لم يتم إدخال رقم مستلم",
            _ => providerMessage ?? "The SMS provider rejected the message."
        };

        return $"{code}: {message}";
    }

    private string NormalizeMobile(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.StartsWith('0') && !string.IsNullOrWhiteSpace(_options.CountryCode))
            digits = _options.CountryCode.Trim() + digits[1..];

        return digits;
    }

    private sealed record ProviderResult(int? Code, string? Message);
}
