using LosLms.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LosLms.Data;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build a context, which it otherwise could not: the runtime
/// constructor takes a <see cref="TenantContext"/> that only exists inside a request scope.
/// </summary>
/// <remarks>
/// The alternative — giving <see cref="LosDbContext"/> a second, options-only constructor — was
/// rejected deliberately. That constructor would be a public, silent way to build a context with no
/// tenant filtering at all, and nothing would stop application code from using it by accident.
/// Confining the unscoped construction to a design-time-only factory keeps that door shut.
///
/// The connection string is a placeholder. Migration scaffolding never opens a connection, because
/// the server version is pinned rather than auto-detected.
/// </remarks>
public sealed class LosDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LosDbContext>
{
    public LosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LosDbContext>()
            .UseMySql(
                "Server=localhost;Port=3306;Database=los_lms;User Id=design-time;Password=design-time;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new LosDbContext(options, TenantContext.ForSeeding());
    }
}
