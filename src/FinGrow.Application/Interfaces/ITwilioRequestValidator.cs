namespace FinGrow.Application.Interfaces;

public interface ITwilioRequestValidator
{
    bool IsValid(string url, IReadOnlyDictionary<string, string> form, string? signature);
}
