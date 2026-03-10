public class PaginationParameters
{
    private const int DefaultPage = 1;
    public int Page { get; set; } = DefaultPage;
    public int? PageSize { get; set; }
}
