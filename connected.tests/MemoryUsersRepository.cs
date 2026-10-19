using System.Collections.Concurrent;
using System.Text.Json;
using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.ViewModels.Users;

namespace Solvix.Connected.Tests;
public sealed class MemoryUsersRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, ProfileResponse> profiles = new();
    public MemoryUsersRepository()
    {
        foreach (var uid in new[] { "owner", "platform-admin", "staff", "other", "stranger", "new-user", "company-owner", "pending" })
        {
            var admin = uid is "owner" or "platform-admin";
            profiles[uid] = new() { Uid = uid, FirstName = uid == "owner" ? "Owner" : uid, LastName = admin ? "Admin" : "User", DisplayName = uid == "owner" ? "Owner Admin" : uid + " User", Email = uid + "@example.test", Role = admin ? "admin" : "staff", Status = uid == "pending" ? "Inactive" : "Active" };
        }
    }
    private static ProfileResponse Copy(ProfileResponse p) => JsonSerializer.Deserialize<ProfileResponse>(JsonSerializer.Serialize(p))!;
    public Task<ProfileResponse?> FindAsync(string uid) => Task.FromResult(profiles.TryGetValue(uid, out var value) ? Copy(value) : null);
    public Task<PagedResponse<ProfileResponse>> ListAsync(int size=50, string? cursor=null)
    {
        var skip = cursor is null ? 0 : int.Parse(cursor);
        var rows = profiles.Values.OrderBy(p => p.Uid).Skip(skip).Take(size + 1).ToList();
        return Task.FromResult(new PagedResponse<ProfileResponse> { Items = rows.Take(size).Select(Copy).ToList(), NextCursor = rows.Count > size ? (skip + size).ToString() : null });
    }
    public Task<ProfileResponse> CreateAsync(string uid, string email, CreateProfileRequest r)
    {
        var p = new ProfileResponse { Uid = uid, Email = email, FirstName = r.FirstName, LastName = r.LastName, DisplayName = r.FirstName + " " + r.LastName, Company = r.Company, Role = "staff", Status = "Inactive" };
        if (!profiles.TryAdd(uid, p)) throw new ServiceException(409, "Profile already exists.");
        return Task.FromResult(Copy(p));
    }
    public async Task<ProfileResponse> UpdateNamesAsync(string uid, string firstName, string lastName)
    {
        var p = await FindAsync(uid) ?? throw new ServiceException(404,"Profile not found.");
        p.FirstName = firstName; p.LastName = lastName; p.DisplayName = firstName + " " + lastName; profiles[uid] = Copy(p); return p;
    }
    public async Task<ProfileResponse> UpdateAdminAsync(string uid, UserEditViewModel r)
    {
        var p = await FindAsync(uid) ?? throw new ServiceException(404,"Profile not found.");
        p.FirstName = r.FirstName; p.LastName = r.LastName; p.DisplayName = r.FirstName + " " + r.LastName; p.Company = r.Company ?? ""; p.Role = r.Role; p.Status = r.Status;
        profiles[uid] = Copy(p); return p;
    }
}
public sealed class MemoryUsersDirectory(MemoryUsersRepository users) : IFirebaseUserDirectory
{
    public async Task VerifyExistsAsync(string uid)
    {
        if (await users.FindAsync(uid) is null) throw new ServiceException(404,"Firebase user not found.");
    }
}
