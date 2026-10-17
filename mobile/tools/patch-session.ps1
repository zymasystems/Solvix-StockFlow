$p = "c:\Users\khuts\Desktop\Solvix.Sims\Solvix.Sims\Services\SessionState.cs"
$c = [System.IO.File]::ReadAllText($p)

if (-not $c.Contains("using Solvix.Sims.Models;")) {
    $c = "using Solvix.Sims.Models;" + [Environment]::NewLine + $c
    "Added using Solvix.Sims.Models"
}

$old1 = @"
        // Multi-Company Tenant Context
        public string ActiveCompanyId { get; private set; } = "solvix-robotics";
        public string ActiveCompanyName { get; private set; } = "Solvix Robotics & Automation";
        public string ActiveCompanyCode { get; private set; } = "SVX";
        public string ActiveCompanyIcon { get; private set; } = "precision_manufacturing";

        public void SignIn
"@

$new1 = @"
        // Multi-Company Tenant Context
        public string ActiveCompanyId { get; private set; } = "solvix-robotics";
        public string ActiveCompanyName { get; private set; } = "Solvix Robotics & Automation";
        public string ActiveCompanyCode { get; private set; } = "SVX";
        public string ActiveCompanyIcon { get; private set; } = "precision_manufacturing";

        // Active Employee Context (desktop admin assigns employees to warehouses)
        public Employee? CurrentEmployee { get; private set; }
        public List<Employee> Employees { get; } = new();

        public void SignIn
"@

if ($c.Contains($old1)) {
    $c = $c.Replace($old1, $new1)
    "Part 1: employees properties added"
} else {
    "Part 1: OLD NOT FOUND"
}

$old2 = @"
        public void SignOut()
        {
                IsSignedIn = false;
                Changed?.Invoke();
        }
"@

$new2 = @"
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
"@

if ($c.Contains($old2)) {
    $c = $c.Replace($old2, $new2)
    [System.IO.File]::WriteAllText($p, $c)
    "Part 2: methods added"
} else {
    "Part 2: OLD NOT FOUND"
}
