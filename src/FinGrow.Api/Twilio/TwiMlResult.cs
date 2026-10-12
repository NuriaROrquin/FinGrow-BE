namespace FinGrow.Api.Twilio;

using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

internal sealed class TwiMlResult : ContentResult
{
    public const string MediaType = "application/xml";

    public TwiMlResult(string? message)
    {
        var response = new XElement("Response");

        if (!string.IsNullOrEmpty(message))
        {
            response.Add(new XElement("Message", message));
        }

        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), response);

        Content = document.Declaration + document.ToString(SaveOptions.DisableFormatting);
        ContentType = MediaType;
        StatusCode = StatusCodes.Status200OK;
    }
}
