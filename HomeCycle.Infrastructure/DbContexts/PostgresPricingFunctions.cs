namespace HomeCycle.Infrastructure.DbContexts;

// Maps the existing PostgreSQL built-in; no SQL function needs to be installed.
public static class PostgresPricingFunctions
{
    public static string RegexpReplace(string input, string pattern, string replacement, string flags) =>
        throw new NotSupportedException("Use this function only inside an EF Core database query.");
}
