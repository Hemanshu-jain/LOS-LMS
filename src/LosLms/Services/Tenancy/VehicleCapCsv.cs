using System.Globalization;

namespace LosLms.Services;

/// <summary>One parsed catalog line, already validated. Make/Model are trimmed, Year and amount typed.</summary>
public sealed record VehicleCapRow(string Make, string Model, int Year, decimal MaxLoanAmount);

/// <summary>
/// Parses the bulk vehicle-cap upload — the CSV the "Download template" link hands the client, filled
/// in and sent back. CSV rather than a real .xlsx reader because it needs no dependency: the template
/// is a CSV, Excel opens and saves it natively, and controlling the format is exactly what stops the
/// "confused columns / missed data" the client worried about.
/// </summary>
/// <remarks>
/// ponytail: naive comma split — a Make or Model containing a literal comma would mis-parse. Vehicle
/// names do not, and the template shows the shape. Swap in a quoted-CSV reader only if a real file
/// ever needs embedded commas.
/// </remarks>
public static class VehicleCapCsv
{
    public const string Header = "Make,Model,Year,MaxLoanAmount";

    private const int MinYear = 1990;
    private const int MaxYear = 2100;

    /// <summary>
    /// Turns raw file text into validated rows plus one human-readable error per bad line. A file
    /// with any error still returns its good rows — the caller imports those and reports the rest, so
    /// one fat-fingered line does not sink the whole upload.
    /// </summary>
    public static (List<VehicleCapRow> Rows, List<string> Errors) Parse(string content)
    {
        var rows = new List<VehicleCapRow>();
        var errors = new List<string>();

        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var lineNumber = 0;

        foreach (var raw in lines)
        {
            lineNumber++;
            var line = raw.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            // The header, whether or not the client left it in. Matched on the first cell so a stray
            // "Make" heading never becomes a row.
            if (lineNumber == 1 && line.StartsWith("make,", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cells = line.Split(',');
            if (cells.Length < 4)
            {
                errors.Add($"Line {lineNumber}: expected 4 columns ({Header}), found {cells.Length}.");
                continue;
            }

            var make = cells[0].Trim();
            var model = cells[1].Trim();
            // Anything past the fourth cell is a thousands-separated amount (e.g. 12,00,000) that the
            // naive split broke apart — rejoin the tail so Indian-formatted figures survive.
            var amountCell = string.Concat(cells.Skip(3));

            if (make.Length == 0 || model.Length == 0)
            {
                errors.Add($"Line {lineNumber}: Make and Model are both required.");
                continue;
            }

            if (!int.TryParse(cells[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                || year is < MinYear or > MaxYear)
            {
                errors.Add($"Line {lineNumber}: Year \"{cells[2].Trim()}\" is not a whole year between {MinYear} and {MaxYear}.");
                continue;
            }

            var amountText = amountCell.Trim().Replace("₹", string.Empty).Replace(",", string.Empty);
            if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                || amount <= 0m)
            {
                errors.Add($"Line {lineNumber}: Max loan amount \"{cells[3].Trim()}\" is not a positive number.");
                continue;
            }

            rows.Add(new VehicleCapRow(make, model, year, amount));
        }

        return (rows, errors);
    }
}
