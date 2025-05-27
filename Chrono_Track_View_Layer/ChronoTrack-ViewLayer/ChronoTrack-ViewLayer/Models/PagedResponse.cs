using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class PagedResponse<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();

        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious => CurrentPage > 1;

        [JsonPropertyName("hasNext")]
        public bool HasNext => CurrentPage < TotalPages;

        public PagedResponse()
        {
        }

        public PagedResponse(List<T> items, int currentPage, int pageSize, int totalItems, int totalPages)
        {
            Items = items;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = totalPages;
        }
    }
} 