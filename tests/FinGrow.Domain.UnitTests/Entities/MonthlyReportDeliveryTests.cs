namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class MonthlyReportDeliveryTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_delivery_records_the_company_the_month_and_the_email_that_carries_it()
    {
        var emailId = Guid.CreateVersion7();

        var delivery = MonthlyReportDelivery.Record(CompanyId, September, emailId, Now);

        delivery.CompanyId.ShouldBe(CompanyId);
        delivery.PeriodStart.ShouldBe(September.Start);
        delivery.Period.ShouldBe(September);
        delivery.OutboundEmailId.ShouldBe(emailId);
        delivery.CreatedAt.ShouldBe(Now);
        delivery.QueuedAt.ShouldBe(Now);
    }

    [Fact]
    public void Requeueing_points_to_the_new_email_and_keeps_when_it_was_first_recorded()
    {
        var delivery = MonthlyReportDelivery.Record(CompanyId, September, Guid.CreateVersion7(), Now);
        var newEmailId = Guid.CreateVersion7();

        delivery.Requeue(newEmailId, Now.AddDays(1));

        delivery.OutboundEmailId.ShouldBe(newEmailId);
        delivery.QueuedAt.ShouldBe(Now.AddDays(1));
        delivery.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Requeueing_needs_a_different_email()
    {
        var emailId = Guid.CreateVersion7();
        var delivery = MonthlyReportDelivery.Record(CompanyId, September, emailId, Now);

        Should.Throw<DomainException>(() => delivery.Requeue(emailId, Now.AddDays(1)));
    }

    [Fact]
    public void A_delivery_always_belongs_to_a_company_and_an_email()
    {
        Should.Throw<DomainException>(() => MonthlyReportDelivery.Record(Guid.Empty, September, Guid.CreateVersion7(), Now));
        Should.Throw<DomainException>(() => MonthlyReportDelivery.Record(CompanyId, September, Guid.Empty, Now));
    }
}
