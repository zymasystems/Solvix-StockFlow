using Solvix.Sims.Models;
using Microsoft.Maui.Storage;

namespace Solvix.Sims.Services;

/// <summary>Who is signed in, active tenant company, active warehouse site, and active employee.</summary>
public class SessionState
{
	public event Action? Changed;
	public SessionState()
	{
		LowStockAlertsEnabled = LoadPreference("notifications.low-stock", true);
		DailySummaryEnabled = LoadPreference("notifications.daily-summary", false);
		InboundApprovalNotificationsEnabled = LoadPreference("notifications.inbound-approvals", true);
		OutboundApprovalNotificationsEnabled = LoadPreference("notifications.outbound-approvals", true);
		DispatchCompletionNotificationsEnabled = LoadPreference("notifications.dispatch-complete", true);
	}

	public bool IsSignedIn { get; private set; } = true;
	public string ProfilePictureDataUrl { get; private set; } = "";
	public bool LowStockAlertsEnabled { get; private set; } = true;
	public bool DailySummaryEnabled { get; private set; }
	public bool InboundApprovalNotificationsEnabled { get; private set; } = true;
	public bool OutboundApprovalNotificationsEnabled { get; private set; } = true;
	public bool DispatchCompletionNotificationsEnabled { get; private set; } = true;
	public string FullName { get; private set; } = "Alex Chen";
	public string Email { get; private set; } = "alex.chen@solvix-robotics.com";
	public string FirstName => FullName.Split(' ')[0];
	public string Initials => string.Concat(FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => p[0])).ToUpperInvariant();
	public string Role { get; private set; } = "Inventory Operations Lead";
	public string Terminal { get; private set; } = "Terminal 01";
	public string HubName { get; private set; } = "Pretoria Robotics Hub WH-01";

	// Multi-Company Tenant Context
	public string ActiveCompanyId { get; private set; } = "solvix-robotics";
	public string ActiveCompanyName { get; private set; } = "Solvix Robotics & Automation";
	public string ActiveCompanyCode { get; private set; } = "SVX";
	public string ActiveCompanyIcon { get; private set; } = "precision_manufacturing";

	// Active Employee Context (desktop admin assigns employees to warehouses)
	public Employee? CurrentEmployee { get; private set; }
	public List<Employee> Employees { get; private set; } = new();

	public void SignIn(string email, string hubName, string? companyId = null, string? companyName = null, string? companyCode = null, string? companyIcon = null, string? fullName = null, string? role = null)
	{
		IsSignedIn = true;
		Email = email;
		HubName = hubName;
		if (!string.IsNullOrWhiteSpace(companyId)) ActiveCompanyId = companyId;
		if (!string.IsNullOrWhiteSpace(companyName)) ActiveCompanyName = companyName;
		if (!string.IsNullOrWhiteSpace(companyCode)) ActiveCompanyCode = companyCode;
		if (!string.IsNullOrWhiteSpace(companyIcon)) ActiveCompanyIcon = companyIcon;
		if (!string.IsNullOrWhiteSpace(role)) Role = role;
		if (!string.IsNullOrWhiteSpace(fullName))
		{
			FullName = fullName;
		}
		else if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
		{
			var rawName = email.Split('@')[0].Replace('.', ' ').Replace('_', ' ').Trim();
			FullName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName);
		}
		Changed?.Invoke();
	}

	public void SwitchCompany(string companyId, string companyName, string companyCode, string hubName, string? companyIcon = null)
	{
		ActiveCompanyId = companyId;
		ActiveCompanyName = companyName;
		ActiveCompanyCode = companyCode;
		HubName = hubName;
		if (!string.IsNullOrWhiteSpace(companyIcon)) ActiveCompanyIcon = companyIcon;
		Changed?.Invoke();
	}

	public void SetWarehouse(string warehouse)
	{
		if (string.IsNullOrWhiteSpace(warehouse) || string.Equals(HubName, warehouse, StringComparison.OrdinalIgnoreCase)) return;
		HubName = warehouse;
		Changed?.Invoke();
	}

	public void SetHub(string hubName)
	{
		HubName = hubName;
		Changed?.Invoke();
	}

	public void UpdateProfile(string fullName, string email, string? profilePictureDataUrl = null)
	{
		if (!string.IsNullOrWhiteSpace(fullName)) FullName = fullName.Trim();
		if (!string.IsNullOrWhiteSpace(email)) Email = email.Trim();
		if (profilePictureDataUrl is not null) ProfilePictureDataUrl = profilePictureDataUrl;
		Changed?.Invoke();
	}

	public void UpdatePreferences(bool lowStockAlerts, bool dailySummary, bool inboundApprovals, bool outboundApprovals, bool dispatchComplete)
	{
		LowStockAlertsEnabled = lowStockAlerts;
		DailySummaryEnabled = dailySummary;
		InboundApprovalNotificationsEnabled = inboundApprovals;
		OutboundApprovalNotificationsEnabled = outboundApprovals;
		DispatchCompletionNotificationsEnabled = dispatchComplete;
		SavePreference("notifications.low-stock", lowStockAlerts);
		SavePreference("notifications.daily-summary", dailySummary);
		SavePreference("notifications.inbound-approvals", inboundApprovals);
		SavePreference("notifications.outbound-approvals", outboundApprovals);
		SavePreference("notifications.dispatch-complete", dispatchComplete);
		Changed?.Invoke();
	}

	private static bool LoadPreference(string key, bool fallback)
	{
		try { return Preferences.Default.Get(key, fallback); }
		catch { return fallback; }
	}

	private static void SavePreference(string key, bool value)
	{
		try { Preferences.Default.Set(key, value); }
		catch { }
	}

	public void SignOut()
	{
		IsSignedIn = false;
		CurrentEmployee = null;
		Employees.Clear();
		Changed?.Invoke();
	}

	public void SetEmployees(List<Employee> employees)
	{
		Employees = employees;
		if (employees.Count > 0 && CurrentEmployee is null)
		{
			CurrentEmployee = employees.First();
		}
		Changed?.Invoke();
	}

	public void SetCurrentEmployee(string employeeId)
	{
		CurrentEmployee = Employees.FirstOrDefault(e => e.Id == employeeId);
		if (CurrentEmployee is not null && !string.IsNullOrWhiteSpace(CurrentEmployee.AssignedWarehouse))
		{
			HubName = CurrentEmployee.AssignedWarehouse;
		}
		Changed?.Invoke();
	}

	public string Greeting => DateTime.Now.Hour switch
	{
		< 12 => "Good morning",
		< 18 => "Good afternoon",
		_ => "Good evening"
	};
}
