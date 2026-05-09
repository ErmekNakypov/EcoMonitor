namespace EcoMonitor.Web.Models.Admin.Users;

public class UserListViewModel
{
    public string? SearchQuery { get; set; }
    public string? RoleFilter { get; set; }
    public bool? ActiveFilter { get; set; }
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = Array.Empty<UserListItemViewModel>();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
}
