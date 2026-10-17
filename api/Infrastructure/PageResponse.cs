using PrimeERP.Domain.Results;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>صفحة صفوفٍ للعميل: الصفوف وعددها الكلّي وموضعها</summary>
    public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
    {
        public static PageResponse<T> From<TSource>(PagedResult<TSource> page, Func<TSource, T> map) =>
            new(page.Items.Select(map).ToList(), page.TotalCount, page.Page, page.PageSize);
    }
}
