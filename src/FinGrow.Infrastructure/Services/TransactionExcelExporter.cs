namespace FinGrow.Infrastructure.Services;

using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Transactions.GetHistory;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using Microsoft.Extensions.Logging;

internal sealed class TransactionExcelExporter(
    ITransactionReadRepository transactionReadRepository,
    ILogger<TransactionExcelExporter> logger) : ITransactionExcelExporter
{
    private static readonly string TemplatePath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "Templates",
        "TransactionsTemplate.xlsx");

    public async Task<Result<byte[]>> ExportAsync(
        Guid employeeId,
        TransactionFilters filters,
        CancellationToken cancellationToken = default)
    {
        if (filters.DateFrom.HasValue
            && filters.DateTo.HasValue
            && filters.DateFrom > filters.DateTo)
        {
            return Result.Failure<byte[]>(Error.Validation(
                "Transactions.InvalidDateRange",
                "La fecha desde no puede ser posterior a la fecha hasta."));
        }

        try
        {
            await using var templateStream = new FileStream(
                TemplatePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using var workbook = new XLWorkbook(templateStream);

            var transactions = await transactionReadRepository.GetFilteredAsync(
                employeeId,
                filters.Search,
                filters.Type,
                filters.EffectiveStatuses,
                filters.ExpenseCategory,
                filters.IncomeCategory,
                filters.PaymentMethod,
                filters.DateFrom,
                filters.DateTo,
                cancellationToken);

            FillWorksheet(workbook.Worksheets.First(), transactions, filters.DateFrom, filters.DateTo);

            using var output = new MemoryStream();
            workbook.SaveAs(output);
            return Result.Success(output.ToArray());
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            logger.LogError("No se encontro la plantilla de transacciones en {TemplatePath}.", TemplatePath);
            return Result.Failure<byte[]>(Error.NotFound(
                "Transactions.TemplateNotFound",
                "No se encontro la plantilla para exportar transacciones."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "No se pudo leer la plantilla de transacciones.");
            return Result.Failure<byte[]>(Error.Failure(
                "Transactions.ExportFailed",
                "No se pudo generar el archivo de transacciones."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error inesperado al generar la exportacion de transacciones.");
            return Result.Failure<byte[]>(Error.Failure(
                "Transactions.ExportFailed",
                "No se pudo generar el archivo de transacciones."));
        }
    }

    private static void FillWorksheet(
        IXLWorksheet worksheet,
        IReadOnlyList<Transaction> transactions,
        DateOnly? dateFrom,
        DateOnly? dateTo)
    {
        var usedRows = worksheet.RowsUsed().ToList();
        var headerRow = usedRows
            .FirstOrDefault(row => row.CellsUsed().Any(cell => IsHeader(cell.GetString(), "Fecha")))
            ?? usedRows.FirstOrDefault();

        if (headerRow is null)
        {
            throw new InvalidDataException("La plantilla no contiene una fila de encabezados.");
        }

        var columns = headerRow.CellsUsed()
            .ToDictionary(cell => Normalize(cell.GetString()), cell => cell.Address.ColumnNumber);

        var dateColumn = FindColumn(columns, "Fecha");
        var descriptionColumn = FindColumn(columns, "Descripcion");
        var categoryColumn = FindColumn(columns, "Categoria");
        var paymentMethodColumn = FindOptionalColumn(columns, "Metodo de Pago");
        var amountColumn = FindColumn(columns, "Importe", "Monto");
        var statusColumn = FindColumn(columns, "Estado");
        worksheet.Column(amountColumn).InsertColumnsBefore(1);
        var currencyColumn = amountColumn;
        amountColumn++;
        worksheet.Column(currencyColumn).Width = worksheet.Column(amountColumn).Width;
        worksheet.Cell(headerRow.RowNumber(), currencyColumn).Value = "Moneda";
        worksheet.Cell(headerRow.RowNumber(), amountColumn).Value = "Monto";
        FillPeriod(worksheet, dateFrom, dateTo);
        var rowNumber = headerRow.RowNumber() + 1;

        foreach (var transaction in transactions)
        {
            worksheet.Cell(rowNumber, dateColumn).Value = transaction.OccurredOn.ToDateTime(TimeOnly.MinValue);
            worksheet.Cell(rowNumber, dateColumn).Style.DateFormat.Format = "dd/MM/yyyy";
            worksheet.Cell(rowNumber, descriptionColumn).Value = transaction.Description;
            worksheet.Cell(rowNumber, categoryColumn).Value = GetCategory(transaction);
            if (paymentMethodColumn is { } paymentColumn)
            {
                worksheet.Cell(rowNumber, paymentColumn).Value = GetPaymentMethodLabel(transaction.PaymentMethod);
            }

            worksheet.Cell(rowNumber, currencyColumn).Value = transaction.Amount.Currency.ToString();
            worksheet.Cell(rowNumber, amountColumn).Value = GetSignedAmount(transaction);
            worksheet.Cell(rowNumber, amountColumn).Style.NumberFormat.Format = "\"$\" #,##0.00";
            worksheet.Cell(rowNumber, statusColumn).Value = GetStatusLabel(transaction.Status);
            rowNumber++;
        }

        var lastUsedRow = Math.Max(9, worksheet.LastRowUsed()?.RowNumber() ?? 9);
        var bodyRange = worksheet.Range(9, 1, lastUsedRow, amountColumn);

        worksheet.ConditionalFormats.Remove(format => format.Ranges.Any(range =>
            range.RangeAddress.FirstAddress.RowNumber <= lastUsedRow
            && range.RangeAddress.LastAddress.RowNumber >= 9));

        var bodyBorder = worksheet.Cell(9, categoryColumn).Style.Border;
        bodyRange.Style.Fill.BackgroundColor = XLColor.White;
        bodyRange.Style.Fill.PatternType = XLFillPatternValues.Solid;
        bodyRange.Style.Border = bodyBorder;

    }

    private static void FillPeriod(IXLWorksheet worksheet, DateOnly? dateFrom, DateOnly? dateTo)
    {
        for (var row = 5; row <= 7; row++)
        {
            for (var column = 5; column <= 8; column++)
            {
                worksheet.Cell(row, column).Value = string.Empty;
            }
        }

        worksheet.Cell("F5").Value = "Fecha desde";
        worksheet.Cell("F6").Value = "Fecha hasta";
        worksheet.Range("F5:F6").Style.Font.Bold = true;

        SetPeriodValue(worksheet.Cell("G5"), dateFrom);
        SetPeriodValue(worksheet.Cell("G6"), dateTo);
    }

    private static void SetPeriodValue(IXLCell cell, DateOnly? date)
    {
        if (date is { } value)
        {
            cell.Value = value.ToDateTime(TimeOnly.MinValue);
            cell.Style.DateFormat.Format = "dd/MM/yyyy";
        }
        else
        {
            cell.Value = "Sin limite";
        }
    }

    private static int FindColumn(Dictionary<string, int> columns, params string[] headers)
    {
        var column = FindOptionalColumn(columns, headers);
        return column
            ?? throw new InvalidDataException(
                $"La plantilla no contiene ninguna de las columnas: {string.Join(", ", headers)}.");
    }

    private static int? FindOptionalColumn(Dictionary<string, int> columns, params string[] headers)
    {
        foreach (var header in headers)
        {
            if (columns.TryGetValue(Normalize(header), out var column))
            {
                return column;
            }
        }

        return null;
    }

    private static bool IsHeader(string value, string expected) => Normalize(value) == Normalize(expected);

    private static string Normalize(string value) => string.Concat(
            value.Normalize(NormalizationForm.FormD)
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark))
        .Trim()
        .ToLowerInvariant();

    private static string GetCategory(Transaction transaction) =>
        transaction.Type == Domain.Enums.TransactionType.Expense
            ? transaction.ExpenseCategory!.Value.ToString()
            : transaction.IncomeCategory!.Value.ToString();

    private static decimal GetSignedAmount(Transaction transaction) =>
        transaction.Type == Domain.Enums.TransactionType.Expense
            ? -Math.Abs(transaction.Amount.Amount)
            : Math.Abs(transaction.Amount.Amount);

    private static string GetPaymentMethodLabel(Domain.Enums.PaymentMethod paymentMethod) =>
        paymentMethod switch
        {
            Domain.Enums.PaymentMethod.Cash => "Efectivo",
            Domain.Enums.PaymentMethod.CreditCard => "Tarjeta de credito",
            Domain.Enums.PaymentMethod.DebitCard => "Tarjeta de debito",
            Domain.Enums.PaymentMethod.BankTransfer => "Transferencia bancaria",
            Domain.Enums.PaymentMethod.DigitalWallet => "Billetera virtual",
            _ => paymentMethod.ToString(),
        };

    private static string GetStatusLabel(Domain.Enums.TransactionStatus status) =>
        status switch
        {
            Domain.Enums.TransactionStatus.Pending => "Pendiente",
            Domain.Enums.TransactionStatus.Confirmed => "Confirmada",
            Domain.Enums.TransactionStatus.Eliminated => "Eliminada",
            _ => status.ToString(),
        };
}
