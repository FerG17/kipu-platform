using Bodega.Platform.Products.Application.CommandServices;
using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Commands;
using Bodega.Platform.Products.Domain.Model.Entities;
using Bodega.Platform.Products.Domain.Model.Queries;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Products.Interfaces.Acl;

namespace Bodega.Platform.Products.Application.Acl;

public class ProductContextFacade(
    IWarehouseCommandService warehouseCommandService,
    IWarehouseQueryService warehouseQueryService,
    IInventoryCommandService inventoryCommandService,
    IInventoryQueryService inventoryQueryService,
    IProductQueryService productQueryService,
    IBatchRepository batchRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IProductContextFacade
{
    public async Task<int> CreateDefaultWarehouse(int businessId, CancellationToken cancellationToken)
    {
        var command = new CreateWarehouseCommand(businessId, "Almacén Principal", "ALM-001", string.Empty,
            WarehouseCapacity.Medium);
        var result = await warehouseCommandService.Handle(command, cancellationToken);
        return result.IsSuccess ? result.Value!.Id : 0;
    }

    public async Task<bool> DecrementStock(int productId, int businessId, int quantity, CancellationToken cancellationToken)
    {
        var result = await inventoryCommandService.Handle(new RegisterStockSaleCommand(productId, businessId, quantity),
            cancellationToken);
        return result.IsSuccess;
    }

    public async Task<int> GetAvailableStock(int productId, CancellationToken cancellationToken)
    {
        var items = await inventoryQueryService.Handle(new GetInventoryByProductIdQuery(productId), cancellationToken);
        return items.Sum(item => item.StockUnit);
    }

    public async Task RegisterStockIntake(int productId, int businessId, int quantity, decimal? purchasePrice,
        string? supplier, string? note, CancellationToken cancellationToken)
    {
        var warehouses = await warehouseQueryService.Handle(new GetAllWarehousesByBusinessIdQuery(businessId), cancellationToken);
        var warehouse = warehouses.FirstOrDefault();
        if (warehouse == null) return;

        var command = new RegisterStockIntakeCommand(productId, businessId, warehouse.Id, quantity, purchasePrice, null,
            supplier, note, null);
        await inventoryCommandService.Handle(command, cancellationToken);
    }

    public async Task<bool> ProductExists(int productId, CancellationToken cancellationToken)
    {
        var product = await productQueryService.Handle(new GetProductByIdQuery(productId), cancellationToken);
        return product != null;
    }

    public async Task<IReadOnlyCollection<ActiveBatchInfo>> GetAllActiveBatchesForExpirationSweep(CancellationToken cancellationToken)
    {
        var batches = await batchRepository.FindAllActiveAsync(cancellationToken);
        var products = await productRepository.ListIgnoringTenantAsync(cancellationToken);
        var productNamesById = products.ToDictionary(product => product.Id, product => product.Name);

        return batches
            .Select(batch => new ActiveBatchInfo(batch.Id, batch.ProductId,
                productNamesById.GetValueOrDefault(batch.ProductId, string.Empty), batch.BusinessId, batch.Expiration))
            .ToList();
    }

    public async Task<ProductKpisSnapshot> GetProductKpisSnapshot(int businessId, CancellationToken cancellationToken)
    {
        var products = (await productQueryService.Handle(new GetAllProductsByBusinessIdQuery(businessId, null), cancellationToken))
            .Where(product => product.IsActive).ToList();
        var priceByProductId = products.ToDictionary(product => product.Id, product => product.BasePrice);

        var inventoryItems = await inventoryQueryService.Handle(new GetInventoryByBusinessIdQuery(businessId), cancellationToken);
        var inventoryItemsList = inventoryItems.ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeBatches = await batchRepository.FindAllByBusinessIdAsync(businessId, cancellationToken);
        var expiringSoonCount = activeBatches.Count(batch => batch.Status == BatchStatus.Active && batch.IsExpiringSoon(today));

        var inventoryValue = inventoryItemsList.Sum(item =>
            priceByProductId.GetValueOrDefault(item.ProductId, 0m) * item.StockUnit);

        return new ProductKpisSnapshot(
            products.Count,
            inventoryItemsList.Count(item => item.IsLowStock),
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
            .Select(group => new TopStockProductInfo(group.Key, nameByProductId.GetValueOrDefault(group.Key, string.Empty),
                group.Sum(item => item.StockUnit)))
            .OrderByDescending(info => info.TotalStock)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyCollection<StockMovementReportLine>> GetStockMovementsForReport(int businessId,
        DateOnly? dateFrom, DateOnly? dateTo, int? productId, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.FindFilteredByBusinessIdAsync(businessId, dateFrom, dateTo, productId,
            cancellationToken);

        var products = await productQueryService.Handle(new GetAllProductsByBusinessIdQuery(businessId, null), cancellationToken);
        var nameByProductId = products.ToDictionary(product => product.Id, product => product.Name);

        return movements
            .Select(movement => new StockMovementReportLine(movement.ProductId,
                nameByProductId.GetValueOrDefault(movement.ProductId, string.Empty), movement.Type, movement.Quantity,
                movement.Supplier, movement.Note, movement.RegisteredAt))
            .ToList();
    }
}
