using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
namespace Solvix.Web.Services.Internal;
internal static class ServiceRules
{
    public static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ServiceException(400, string.Join(" ", errors.Select(x => x.ErrorMessage)));
    }
    public static string Id(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('/') || value is "." or ".." || value.Length > 128)
            throw new ServiceException(400, "Invalid document ID.");
        return value;
    }
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static decimal Money(double value) => decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    public static void Price(decimal value)
    {
        if (decimal.Round(value, 2) != value) throw new ServiceException(400, "Price supports at most two decimal places.");
    }
    public static string Status(long stock, long min) => stock <= 0 ? "Out of Stock" : stock <= min ? "Low Stock" : "In Stock";
}
