$p = "c:\Users\khuts\Desktop\Solvix.Sims\Solvix.Sims\Services\SessionState.cs"
$c = [System.IO.File]::ReadAllText($p)

if (-not $c.Contains("using Solvix.Sims.Models;")) {
    $c = "using Solvix.Sims.Models;" + [Environment]::NewLine + $c
}

# Insert employee properties after the ActiveCompanyIcon line
$anchor1 = "        public string ActiveCompanyIcon { get; private set; } = ""precision_manufacturing"";"
$insert1 = @"

        // Active Employee Context (desktop admin assigns employees to warehouses)
        public Employee? CurrentEmployee { get; private set; }
        public List<Employee> Employees { get; } = new();"
if ($c.Contains($anchor1)) {
    $c = $c.Replace($anchor1, $anchor1 + $insert1)
    "Inserted employee properties"
} else {
    "Anchor1 NOT FOUND"
}

# Replace SignOut method
$anchor2 = @"
        public void SignOut()
        {
                IsSignedIn = false;
                Changed?.Invoke();
        }"
$replace2 = @"
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
        }"
if ($c.Contains($anchor2)) {
    $c = $c.Replace($anchor2, $replace2)
    "Replaced SignOut method"
} else {
    "Anchor2 NOT FOUND"
}

[System.IO.File]::WriteAllText($p, $c)
"Done"
