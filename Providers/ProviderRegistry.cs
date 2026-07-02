using System.Collections.Generic;
using System.Linq;

namespace Prism.Providers;

/// <summary>
/// Holds all available chat providers keyed by their <see cref="IChatProvider.Id"/>.
/// Register new providers where this is constructed (see App.axaml.cs).
/// </summary>
public sealed class ProviderRegistry
{
    private readonly Dictionary<string, IChatProvider> _providers;

    public ProviderRegistry(IEnumerable<IChatProvider> providers)
        => _providers = providers.ToDictionary(p => p.Id);

    public IReadOnlyList<IChatProvider> All => _providers.Values.ToList();

    public IChatProvider Get(string id) => _providers[id];

    public bool TryGet(string id, out IChatProvider provider)
        => _providers.TryGetValue(id, out provider!);
}
