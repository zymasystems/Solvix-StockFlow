using Solvix.Contracts;
using Solvix.Web.Repositories.Interfaces;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;

namespace Solvix.Web.Connected;

public sealed class UserDeletionBarrier
{
    public string CompanyId { get; set; } = "";
}

public sealed class CompanyDeletionJob
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string ActorUid { get; set; } = "";
    public bool DeleteExclusiveAccounts { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<string> MemberIds { get; set; } = new();
    public List<string> ProcessedMemberIds { get; set; } = new();
    public List<string> DeletedAccountIds { get; set; } = new();
}

public sealed class CompanyDeletionEmployee
{
    public string Uid { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool DeletedAccount { get; set; }
    public bool SharedAccount { get; set; }
    public bool ProtectedAccount { get; set; }
}

public sealed class CompanyDeletionViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string ConfirmationName { get; set; } = "";
    public bool DeleteExclusiveAccounts { get; set; }
    public bool Started { get; set; }
    public bool Completed { get; set; }
    public List<CompanyDeletionEmployee> Employees { get; set; } = new();
}

// Only system account administrators may delete tenants. No deletion is performed by GET.
public sealed class CompanyDeletionService(ICompanyStore store, IUserRepository users, IFirebaseUserDirectory identities, TimeProvider clock)
{
    private async Task Authorize(string uid)
    {
        if (await users.FindAsync(CompanyService.Id(uid)) is not { Role: "admin", Status: "Active" })
            throw new ServiceException(403, "System administrator permission is required to delete a company.");
    }

    public async Task<List<CompanyDeletionJob>> PendingAsync(string uid)
    {
        await Authorize(uid);
        return (await store.ListAsync<CompanyDeletionJob>("companyDeletions")).Where(j => j.CompletedAt is null).ToList();
    }

    public async Task<CompanyDeletionViewModel> PreviewAsync(string uid, string companyId)
    {
        await Authorize(uid);
        CompanyService.Id(companyId);
        var job = await store.GetAsync<CompanyDeletionJob>("companyDeletions/" + companyId);
        var company = await store.GetAsync<Company>("companies/" + companyId + "/meta/company");
        if (company is null && job is null) throw new ServiceException(404, "Company not found.");
        var members = await store.ListAsync<Member>("companies/" + companyId + "/members");
        var result = new CompanyDeletionViewModel
        {
            Id = companyId, Name = job?.Name ?? company!.Name, Code = job?.Code ?? company!.Code,
            Started = job is not null, Completed = job?.CompletedAt is not null,
            DeleteExclusiveAccounts = job?.DeleteExclusiveAccounts ?? false
        };
        foreach (var id in job?.MemberIds ?? members.Select(m => m.Id).ToList())
        {
            var profile = await users.FindAsync(id);
            var member = members.FirstOrDefault(m => m.Id == id);
            result.Employees.Add(new()
            {
                DeletedAccount = job?.DeletedAccountIds.Contains(id) ?? false,
                Uid = id, Name = profile?.DisplayName ?? member?.FullName ?? id, Email = profile?.Email ?? member?.Email ?? "",
                SharedAccount = (await store.ListAsync<Entity>("userCompanies/" + id + "/companies")).Any(c => c.Id != companyId),
                ProtectedAccount = id == uid || id == job?.ActorUid || profile is null || profile.Role != "staff"
            });
        }
        return result;
    }

    public async Task DeleteAsync(string uid, string companyId, string confirmationName, bool deleteExclusiveAccounts)
    {
        await Authorize(uid);
        try { await DeleteCore(uid, companyId, confirmationName, deleteExclusiveAccounts); }
        catch (Grpc.Core.RpcException) { throw new ServiceException(503, "Company deletion could not finish. Access remains blocked if deletion started; resume deletion to retry cleanup."); }
    }

