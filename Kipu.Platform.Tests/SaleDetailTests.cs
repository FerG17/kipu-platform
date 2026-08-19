using Kipu.Platform.Sales.Domain.Model.Entities;

namespace Kipu.Platform.Tests;

/// <summary>
///     SaleDetail.Subtotal used to be a raw, unrounded product. Every real
///     caller today (SaleCommandService) always passes a 2-decimal UnitPrice
///     (sourced from Product.BasePrice, a decimal(10,2) column) and a
///     hardcoded 0 Discount, so the bug was never actually reachable through
///     the public API — but the entity itself makes no such guarantee, and
///     its own doc comment says Discount is kept specifically so a real
///     discount feature can use it later. These are unit tests, not
///     integration ones: nothing in the current API can construct a
///     SaleDetail with a fractional-cent raw product, so this exercises the
///     entity directly.
/// </summary>
public class SaleDetailTests
{
    [Fact]
    public void Subtotal_WithAFractionOfACent_RoundsAwayFromZero()
    {
        var detail = new SaleDetail(saleId: 1, productId: 1, quantity: 3, unitPrice: 3.333m, discount: 0m);

        // 3 * 3.333 = 9.999, which rounds up to 10.00 — not truncates to 9.99.
        Assert.Equal(10.00m, detail.Subtotal);
    }

    [Fact]
    public void Subtotal_WithADiscountThatIntroducesExtraDecimals_StillRoundsToTwoPlaces()
    {
        var detail = new SaleDetail(saleId: 1, productId: 1, quantity: 1, unitPrice: 10.00m, discount: 0.333m);

        // 10.00 * (1 - 0.333) = 6.67
        Assert.Equal(6.67m, detail.Subtotal);
    }

    [Fact]
    public void Subtotal_WithExactlyTwoDecimals_IsUnaffectedByRounding()
    {
        var detail = new SaleDetail(saleId: 1, productId: 1, quantity: 4, unitPrice: 12.50m, discount: 0m);

        Assert.Equal(50.00m, detail.Subtotal);
    }
}
