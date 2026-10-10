namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class TransactionReceiptConfiguration : IEntityTypeConfiguration<TransactionReceipt>
{
    public void Configure(EntityTypeBuilder<TransactionReceipt> builder)
    {
        builder.ToTable("transaction_receipts", table =>
            table.HasCheckConstraint(
                "ck_transaction_receipts_size_range",
                $"size_bytes > 0 AND size_bytes <= {TransactionReceipt.MaxSizeBytes}"));

        builder.HasKey(receipt => receipt.Id);

        builder.Property(receipt => receipt.StorageKey)
            .HasMaxLength(TransactionReceipt.MaxStorageKeyLength)
            .IsRequired();

        builder.Property(receipt => receipt.ContentType)
            .HasMaxLength(TransactionReceipt.MaxContentTypeLength)
            .IsRequired();

        builder.Property(receipt => receipt.FileName)
            .HasMaxLength(TransactionReceipt.MaxFileNameLength);

        builder.Property(receipt => receipt.SizeBytes).IsRequired();
        builder.Property(receipt => receipt.UploadedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(receipt => receipt.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Si el movimiento desaparece, el comprobante queda suelto en lugar de perderse: es la
        // misma situación que cuando el OCR falla y todavía nadie lo asoció a nada.
        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(receipt => receipt.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(receipt => receipt.StorageKey).IsUnique();
        builder.HasIndex(receipt => receipt.EmployeeId);
        builder.HasIndex(receipt => receipt.TransactionId);
    }
}
