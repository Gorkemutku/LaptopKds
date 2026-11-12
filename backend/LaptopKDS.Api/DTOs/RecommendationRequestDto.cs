namespace LaptopKDS.Api.DTOs;

public record RecommendationRequestDto(
    decimal BudgetMax,
    int ProfileId
);
