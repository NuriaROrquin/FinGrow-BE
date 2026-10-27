namespace FinGrow.Application.UnitTests.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using static MonthlyReportMailingScenario;

public class CompanyMonthlyReportMailerTests
{
    private readonly MonthlyReportMailingScenario _scenario = new();

    [Fact]
    public async Task A_subscribed_company_gets_the_last_closed_month_by_email_and_the_delivery_is_recorded()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.Queued);
        var email = _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
        email.Recipient.ShouldBe(Email.From("rrhh@acme.com"));
        email.Subject.ShouldBe("Reporte mensual de Acme S.A.: septiembre de 2026");
        email.Status.ShouldBe(OutboundEmailStatus.Pending);
        var attachment = email.Attachment.ShouldNotBeNull();
        attachment.FileName.ShouldBe("reporte-empresa_202609.pdf");
        Convert.FromBase64String(attachment.Base64Content).ShouldBe(FakeCompanyReportPdfRenderer.Pdf);
        _scenario.Renderer.Rendered.ShouldHaveSingleItem().Header
            .ShouldContain(field => field.Value == "Septiembre de 2026");
        var delivery = _scenario.Deliveries.Deliveries.ShouldHaveSingleItem();
        delivery.CompanyId.ShouldBe(_scenario.Company.Id);
        delivery.PeriodStart.ShouldBe(September.Start);
        delivery.OutboundEmailId.ShouldBe(email.Id);
        _scenario.UnitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_company_that_never_turned_it_on_gets_nothing()
    {
        _scenario.PhotographAugustAndSeptember();

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.NotSubscribed);
        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
        _scenario.Deliveries.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_company_that_turned_it_off_gets_nothing()
    {
        var setting = _scenario.Subscribe(Now.AddMonths(-2));
        setting.Change(isEnabled: false, NotificationCadence.Monthly, Now.AddDays(-3));
        _scenario.PhotographAugustAndSeptember();

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.NotSubscribed);
        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_month_that_had_already_closed_when_the_company_subscribed_is_not_sent()
    {
        _scenario.Subscribe(SeptemberCloses.AddMinutes(1));
        _scenario.PhotographAugustAndSeptember();

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.ClosedBeforeSubscribing);
        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
        _scenario.Deliveries.Deliveries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Subscribing_on_the_last_evening_of_the_month_in_Argentina_already_counts_for_that_month()
    {
        // 30/9 a las 23:59 de Argentina ya es 1/10 en UTC.
        _scenario.Subscribe(SeptemberCloses.AddMinutes(-1));
        _scenario.PhotographAugustAndSeptember();

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.Queued);
    }

    [Fact]
    public async Task Without_the_photo_of_the_month_it_waits_and_records_nothing()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.TakeSnapshot(August, PeriodMetrics.From(10, 5, 100, 2, 4, 1, 3, 4, 240));

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.WaitingForSnapshot);
        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
        _scenario.Deliveries.Deliveries.ShouldBeEmpty();
        _scenario.UnitOfWork.SaveCount.ShouldBe(0);
        _scenario.Renderer.Rendered.ShouldBeEmpty();
    }

    [Fact]
    public async Task Running_again_the_same_month_does_not_send_a_second_email()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();

        await Send();
        var second = await Send(Now.AddDays(1));

        second.ShouldBe(MonthlyReportMailOutcome.AlreadyQueued);
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
        _scenario.Deliveries.Deliveries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_email_that_was_already_sent_is_not_sent_again()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();
        await Send();
        _scenario.OutboundEmails.Emails.Single().MarkSent("re_123", Now.AddMinutes(1));

        var second = await Send(Now.AddDays(1));

        second.ShouldBe(MonthlyReportMailOutcome.AlreadyQueued);
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_abandoned_email_is_queued_again_and_the_delivery_points_to_the_new_one()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();
        await Send();
        var abandoned = _scenario.OutboundEmails.Emails.Single();
        Abandon(abandoned);

        var second = await Send(Now.AddDays(1));

        second.ShouldBe(MonthlyReportMailOutcome.Requeued);
        _scenario.OutboundEmails.Emails.Count.ShouldBe(2);
        var retry = _scenario.OutboundEmails.Emails.Single(email => email.Id != abandoned.Id);
        retry.Status.ShouldBe(OutboundEmailStatus.Pending);
        var delivery = _scenario.Deliveries.Deliveries.ShouldHaveSingleItem();
        delivery.OutboundEmailId.ShouldBe(retry.Id);
        delivery.QueuedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public async Task A_month_below_the_anonymity_threshold_is_still_sent_and_the_pdf_explains_why_it_has_no_data()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.TakeSnapshot(August, PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 3, 210));
        _scenario.TakeSnapshot(September, PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 2, 140));

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.Queued);
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem().Attachment.ShouldNotBeNull();
        var content = _scenario.Renderer.Rendered.ShouldHaveSingleItem();
        content.AnonymizedNotice.ShouldBe(
            "En septiembre de 2026 hubo menos de 3 empleados activos. Los indicadores se ocultan para no identificar a nadie.");
        content.Indicators.ShouldAllBe(card => card.Value == "Oculto");
    }

    [Fact]
    public async Task If_the_ai_does_not_answer_the_report_still_goes_out_without_recommendations()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();
        _scenario.Ai.Unreachable = true;

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.Queued);
        var content = _scenario.Renderer.Rendered.ShouldHaveSingleItem();
        content.Recommendations.Notice.ShouldBe(
            "No pudimos generar recomendaciones ahora. Probá de nuevo más tarde; mientras tanto, los indicadores están en Principales Preocupaciones.");
        content.Concerns.Groups.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Only_the_photos_of_its_own_company_count()
    {
        var other = CompanyStaffScenario.CreateCompany("Otra S.A.", "rrhh@otra.com");
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember(other);

        var outcome = await Send();

        outcome.ShouldBe(MonthlyReportMailOutcome.WaitingForSnapshot);
        _scenario.OutboundEmails.Emails.ShouldBeEmpty();
    }

    private Task<MonthlyReportMailOutcome> Send(DateTimeOffset? now = null) =>
        _scenario.Mailer().SendAsync(_scenario.Company, now ?? Now, CancellationToken.None);

    private static void Abandon(OutboundEmail email)
    {
        for (var attempt = 0; attempt < OutboundEmail.MaxAttempts; attempt++)
        {
            email.MarkFailed("Resend no respondió", Now.AddMinutes(attempt));
        }

        email.Status.ShouldBe(OutboundEmailStatus.Abandoned);
    }
}
