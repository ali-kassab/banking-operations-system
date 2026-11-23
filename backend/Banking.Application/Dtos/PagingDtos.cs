using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>
/// "?page=2&amp;pageSize=20" query parameters shared by every list endpoint.
/// Invalid values (page 0, pageSize 500) are rejected with 400 by the [ApiController] validation.
/// </summary>
public class PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>How many rows to skip in the database query.</summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>One page of results plus what the client needs to draw "Page 2 of 7".</summary>
public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
