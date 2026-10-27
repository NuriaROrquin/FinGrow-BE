namespace FinGrow.Domain.UnitTests.ValueObjects;

using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class EmailAttachmentTests
{
    [Fact]
    public void An_attachment_keeps_its_name_and_its_content_in_base64()
    {
        var attachment = EmailAttachment.From(" reporte.pdf ", new byte[] { 37, 80, 68, 70 });

        attachment.FileName.ShouldBe("reporte.pdf");
        attachment.Base64Content.ShouldBe("JVBERg==");
        EmailAttachment.FromStored("reporte.pdf", "JVBERg==").ShouldBe(attachment);
    }

    [Fact]
    public void An_attachment_needs_a_name_and_some_content()
    {
        Should.Throw<DomainException>(() => EmailAttachment.From(" ", new byte[] { 1 }));
        Should.Throw<DomainException>(() => EmailAttachment.From("reporte.pdf", Array.Empty<byte>()));
        Should.Throw<DomainException>(() => EmailAttachment.From(new string('a', EmailAttachment.MaxFileNameLength + 1), new byte[] { 1 }));
    }

    [Fact]
    public void An_attachment_cannot_be_larger_than_the_limit()
    {
        Should.Throw<DomainException>(() => EmailAttachment.From("grande.pdf", new byte[EmailAttachment.MaxContentBytes + 1]));
    }
}
