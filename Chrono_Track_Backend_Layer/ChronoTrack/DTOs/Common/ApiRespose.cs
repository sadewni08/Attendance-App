namespace ChronoTrack.DTOs.Common
{
    public record ApiResponse<T>(
        bool Success,
        string Message,
        T? Data = default
    );

    public record PagedResponse<T>
    {
        public List<T> Items { get; init; } = new();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages { get; init; }
        
        // Add calculated properties for pagination display
        public int StartItem => ((Page - 1) * PageSize) + 1;
        public int EndItem => Math.Min(Page * PageSize, TotalItems);
        
        public PagedResponse(List<T> items, int page, int pageSize, int totalItems, int totalPages)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = totalPages;
        }
    }

    public record ErrorResponse(
        bool Success,
        string Message,
        List<string>? Errors = null
    );
}