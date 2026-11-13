using System.Globalization;

namespace BudgetPipeline;

public static class TransactionParser
{
    private const string ExpectedHeader = "Id,Category,Amount,Status";

    public static IReadOnlyList<Transaction> ParseLines(IEnumerable<string>? lines)
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines), "Input data is missing.");

        var source = lines.ToArray();

        if (source.Length == 0)
            throw new ArgumentException("Input file has no header.", nameof(lines));

        if (source[0].Trim() != ExpectedHeader)
            throw new FormatException($"The first line must be: {ExpectedHeader}");

        var result = new List<Transaction>();

        for (var i = 1; i < source.Length; i++)
        {
            var line = source[i].Trim();
            if (line.Length == 0)
                continue;

            var lineNumber = i + 1;
            var parts = line.Split(',');
            if (parts.Length != 4)
                throw new FormatException($"Line {lineNumber}: expected 4 comma-separated fields.");

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                throw new FormatException($"Line {lineNumber}: Id must be an integer.");

            var category = parts[1].Trim();
            if (category.Length == 0)
                throw new FormatException($"Line {lineNumber}: Category is required.");

            if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                throw new FormatException($"Line {lineNumber}: Amount must be a number.");

            if (amount < 0)
                throw new FormatException($"Line {lineNumber}: Amount cannot be negative.");

            if (!TryParseStatus(parts[3], out var status))
                throw new FormatException($"Line {lineNumber}: Status must be Approved, Pending, or Rejected.");

            result.Add(new Transaction(id, category, amount, status));
        }

        return result.AsReadOnly();
    }

    private static bool TryParseStatus(string text, out TransactionStatus status)
    {
        switch (text.Trim().ToLowerInvariant())
        {
            case "approved":
                status = TransactionStatus.Approved;
                return true;
            case "pending":
                status = TransactionStatus.Pending;
                return true;
            case "rejected":
                status = TransactionStatus.Rejected;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
