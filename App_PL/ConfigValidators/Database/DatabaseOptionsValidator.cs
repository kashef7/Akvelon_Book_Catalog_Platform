using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace App_PL.ConfigValidators.Database;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";
    public string DefaultConnection { get; set; } = string.Empty;
}

public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();
        var value = options.DefaultConnection;

        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add("ConnectionStrings:DefaultConnection is missing. Set it via appsettings, environment variables, or .env.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (value.Contains("ChangeMe", StringComparison.OrdinalIgnoreCase))
            failures.Add("ConnectionStrings:DefaultConnection is still the placeholder value from appsettings.Development.json.");

        try
        {
            var builder = new SqlConnectionStringBuilder(value);

            if (string.IsNullOrWhiteSpace(builder.DataSource))
                failures.Add("ConnectionStrings:DefaultConnection has no Server/Data Source specified.");

            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
                failures.Add("ConnectionStrings:DefaultConnection has no Database/Initial Catalog specified.");

            if (!builder.IntegratedSecurity && string.IsNullOrWhiteSpace(builder.UserID))
                failures.Add("ConnectionStrings:DefaultConnection uses SQL auth but has no User Id.");

            if (!builder.IntegratedSecurity && string.IsNullOrWhiteSpace(builder.Password))
                failures.Add("ConnectionStrings:DefaultConnection uses SQL auth but has an empty Password.");
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            failures.Add($"ConnectionStrings:DefaultConnection is not a valid connection string: {ex.Message}");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}