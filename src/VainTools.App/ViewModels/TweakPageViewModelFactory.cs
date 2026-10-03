using Microsoft.Extensions.Logging;
using VainTools.App.Models;
using VainTools.App.Services;
using VainTools.Framework.Services;

namespace VainTools.App.ViewModels;

/// <summary>
/// Builds <see cref="TweakPageViewModel"/> instances for the registry-tweak pages.
///
/// Pages are constructed by the DI container but each needs a different tweak list, so
/// they take the factory and ask for the page they represent rather than each
/// registering its own view-model type.
/// </summary>
public sealed class TweakPageViewModelFactory
{
    private readonly IRegistryTweakService _registry;
    private readonly IDialogService _dialogs;
    private readonly IInfoBarService _infoBar;
    private readonly ILogger<TweakPageViewModel> _logger;

    public TweakPageViewModelFactory(
        IRegistryTweakService registry,
        IDialogService dialogs,
        IInfoBarService infoBar,
        ILogger<TweakPageViewModel> logger)
    {
        _registry = registry;
        _dialogs = dialogs;
        _infoBar = infoBar;
        _logger = logger;
    }

    /// <summary>Creates a page view model for a named tweak set.</summary>
    public TweakPageViewModel Create(string title, IReadOnlyList<RegistryTweak> tweaks) =>
        new(title, tweaks, _registry, _dialogs, _infoBar, _logger);
}
