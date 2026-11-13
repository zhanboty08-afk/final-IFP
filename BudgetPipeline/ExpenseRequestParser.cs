using System.Globalization;

namespace BudgetPipeline;

public static class ExpenseRequestParser
{
    private const string ExpectedHeader = "Id,Category,Amount";

    public static IReadOnlyList<ExpenseRequest> ParseLines(IEnumerable<string>? lines)
    {
        if (lines is null)
            throw new ArgumentNullException(nameof(lines), "Input data is missing.");

        var source = lines.ToArray();

        if (source.Length == 0)
            throw new ArgumentException("Input file has no header.", nameof(lines));

        if (source[0].Trim() != ExpectedHeader)
            throw new FormatException($"The first line must be: {ExpectedHeader}");

        var result = new List<ExpenseRequest>();

        for (var i = 1; i < source.Length; i++)
        {
            var line = source[i].Trim();
            if (line.Length == 0)
                continue;

            var lineNumber = i + 1;
            var parts = line.Split(',');
            if (parts.Length != 3)
                throw new FormatException($"Line {lineNumber}: expected 3 comma-separated fields.");

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                throw new FormatException($"Line {lineNumber}: Id must be an integer.");

            if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                throw new FormatException($"Line {lineNumber}: Amount must be a number.");

            result.Add(new ExpenseRequest(id, parts[1].Trim(), amount));
        }

        return result.AsReadOnly();
    }
}
