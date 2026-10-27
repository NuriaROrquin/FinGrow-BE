namespace FinGrow.Application.UnitTests.Features.Metrics.MonthlyReport;

using System.Text.Json;
using FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using static MonthlyReportMailingScenario;

public class CompanyMonthlyReportEmailTests
{
    private readonly MonthlyReportMailingScenario _scenario = new();

    [Fact]
    public async Task The_spanish_email_introduces_the_attached_report_and_links_to_the_panel()
    {
        _scenario.PhotographAugustAndSeptember();

        var email = CompanyMonthlyReportEmail.Compose(await _scenario.SeptemberReportAsync(), Language.es, PanelUrl);

        email.Subject.ShouldBe("Reporte mensual de Acme S.A.: septiembre de 2026");
        email.Text.ShouldStartWith("Hola, Acme S.A.:");
        email.Text.ShouldContain("Te adjuntamos el reporte de bienestar financiero de septiembre de 2026, comparado con agosto de 2026");
        email.Text.ShouldContain("Los indicadores de grupos de menos de 3 empleados no se muestran.");
        email.Text.ShouldContain($"Ver el panel de la empresa: {PanelUrl}");
        email.Text.ShouldContain("Configuración → Notificaciones");
    }

    [Fact]
    public async Task The_english_and_portuguese_emails_use_their_own_months()
    {
        _scenario.PhotographAugustAndSeptember();
        var report = await _scenario.SeptemberReportAsync();

        var english = CompanyMonthlyReportEmail.Compose(report, Language.en, PanelUrl);
        var portuguese = CompanyMonthlyReportEmail.Compose(report, Language.pt, PanelUrl);

        english.Subject.ShouldBe("Acme S.A. monthly report: September 2026");
        english.Text.ShouldContain("compared with August 2026");
        portuguese.Subject.ShouldBe("Relatório mensal de Acme S.A.: setembro de 2026");
        portuguese.Text.ShouldContain("comparado com agosto de 2026");
    }

    [Fact]
    public async Task The_html_escapes_what_comes_from_the_company()
    {
        var company = CompanyStaffScenario.CreateCompany("Pérez & Hijos <S.A.>", "rrhh@perez.com");
        _scenario.PhotographAugustAndSeptember(company);
        var report = (await _scenario.Builder.BuildAsync(company, September, CancellationToken.None)).ShouldNotBeNull();

        var email = CompanyMonthlyReportEmail.Compose(report, Language.es, PanelUrl);

        email.Html.ShouldContain("&amp; Hijos &lt;S.A.&gt;");
        email.Html.ShouldNotContain("<S.A.>");
        email.Html.ShouldContain($"<a href=\"{PanelUrl}\">");
    }

    [Fact]
    public async Task The_email_goes_to_the_company_account_in_its_language_with_the_pdf_attached()
    {
        _scenario.Company.UpdatePreferences(Theme.Light, Language.en, Currency.ARS, 1, Now);
        _scenario.PhotographAugustAndSeptember();
        var pdf = (await _scenario.PdfBuilder.BuildAsync(_scenario.Company, September, Now, CancellationToken.None)).ShouldNotBeNull();

        var email = CompanyMonthlyReportEmail.Queue(_scenario.Company, pdf, PanelUrl, Now);

        email.Recipient.ShouldBe(Email.From("rrhh@acme.com"));
        email.Subject.ShouldBe("Acme S.A. monthly report: September 2026");
        var attachment = email.Attachment.ShouldNotBeNull();
        attachment.FileName.ShouldBe("company-report_202609.pdf");
        Convert.FromBase64String(attachment.Base64Content).ShouldBe(FakeCompanyReportPdfRenderer.Pdf);
    }

    [Fact]
    public async Task Neither_the_email_nor_the_pdf_carry_an_employee_identifier()
    {
        var staff = new CompanyStaffScenario();
        var ana = staff.AddEmployee(department: null, company: _scenario.Company, email: "ana.gomez@acme.com", fullName: "Ana Gomez");
        var scored = _scenario.PhotographAugustAndSeptember();
        var pdf = (await _scenario.PdfBuilder.BuildAsync(_scenario.Company, September, Now, CancellationToken.None)).ShouldNotBeNull();

        var email = CompanyMonthlyReportEmail.Queue(_scenario.Company, pdf, PanelUrl, Now);

        var everything = email.Subject + email.HtmlBody + email.TextBody
            + JsonSerializer.Serialize(_scenario.Renderer.Rendered.ShouldHaveSingleItem());
        foreach (var employeeId in scored.Append(ana.Id))
        {
            everything.ShouldNotContain(employeeId.ToString());
        }

        everything.ShouldNotContain("Ana Gomez");
        everything.ShouldNotContain("ana.gomez@acme.com");
        everything.ShouldNotContain(_scenario.Company.Id.ToString());
    }
}
