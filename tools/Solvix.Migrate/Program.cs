using Google.Cloud.Firestore;
using Solvix.Migrate;

namespace Solvix.Migrate;
internal static class Cli
{
    public static async Task<int> Main(string[] args)
    {
        string Required(string name)
        {
            var i = Array.IndexOf(args, name);
            if (i < 0 || i + 1 >= args.Length)
                throw new ArgumentException("Required: " + name);
            return args[i + 1];
        }

        try
        {
            var project = Required("--project");
            var company = Required("--company");
            var warehouse = Required("--warehouse");
            var owner = Required("--owner-uid");
            var db = new FirestoreDbBuilder
            {
                ProjectId = project
            }.Build(); // Operator ADC, never a client credential.
            var count = await LegacyMigration.RunAsync(db, company, warehouse, owner, args.Contains("--legacy-price-is-cost"), args.Contains("--apply"));
            Console.WriteLine($"{(args.Contains("--apply") ? "Imported/resumed" : "DRY RUN ONLY")} {count} legacy documents. Sources retained.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
