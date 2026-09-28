using CompliCore.DTOs.ComplianceItemDtos;

namespace CompliCore.DTOs;

public record DashboardResponse(
    int Total,
    int Valid,
    int Expiring,
    int Expired,
    List<ComplianceItemResponse> NextToExpire
);