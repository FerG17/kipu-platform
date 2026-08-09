namespace Bodega.Platform.Iam.Domain.Model.Commands;

public record UpdateBusinessCommand(int BusinessId, string Name, string Type, string Address, string Ruc);
