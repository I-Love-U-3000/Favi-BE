namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public sealed record PostLocationReadModel(
    string? Name,
    string? FullAddress,
    double? Latitude,
    double? Longitude);
