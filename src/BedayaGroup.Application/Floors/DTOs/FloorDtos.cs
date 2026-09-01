namespace BedayaGroup.Application.Floors.DTOs;

public record CreateFloorRequest(
    int ProjectId,
    int FloorNumber,
    string Name,
    string? Description
);

public record UpdateFloorRequest(
    int FloorNumber,
    string Name,
    string? Description
);

public record FloorDto(
    int Id,
    int ProjectId,
    int FloorNumber,
    string Name,
    string? Description,
    DateTime CreatedAt
);
