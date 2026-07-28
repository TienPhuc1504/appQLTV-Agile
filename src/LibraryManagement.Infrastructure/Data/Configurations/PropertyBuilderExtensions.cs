using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LibraryManagement.Infrastructure.Data.Configurations;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<decimal> HasMoneyConversion(
        this PropertyBuilder<decimal> propertyBuilder)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        var converter = new ValueConverter<decimal, long>(
            value => decimal.ToInt64(value * 100m),
            value => value / 100m);

        return propertyBuilder
            .HasConversion(converter)
            .HasColumnType("INTEGER");
    }
}
