using Microsoft.EntityFrameworkCore;

namespace PickPlace.Api.Helpers
{
    /// <summary>
    /// Wrapper response standar untuk semua endpoint list yang menggunakan pagination.
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = [];
        public PaginationMeta Pagination { get; set; } = new();
    }

    public class PaginationMeta
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public static class PaginationExtensions
    {
        /// <summary>
        /// Ekstensi untuk IQueryable — jalankan COUNT lalu ambil slice sesuai page & pageSize.
        /// Maksimal pageSize dibatasi 100 agar tidak overload.
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            // Sanitasi input
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var totalItems = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Data = data,
                Pagination = new PaginationMeta
                {
                    CurrentPage     = page,
                    PageSize        = pageSize,
                    TotalItems      = totalItems,
                    TotalPages      = (int)Math.Ceiling(totalItems / (double)pageSize),
                    HasNextPage     = page * pageSize < totalItems,
                    HasPreviousPage = page > 1
                }
            };
        }
    }
}
