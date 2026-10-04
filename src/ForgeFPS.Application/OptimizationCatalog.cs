namespace ForgeFPS.Application;

using Domain;

/// <summary>
/// Catalog of available optimization actions.
/// </summary>
public class OptimizationCatalog
{
    private readonly List<IOptimizationAction> _actions = new();

    public IReadOnlyList<IOptimizationAction> All => _actions;

    public void Register(IOptimizationAction action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (_actions.Any(a => a.Id == action.Id))
            throw new ArgumentException($"Action with Id '{action.Id}' already registered.", nameof(action));
        _actions.Add(action);
    }

    public IOptimizationAction? Get(string id) => _actions.FirstOrDefault(a => a.Id == id);

    public IReadOnlyList<IOptimizationAction> GetByCategory(OptimizationCategory category)
        => _actions.Where(a => a.Category == category).ToList();

    public IReadOnlyList<IOptimizationAction> GetSafeActions()
        => _actions.Where(a => a.Risk == RiskLevel.Low).ToList();

    public IReadOnlyList<IOptimizationAction> GetForGame(string gameName)
        => _actions.Where(a => a.CompatibilityTags.Contains(gameName)).ToList();
}