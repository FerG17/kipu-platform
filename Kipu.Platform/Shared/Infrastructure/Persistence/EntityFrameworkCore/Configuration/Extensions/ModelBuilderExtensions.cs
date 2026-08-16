using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the database context.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Renames every table to snake_case plural and every column/key/index to
    ///     snake_case, matching the naming convention used across the platform's
    ///     MySQL schema.
    /// </summary>
    public static void UseSnakeCaseNamingConvention(this ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName)) entity.SetTableName(tableName.Pluralize().Underscore());

            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.GetColumnName().Underscore());

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrEmpty(keyName)) key.SetName(keyName.Underscore());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var foreignKeyName = foreignKey.GetConstraintName();
                if (!string.IsNullOrEmpty(foreignKeyName)) foreignKey.SetConstraintName(foreignKeyName.Underscore());
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexDatabaseName = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(indexDatabaseName)) index.SetDatabaseName(indexDatabaseName.Underscore());
            }
        }
    }
}
