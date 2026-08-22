using Kipu.Platform.Shared.Domain.Model.Queries;

namespace Kipu.Platform.Suppliers.Domain.Model.Queries;

/// <summary>
///     X4 M11: IncludeInactive defaults to false — a deactivated supplier
///     used to stay in every picker (new product, new purchase order) with
///     no way to tell it apart from an active one. The supplier management
///     page itself passes true, since it needs to show (and offer to
///     reactivate) inactive suppliers too.
/// </summary>
public record GetAllSuppliersByBusinessIdQuery(int BusinessId, PageRequest Page, bool IncludeInactive = false);
