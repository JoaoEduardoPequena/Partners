namespace Partners.Application.Wrapper
{
    public class PagedResponse<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];

        // Total de registos depois dos filtros e antes da paginação.
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }
}
