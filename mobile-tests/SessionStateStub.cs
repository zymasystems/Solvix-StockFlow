namespace Solvix.Sims.Services;

// Keeps InventoryService tests independent from Android-only MAUI Preferences.
public sealed class SessionState
{
    public void SetWarehouse(string warehouse) => HubName = warehouse;
    public string HubName { get; private set; } = "Pretoria Robotics Hub WH-01";

    public void SwitchCompany(string id, string name, string code, string hubName, string? icon = null)
    {
        HubName = hubName;
    }

    public void SignIn(string email, string hubName, string? companyId = null, string? companyName = null,
        string? companyCode = null, string? companyIcon = null, string? fullName = null, string? role = null)
    {
        HubName = hubName;
    }
}
