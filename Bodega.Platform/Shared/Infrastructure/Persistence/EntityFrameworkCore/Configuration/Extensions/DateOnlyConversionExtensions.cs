using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     MySql.EntityFrameworkCore has no native DateOnly support — it reads a
///     "date" column back as DateTime and throws an InvalidCastException
///     without an explicit conversion. Every DateOnly/DateOnly? column in
///     every bounded context needs this.
/// </summary>
public static class DateOnlyConversionExtensions
{
    public static PropertyBuilder<DateOnly> HasDateOnlyConversion(this PropertyBuilder<DateOnly> builder)
    {
        return builder.HasConversion(date => date.ToDateTime(TimeOnly.MinValue), date => DateOnly.FromDateTime(date))
            .HasColumnType("date");
    }

    public static PropertyBuilder<DateOnly?> HasDateOnlyConversion(this PropertyBuilder<DateOnly?> builder)
    {
        return builder.HasConversion(
                date => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                date => date.HasValue ? DateOnly.FromDateTime(date.Value) : (DateOnly?)null)
            .HasColumnType("date");
    }
}
