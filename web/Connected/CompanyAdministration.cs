using Solvix.Contracts;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;

public sealed partial class CompanyService
{
    public async Task<bool> IsPlatformAdministratorAsync(string uid)
    {
        var profile = await accounts.FindAsync(Id(uid));
        return profile is { Role: "admin", Status: "Active" };
    }

    // Selection and navigation establish a persisted membership only for verified system administrators; tenant checks still apply.
    public async Task EnsurePlatformAdministratorAccessAsync(string uid, string companyId)
    {
        if (!await IsPlatformAdministratorAsync(uid)) return;
        var account = (await accounts.FindAsync(uid))!;
        var root = Root(companyId);
        await store.RunAsync(async tx =>
        {
            await CompanyAvailable(tx, companyId);
            await UserAvailable(tx, uid);
            Require(await tx.GetAsync<Company>(root + "meta/company") is not null, "Company not found.", 404);
            var member = await tx.GetAsync<Member>(root + "members/" + Id(uid)) ?? new Member { Id = uid, JoinedAt = clock.GetUtcNow() };
            member.Role = "admin"; member.Status = "Active";
            member.FullName = AccountDisplayName.From(account); member.Email = account.Email;
            member.RegisteredAt ??= account.CreatedAt;
            tx.Put(root + "members/" + uid, member);
            tx.Put("userCompanies/" + uid + "/companies/" + companyId, new Entity { Id = companyId });
            return 0;
        });
    }

    public async Task<Membership?> NavigationFallbackAsync(string uid, string? preferredCompanyId)
    {
        if (!await IsPlatformAdministratorAsync(uid)) return null;
        var available = (await store.ListCompaniesAsync()).OrderByDescending(c => c.Id == preferredCompanyId).ThenBy(c => c.Name).ToList();
        foreach (var company in available)
        {
            if (await store.GetAsync<CompanyDeletionJob>("companyDeletions/" + company.Id) is not null) continue;
            try
            {
                await EnsurePlatformAdministratorAccessAsync(uid, company.Id);
                var member = await store.GetAsync<Member>(Root(company.Id) + "members/" + uid);
                if (member is { Role: "admin", Status: "Active" }) return new() { Company = company, Member = member };
            }
            catch (ServiceException error) when (error.StatusCode is 404 or 410) { } // A company may be deleted while the directory is loading.
        }
        return null;
    }

    public async Task<List<CompanyPanel>> ManagementPanelsAsync(string uid)
    {
        var platform = await IsPlatformAdministratorAsync(uid);
        var own = await CompaniesAsync(uid);
        var companies = platform ? await store.ListCompaniesAsync() : own.Select(m => m.Company).ToList();
        var result = new List<CompanyPanel>();
        foreach (var company in companies.OrderBy(c => c.Name))
        {
            if (await store.GetAsync<CompanyDeletionJob>("companyDeletions/" + company.Id) is not null) continue;
            var member = own.FirstOrDefault(m => m.Company.Id == company.Id)?.Member;
            bool manage = platform || member is { Role: "admin", Status: "Active" };
            var root = Root(company.Id);
            var sites = await store.ListAsync<Warehouse>(root + "warehouses");
            var members = manage ? await store.ListAsync<Member>(root + "members") : new();
            foreach (var person in members.Where(m => m.RegisteredAt is null))
                person.RegisteredAt = (await accounts.FindAsync(person.Id))?.CreatedAt;
            result.Add(new CompanyPanel
            {
                Company = company, CanManage = manage, CanDelete = platform, Status = platform ? "Active" : member?.Status ?? "Pending",
                Role = platform ? "admin" : member?.Role ?? "staff",
                Warehouses = sites.Where(w => w.Active && (manage || (member is { Status: "Active" } && member.WarehouseIds.Contains(w.Id)))).ToList(),
                Members = members
            });
        }
        return result;
    }

    public async Task ApproveAccountCompanyAsync(string actorUid, string targetUid, string companyRole)
    {
        Require(await IsPlatformAdministratorAsync(actorUid), "Account administrator permission is required.", 403);
        Require(companyRole is "admin" or "staff", "Choose a valid company role.");
        var account = await accounts.FindAsync(Id(targetUid)) ?? throw new ServiceException(404, "User profile not found.");
        Require(account.Status == "Active", "Activate the account before assigning company access.", 409);
        if (string.IsNullOrWhiteSpace(account.Company)) return;
        var company = await CreateAsync(actorUid, new() { Name = account.Company, RequestId = Guid.NewGuid() });
        await EnsurePlatformAdministratorAccessAsync(actorUid, company.Id);
        var root = Root(company.Id);
        var fallbackWarehouseId = Key(Guid.NewGuid().ToString())[..32];
        await store.RunAsync(async tx =>
        {
            await CompanyAvailable(tx, company.Id);
            await UserAvailable(tx, targetUid);
            var brand = (await tx.GetAsync<Company>(root + "meta/company"))!;
            var sites = (await tx.ListAsync<Warehouse>(root + "warehouses")).Where(w => w.Active).ToList();
            var warehouse = sites.FirstOrDefault(w => w.Id == brand.DefaultWarehouseId) ?? sites.FirstOrDefault();
            if (warehouse is null)
            {
                warehouse = new Warehouse { Id = fallbackWarehouseId, Name = brand.Name + " Main warehouse" };
                tx.Put(root + "warehouses/" + warehouse.Id, warehouse);
            }
            if (brand.DefaultWarehouseId != warehouse.Id)
            {
                brand.DefaultWarehouseId = warehouse.Id;
                tx.Put(root + "meta/company", brand);
            }
            var member = await tx.GetAsync<Member>(root + "members/" + targetUid) ?? new Member { Id = targetUid, JoinedAt = clock.GetUtcNow() };
            // Account approval and company role are separate from system-wide administrator status.
            // Never demote the administrator performing their own approval.
            if (targetUid != actorUid) member.Role = companyRole;
            member.Status = "Active"; member.FullName = AccountDisplayName.From(account); member.Email = account.Email;
            member.RegisteredAt ??= account.CreatedAt;
            member.WarehouseIds = member.WarehouseIds.Where(id => sites.Any(w => w.Id == id)).ToList();
            if (member.WarehouseIds.Count == 0) member.WarehouseIds.Add(warehouse.Id);
            tx.Put(root + "members/" + targetUid, member);
            tx.Put("userCompanies/" + targetUid + "/companies/" + company.Id, new Entity { Id = company.Id });
            return 0;
        });
    }
}

public sealed class CompanyPanel
{
    public Company Company { get; set; } = new();
    public bool CanDelete { get; set; }
    public bool CanManage { get; set; }
    public string Status { get; set; } = "Pending";
    public string Role { get; set; } = "staff";
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<Member> Members { get; set; } = new();
}
