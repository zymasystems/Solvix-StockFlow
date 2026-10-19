using Solvix.Sims.Services;

namespace Solvix.Sims.Services.Authentication;
/// <summary>Firebase identity plus approval verified by the existing backend.</summary>
public sealed class MobileAuthCoordinator
{
    private readonly IFirebaseAuthService firebase;
    private readonly ApiService api;
    private readonly SessionState session;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    public BackendProfile? Profile { get; private set; }
    public bool IsApproved => Profile is not null; // Identity-only shell; every company operation checks active membership on the server.

    public event Action? Changed;
    public MobileAuthCoordinator(IFirebaseAuthService firebase, ApiService api, SessionState session)
    {
        this.firebase = firebase;
        this.api = api;
        this.session = session;
        firebase.SignedOut += ClearProfile;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized)
                return;
            if (await firebase.RestoreAsync(ct))
                await LoadProfileAsync(ct);
            initialized = true;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SignInAsync(string email, string password, bool remember, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            ClearProfile();
            await firebase.SignInAsync(email, password, remember, ct);
            await LoadProfileAsync(ct);
            initialized = true;
        }
        catch
        {
            ClearProfile();
            await firebase.SignOutAsync();
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RegisterAsync(string email, string password, RegisterProfileInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) || input.FirstName.Trim().Length > 100 || string.IsNullOrWhiteSpace(input.LastName) || input.LastName.Trim().Length > 100 || string.IsNullOrWhiteSpace(input.Company) || input.Company.Trim().Length > 150 || !input.AcceptTerms)
            throw new AuthException("PROFILE_REQUIRED", "Enter your first name, last name and company, and accept your organisation's access terms.");
        await gate.WaitAsync(ct);
        try
        {
            ClearProfile();
            try
            {
                await firebase.RegisterAsync(email, password, ct);
            }
            catch (AuthException error)when (error.Code == "EMAIL_EXISTS")
            {
                // Recover an interrupted signup only after proving ownership with a real password sign-in.
                await firebase.SignInAsync(email, password, false, ct);
            }

            try
            {
                await api.PostAsync<RegisterProfileInput, BackendProfile>("api/profile", input, ct);
            }
            catch (ApiException error)when (error.StatusCode == 409)
            {
                throw new AuthException("PROFILE_EXISTS", "Your profile already exists. Sign in if approved, or contact your administrator.");
            }
        }
        finally
        {
            // Registration does not open the application; current backend creates staff/Inactive.
            try
            {
                ClearProfile();
                await firebase.SignOutAsync();
                initialized = true;
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private async Task LoadProfileAsync(CancellationToken ct)
    {
        BackendProfile? profile;
        try
        {
            profile = await api.GetAsync<BackendProfile>("api/profile", ct);
        }
        catch (ApiException error)when (error.StatusCode == 403)
        {
            ClearProfile();
            throw new AuthException("APPROVAL_REQUIRED", "Your application profile is missing, inactive, or not approved. Contact your administrator. If signup was interrupted, complete registration again with the same email and password.");
        }

        if (profile is null || profile.Uid != firebase.UserId)
        {
            ClearProfile();
            throw new AuthException("APPROVAL_REQUIRED", "Sign in again to load your account.");
        }

        Profile = profile;
        session.SignOut(); // Clear old UI identity/company selection.
        session.SignIn(profile.Email, "", companyName: profile.Company, fullName: profile.DisplayName, role: profile.Role);
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        await gate.WaitAsync();
        try
        {
            ClearProfile();
            await firebase.SignOutAsync();
            initialized = true;
        }
        finally
        {
            gate.Release();
        }
    }

    private void ClearProfile()
    {
        Profile = null;
        session.SignOut();
        Changed?.Invoke();
    }
}
