namespace FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;

using System.Net;
using System.Text;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// El mail mensual de HU-82: un texto corto y el PDF del reporte adjunto, el mismo que descarga el
/// panel. El contenido vive en el PDF para que el mail y la exportación no puedan decir dos cosas
/// distintas del mismo mes.
/// </summary>
internal static class CompanyMonthlyReportEmail
{
    public static OutboundEmail Queue(
        Company company,
        CompanyReportPdf pdf,
        string panelUrl,
        DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(pdf);

        var content = Compose(pdf.Report, company.Language, panelUrl);

        return OutboundEmail.Queue(
            company.User.Email,
            content.Subject,
            content.Html,
            content.Text,
            queuedAt,
            EmailAttachment.From(pdf.FileName, pdf.Content));
    }

    public static CompanyMonthlyReportEmailContent Compose(
        CompanyMonthlyReportResponse report,
        Language language,
        string panelUrl)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Period is not { } period || report.PreviousPeriod is not { } previousPeriod)
        {
            throw new ArgumentException("El mail mensual solo se arma para un mes con foto.", nameof(report));
        }

        var texts = CompanyReportTexts.For(language);
        var month = texts.Locale.MonthAndYear(period);
        var paragraphs = new[]
        {
            texts.MailIntroduction(month, texts.Locale.MonthAndYear(previousPeriod)),
            texts.PrivacyNote(texts.Locale.Number(report.MinimumGroupSize)),
        };

        return new CompanyMonthlyReportEmailContent(
            texts.MailSubject(report.CompanyName, month),
            Html(texts.MailGreeting(report.CompanyName), paragraphs, texts, panelUrl),
            Text(texts.MailGreeting(report.CompanyName), paragraphs, texts, panelUrl));
    }

    private static string Text(string greeting, IReadOnlyList<string> paragraphs, CompanyReportTexts texts, string panelUrl)
    {
        var text = new StringBuilder();
        text.Append(greeting).Append('\n');

        foreach (var paragraph in paragraphs)
        {
            text.Append('\n').Append(paragraph).Append('\n');
        }

        text.Append('\n').Append(texts.MailPanelLink).Append(": ").Append(panelUrl).Append('\n');
        text.Append('\n').Append(texts.MailFooter).Append('\n');

        return text.ToString();
    }

    private static string Html(string greeting, IReadOnlyList<string> paragraphs, CompanyReportTexts texts, string panelUrl)
    {
        var html = new StringBuilder();
        html.Append("<p>").Append(Encode(greeting)).Append("</p>");

        foreach (var paragraph in paragraphs)
        {
            html.Append("<p>").Append(Encode(paragraph)).Append("</p>");
        }

        html.Append("<p><a href=\"").Append(Encode(panelUrl)).Append("\">").Append(Encode(texts.MailPanelLink)).Append("</a></p>");
        html.Append("<p><small>").Append(Encode(texts.MailFooter)).Append("</small></p>");

        return html.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

internal sealed record CompanyMonthlyReportEmailContent(string Subject, string Html, string Text);
