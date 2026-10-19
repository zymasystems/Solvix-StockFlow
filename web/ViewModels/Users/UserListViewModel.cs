using Solvix.Web.Models;

namespace Solvix.Web.ViewModels.Users;

public class UserListViewModel
{
    public string? SearchTerm { get; set; }

    public string? Role { get; set; }

    public List<UserProfile> Users { get; set; } = new();
}
