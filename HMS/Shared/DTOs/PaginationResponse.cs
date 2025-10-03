namespace Shared.DTOs;

public sealed record PaginationResponse<T>(
    IEnumerable<T> Items,
    PaginationResponse Pagination
    );

public sealed record PaginationResponse(
    int Page, 
    int PageSize,
    int TotalItems,
    int TotalPages
    );
