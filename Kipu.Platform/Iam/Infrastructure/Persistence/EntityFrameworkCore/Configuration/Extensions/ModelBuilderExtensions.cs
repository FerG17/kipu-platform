using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Iam.Domain.Model.Entities;

namespace Kipu.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).ValueGeneratedOnAdd();
            entity.Property(user => user.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Name).IsRequired().HasMaxLength(100);
            entity.Property(user => user.LastName).HasMaxLength(100);
            entity.Property(user => user.Status).IsRequired().HasMaxLength(20);
            entity.Property(user => user.Phone).HasMaxLength(30);

            // User.BusinessId IS a real, enforced FK — every other bounded
            // context depends on it always pointing to a valid Business.
            entity.HasOne<Business>()
                .WithMany()
                .HasForeignKey(user => user.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(user => user.Version).IsConcurrencyToken();
        });

        builder.Entity<Business>(entity =>
        {
            entity.HasKey(business => business.Id);
            entity.Property(business => business.Id).ValueGeneratedOnAdd();
            entity.Property(business => business.Name).IsRequired().HasMaxLength(150);
            entity.Property(business => business.Type).IsRequired().HasMaxLength(20);
            entity.Property(business => business.Address).HasMaxLength(255);
            entity.Property(business => business.Ruc).HasMaxLength(20);

            // Business.UserId is deliberately NOT a database-enforced FK — see
            // Business.cs for why (circular reference with User.BusinessId).
        });

        builder.Entity<PasswordResetCode>(entity =>
        {
            entity.HasKey(code => code.Id);
            entity.Property(code => code.Id).ValueGeneratedOnAdd();
            entity.Property(code => code.CodeHash).IsRequired();
            entity.Property(code => code.Version).IsConcurrencyToken();

            // No enforced FK to User on purpose — same reasoning as everywhere
            // else a lookup happens before a tenant/auth context exists
            // (compare Business.UserId): losing a code row if its user is ever
            // hard-deleted is harmless, the row just becomes unreachable.
            entity.HasIndex(code => code.UserId);
        });

        builder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).ValueGeneratedOnAdd();
            entity.Property(role => role.Position).IsRequired().HasMaxLength(30);

            // Seed the fixed roles the frontend already assumes exist.
            entity.HasData(
                new { Id = 1, Position = "ADMIN" },
                new { Id = 2, Position = "CASHIER" },
                new { Id = 3, Position = "WAREHOUSE" }
            );
        });
    }
}
