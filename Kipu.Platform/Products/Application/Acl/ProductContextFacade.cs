using Kipu.Platform.Shared.Application;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Commands;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Products.Domain.Model.Queries;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Shared.Domain.Model.Services;

namespace Kipu.Platform.Products.Application.Acl;

public class ProductContextFacade(
    IWarehouseCommandService warehouseCommandService,
    IWarehouseQueryService warehouseQueryService,
    IInventoryCommandService inventoryCommandService,
    IInventoryQueryService inventoryQueryService,
    IProductQueryService productQueryService,
    IBatchRepository batchRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository,
    IBusinessClock businessClock)
    : IProductContextFacade
{
    public async Task<int> CreateDefaultWarehouse(int businessId, CancellationToken cancellationToken)
    {
        var command = new CreateWarehouseCommand(businessId, "Almacén Principal", "ALM-001", string.Empty,
            WarehouseCapacity.Medium);
        var result = await warehouseCommandService.Handle(command, cancellationToken);
        return result.IsSuccess ? result.Value!.Id : 0;
    }

    public async Task<bool> DecrementStock(int productId, int businessId, decimal quantity, CancellationToken cancellationToken)
    {
        var result = await inventoryCommandService.Handle(new RegisterStockSaleCommand(productId, businessId, quantity),
            cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> RestoreStock(int productId, int businessId, decimal quantity, CancellationToken cancellationToken)
    {
        var result = await inventoryCommandService.Handle(new RegisterStockReturnCommand(productId, businessId, quantity),
            cancellationToken);
        return result.IsSuccess;
    }

    public async Task<decimal> GetAvailableStock(int productId, CancellationToken cancellationToken)
    {
        var items = await inventoryQueryService.Handle(new GetInventoryByProductIdQuery(productId), cancellationToken);
        return items.Sum(item => item.StockUnit);
    }

    /// <summary>
    ///     Receives into whichever warehouse the product already has stock in
    ///     (its lowest WarehouseId, matching the order RegisterStockSaleCommand
    ///     draws from), so a purchase order tops up the existing InventoryItem
    ///     instead of creating a second one in "the first warehouse" — which
    ///     used to fragment a single product's stock into two separate rows
    ///     the moment its existing stock happened to live anywhere but the
    ///     business's very first warehouse, each with its own independent
    ///     LOW_STOCK/OUT_OF_STOCK alert (the old one never clearing, a new one
    ///     firing for the "empty" original warehouse). Only a product with no
    ///     InventoryItem anywhere yet (never stocked before) falls back to the
    ///     business's first warehouse.
    /// </summary>
    public async Task<bool> RegisterStockIntake(int productId, int businessId, decimal quantity, decimal? purchasePrice,
        string? supplier, string? note, int? supplierId, CancellationToken cancellationToken)
    {
        // X4 M10: nothing checked warehouse status before — a purchase order
        // could silently receive into a deactivated warehouse, including one
        // the product already happened to have (now-stale) stock in.
        var warehouses = (await warehouseQueryService.Handle(new GetAllWarehousesByBusinessIdQuery(businessId),
            cancellationToken)).ToList();
        var activeWarehouseIds = warehouses.Where(warehouse => warehouse.Status == WarehouseStatus.Active)
            .Select(warehouse => warehouse.Id).ToHashSet();

        var existingItems = await inventoryQueryService.Handle(new GetInventoryByProductIdQuery(productId), cancellationToken);
        var targetWarehouseId = existingItems.Where(item => activeWarehouseIds.Contains(item.WarehouseId))
            .OrderBy(item => item.WarehouseId).FirstOrDefault()?.WarehouseId;

        targetWarehouseId ??= warehouses.Where(warehouse => warehouse.Status == WarehouseStatus.Active)
            .OrderBy(warehouse => warehouse.Id).FirstOrDefault()?.Id;

        // A business with nowhere active to receive into is a real failure,
        // not a no-op to pass over in silence — the caller has to know the
        // goods never landed.
        if (targetWarehouseId == null) return false;

        var command = new RegisterStockIntakeCommand(productId, businessId, targetWarehouseId.Value, quantity, purchasePrice,
            null, supplier, note, null, supplierId);
        var result = await inventoryCommandService.Handle(command, cancellationToken);
        return result.IsSuccess;
    }

    public async Task<bool> ProductExists(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product != null;
    }

    public async Task<decimal?> GetBasePrice(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product?.BasePrice;
    }

    public async Task<bool> IsProductActive(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product?.IsActive ?? false;
    }

    public async Task<bool> IsSoldByWeight(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product?.IsSoldByWeight ?? false;
    }

    /// <summary>
    ///     Deactivated products are excluded here: their batches would
    ///     otherwise keep re-triggering EXPIRATION/EXPIRED alerts on every
    ///     sweep forever, even though ProductDeactivatedEventHandler already
    ///     resolved those alerts the moment the product was deactivated — the
    ///     next sweep would just raise them right back.
    /// </summary>
    public async Task<IReadOnlyCollection<ActiveBatchInfo>> GetAllActiveBatchesForExpirationSweep(CancellationToken cancellationToken)
    {
        var batches = await batchRepository.FindAllActiveAsync(cancellationToken);
        var activeProductsById = (await productRepository.ListIgnoringTenantAsync(cancellationToken))
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);

        return batches
            .Where(batch => activeProductsById.ContainsKey(batch.ProductId))
            .Select(batch => new ActiveBatchInfo(batch.Id, batch.ProductId,
                activeProductsById[batch.ProductId].Name, batch.BusinessId, batch.Expiration))
            .ToList();
    }

    public async Task<ProductKpisSnapshot> GetProductKpisSnapshot(int businessId, CancellationToken cancellationToken)
    {
        var products = (await productQueryService.Handle(new GetAllProductsByBusinessIdQuery(businessId, null), cancellationToken))
            .Where(product => product.IsActive).ToList();
        var priceByProductId = products.ToDictionary(product => product.Id, product => product.BasePrice);

        var inventoryItems = await inventoryQueryService.Handle(new GetInventoryByBusinessIdQuery(businessId), cancellationToken);
        // Only inventory belonging to a still-active product counts toward
        // these KPIs — TotalProducts (below) already excludes inactive
        // products, and stock the owner can no longer sell shouldn't inflate
        // "inventory value" or count against "catalog health".
        var activeInventoryItems = inventoryItems.Where(item => priceByProductId.ContainsKey(item.ProductId)).ToList();

        var today = businessClock.Today;
        var activeBatches = await batchRepository.FindAllByBusinessIdAsync(businessId, cancellationToken);
        var expiringSoonCount = activeBatches.Count(batch => batch.Status == BatchStatus.Active && batch.IsExpiringSoon(today));

        var inventoryValue = activeInventoryItems.Sum(item =>
            priceByProductId.GetValueOrDefault(item.ProductId, 0m) * item.StockUnit);

        // Counted per PRODUCT (same unit as TotalProducts below), not per
        // InventoryItem — a product split across 2+ warehouses has one row
        // per warehouse, and counting rows instead of products would inflate
        // this number and mix units with TotalProducts in the health
        // percentage the caller derives from these two.
        var lowStockProductCount = activeInventoryItems
            .GroupBy(item => item.ProductId)
            .Count(group => StockRules.IsLowStock(group.Sum(item => item.StockUnit), group.Max(item => item.MinimumStock)));

        return new ProductKpisSnapshot(
            products.Count,
            lowStockProductCount,
            expiringSoonCount,
            inventoryValue);
    }

    public async Task<IReadOnlyCollection<TopStockProductInfo>> GetTopStockProducts(int businessId, int count,
        CancellationToken cancellationToken)
    {
        var products = await productQueryService.Handle(new GetAllProductsByBusinessIdQuery(businessId, null), cancellationToken);
        var nameByProductId = products.ToDictionary(product => product.Id, product => product.Name);

        var inventoryItems = await inventoryQueryService.Handle(new GetInventoryByBusinessIdQuery(businessId), cancellationToken);

        return inventoryItems
            .GroupBy(item => item.ProductId)
            .Select(group => new TopStockProductInfo(group.Key,
                nameByProductId.GetValueOrDefault(group.Key, $"#{group.Key}"),
                group.Sum(item => item.StockUnit)))
            .OrderByDescending(info => info.TotalStock)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyCollection<StockMovementReportLine>> GetStockMovementsForReport(int businessId,
        DateOnly? dateFrom, DateOnly? dateTo, int? productId, int? supplierId, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.FindFilteredByBusinessIdAsync(businessId, dateFrom, dateTo, productId,
            supplierId, cancellationToken);

        var products = await productQueryService.Handle(new GetAllProductsByBusinessIdQuery(businessId, null), cancellationToken);
        var nameByProductId = products.ToDictionary(product => product.Id, product => product.Name);

        return movements
            .Select(movement => new StockMovementReportLine(movement.ProductId,
                nameByProductId.GetValueOrDefault(movement.ProductId, string.Empty), movement.Type, movement.Quantity,
                movement.Supplier, movement.Note, movement.RegisteredAt))
            .ToList();
    }
}