    private async Task DeleteCore(string uid, string companyId, string confirmationName, bool deleteExclusiveAccounts)
    {
        CompanyService.Id(companyId);
        var root = "companies/" + companyId + "/";
        var path = "companyDeletions/" + companyId;
        var job = await store.RunAsync(async tx =>
        {
            var current = await tx.GetAsync<CompanyDeletionJob>(path);
            var company = await tx.GetAsync<Company>(root + "meta/company");
            if (current is null && company is null) throw new ServiceException(404, "Company not found.");
            if (!string.Equals(confirmationName?.Trim(), current?.Name ?? company!.Name, StringComparison.Ordinal))
                throw new ServiceException(400, "Type the company name exactly to confirm deletion.");
            if (current is not null)
            {
                if (current.CompletedAt is null && current.DeleteExclusiveAccounts != deleteExclusiveAccounts)
                    throw new ServiceException(409, "Deletion has already started. Resume using the recorded account-deletion option.");
                return current;
            }
            current = new CompanyDeletionJob
            {
                Id = companyId, Name = company!.Name, Code = company.Code, ActorUid = uid,
                DeleteExclusiveAccounts = deleteExclusiveAccounts, StartedAt = clock.GetUtcNow(),
                MemberIds = (await tx.ListAsync<Member>(root + "members")).Select(m => m.Id).Distinct().ToList()
            };
            tx.Put(path, current); // All future company reads/writes and new memberships are blocked first.
            return current;
        });
        if (job.CompletedAt is not null) return;

        foreach (var memberId in job.MemberIds.Except(job.ProcessedMemberIds))
        {
            var profile = await users.FindAsync(memberId);
            var links = await store.ListAsync<Entity>("userCompanies/" + memberId + "/companies");
            bool deleted = false;
            if (job.DeleteExclusiveAccounts && memberId != uid && memberId != job.ActorUid && profile?.Role == "staff" && links.All(c => c.Id == companyId)
                && await users.PrepareCompanyDeletionAsync(memberId, companyId))
            {
                // Atomic preparation rechecks the real profile role and other-company links, and blocks new joins.
                await identities.DeleteAsync(memberId);
                await users.DeleteAsync(memberId);
                deleted = true;
            }
            else if (profile is null && (await store.GetAsync<UserDeletionBarrier>("userDeletions/" + memberId))?.CompanyId == companyId)
                deleted = true; // Resume after successful account deletion but before recording progress.

            if (!deleted && profile is not null && CompanyNames.Normalize(profile.Company) == CompanyNames.Normalize(job.Name))
            {
                string nextCompany = "";
                foreach (var link in links.Where(c => c.Id != companyId))
                {
                    if (await store.GetAsync<CompanyDeletionJob>("companyDeletions/" + link.Id) is not null) continue;
                    var other = await store.GetAsync<Company>("companies/" + link.Id + "/meta/company");
                    if (other is not null) { nextCompany = other.Name; break; }
                }
                await users.UpdateCompanyAsync(memberId, nextCompany);
            }
            await store.RunAsync(async tx =>
            {
                var progress = (await tx.GetAsync<CompanyDeletionJob>(path))!;
                tx.Delete("userCompanies/" + memberId + "/companies/" + companyId);
                if (!progress.ProcessedMemberIds.Contains(memberId)) progress.ProcessedMemberIds.Add(memberId);
                if (deleted && !progress.DeletedAccountIds.Contains(memberId)) progress.DeletedAccountIds.Add(memberId);
                tx.Put(path, progress);
                return 0;
            });
        }
        // Firestore does not remove subcollections when a parent is deleted. Remove the entire subtree explicitly.
        await store.PurgeCompanyAsync(companyId);
        await store.RunAsync(async tx =>
        {
            var progress = (await tx.GetAsync<CompanyDeletionJob>(path))!;
            var claimPath = CompanyNames.ClaimPath(progress.Name);
            var claim = await tx.GetAsync<CompanyNameClaim>(claimPath);
            if (claim?.CompanyId == companyId) tx.Delete(claimPath);
            progress.CompletedAt ??= clock.GetUtcNow();
            tx.Put(path, progress); // Retain the deletion receipt to reject stale requests and safely retry.
            return 0;
        });
    }
}
