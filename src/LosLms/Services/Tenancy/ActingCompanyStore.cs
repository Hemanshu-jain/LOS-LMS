using System.Collections.Concurrent;

namespace LosLms.Services;

/// <summary>
/// Which company a SuperAdmin is currently "acting as". Set from the company picker, read by
/// <see cref="TenantContext"/> so every screen scopes to that one company — the same way a normal
/// company user is scoped. Only a SuperAdmin ever has an entry; everyone else is unaffected.
/// </summary>
/// <remarks>
/// ponytail: in-memory and single-server, like <c>AdminRequestNotifier</c>. It resets on restart, so
/// the SuperAdmin simply re-picks a company after a restart — fine for one vendor operator. A persisted
/// store is overkill until there are many concurrent super-admins.
/// </remarks>
public sealed class ActingCompanyStore
{
    private readonly ConcurrentDictionary<string, int> _byUser = new();

    public void Set(string userId, int companyId) => _byUser[userId] = companyId;

    public void Clear(string userId) => _byUser.TryRemove(userId, out _);

    public int? Get(string userId) => _byUser.TryGetValue(userId, out var id) ? id : null;
}
