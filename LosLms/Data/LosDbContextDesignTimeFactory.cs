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
/// The data source is a throwaway path — migration scaffolding builds the model, it never opens the
/// file — but it must be the same provider (SQLite) the app runs on, so the generated migrations match.
/// </remarks>
public sealed class LosDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LosDbContext>
{
    public LosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LosDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new LosDbContext(options, TenantContext.ForSeeding());
    }
}
