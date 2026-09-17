using System.Globalization;

namespace LosLms.Services;

/// <summary>One parsed, validated migrated-loan line ready to persist.</summary>
public sealed record MigratedLoanRow(
    string LoanAccountNo,
    string CustomerName,
    string? Pan,
    string? Mobile,
    string? Product,
    decimal? SanctionedAmount,
    decimal? OutstandingAmount,
    string? Status,
    DateOnly? DisbursedOn,
    string? Branch);

/// <summary>
/// Parses the bulk migrated-loan upload — the CSV the "Download template" button hands the client,
/// filled in and sent back. CSV, not a real .xlsx reader, so it needs no dependency and Excel opens
/// and saves it natively. Only two columns are required (account number and customer name); everything
/// else is optional, because back-book exports vary and a partial record still beats no record.
/// </summary>
/// <remarks>
/// Unlike the vehicle-cap parser this one is quote-aware (<see cref="SplitCsvLine"/>), because customer
/// names and Indian-formatted amounts routinely contain commas. A field wrapped in double quotes keeps
/// its commas; amounts are additionally stripped of ₹, spaces and stray commas before parsing.
/// </remarks>
public static class MigratedLoanCsv
{
    public const string Header =
        "LoanAccountNo,CustomerName,PAN,Mobile,Product,SanctionedAmount,OutstandingAmount,Status,DisbursedDate,Branch";

    private static readonly string[] DateFormats =
        { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yy", "d/M/yy" };

    /// <summary>
    /// Turns raw file text into validated rows plus one human-readable error per bad line. A file with
    /// some bad lines still returns its good rows — the caller imports those and reports the rest.
    /// </summary>
    public static (List<MigratedLoanRow> Rows, List<string> Errors) Parse(string content)
    {
        var rows = new List<MigratedLoanRow>();
        var errors = new List<string>();

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            if (raw.Trim().Length == 0)
            {
                continue;
            }

            // The header, whether or not the client left it in — matched on the first cell.
            if (lineNumber == 1 && raw.TrimStart().StartsWith("LoanAccountNo", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cells = SplitCsvLine(raw);

            var account = Cell(cells, 0);
            var customer = Cell(cells, 1);

            if (account.Length == 0 || customer.Length == 0)
            {
                errors.Add($"Line {lineNumber}: Loan account number and customer name are both required.");
                continue;
            }

            var sanctioned = ParseAmount(Cell(cells, 5));
            var outstanding = ParseAmount(Cell(cells, 6));
            var disbursed = ParseDate(Cell(cells, 8));

            if (Cell(cells, 8).Length > 0 && disbursed is null)
            {
                errors.Add($"Line {lineNumber}: Disbursed date \"{Cell(cells, 8)}\" isn't a recognisable date (use dd-mm-yyyy).");
                continue;
            }

            rows.Add(new MigratedLoanRow(
                LoanAccountNo: account,
                CustomerName: customer,
                Pan: NullIfEmpty(Cell(cells, 2).ToUpperInvariant()),
                Mobile: NullIfEmpty(Cell(cells, 3)),
                Product: NullIfEmpty(Cell(cells, 4)),
                SanctionedAmount: sanctioned,
                OutstandingAmount: outstanding,
                Status: NullIfEmpty(Cell(cells, 7)),
                DisbursedOn: disbursed,
                Branch: NullIfEmpty(Cell(cells, 9))));
        }

        return (rows, errors);
    }

    private static string Cell(IReadOnlyList<string> cells, int i) => i < cells.Count ? cells[i].Trim() : string.Empty;

    private static string? NullIfEmpty(string s) => s.Length == 0 ? null : s;

    private static decimal? ParseAmount(string cell)
    {
        var text = cell.Replace("₹", string.Empty).Replace(",", string.Empty).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount >= 0m
            ? amount
            : null;
    }

    private static DateOnly? ParseDate(string cell)
    {
        if (cell.Length == 0)
        {
            return null;
        }

        return DateTime.TryParseExact(cell, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? DateOnly.FromDateTime(dt)
            : null;
    }

    /// <summary>Splits one CSV line, honouring double-quoted fields (so a "Patil, Sons" name stays whole).</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    // A doubled "" inside a quoted field is a literal quote.
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else { inQuotes = false; }
                }
                else { current.Append(c); }
            }
            else if (c == '"') { inQuotes = true; }
            else if (c == ',') { cells.Add(current.ToString()); current.Clear(); }
            else { current.Append(c); }
        }

        cells.Add(current.ToString());
        return cells;
    }
}
