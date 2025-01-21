using Reloaded.Mod.Loader.Update.Providers.GitHub;

namespace Reloaded.Mod.Launcher.Lib.Commands.Mod;

/// <summary>
/// A command that allows you to visit a website for a given mod.
/// </summary>
public class VisitModProjectUrlCommand : WithCanExecuteChanged, ICommand
{
    private static readonly GitHubReleasesUpdateResolverFactory _ghResolver = new();
    private static readonly GameBananaUpdateResolverFactory _gbResolver = new();

    private readonly PathTuple<ModConfig>? _modTuple;

    /// <inheritdoc />
    public VisitModProjectUrlCommand(PathTuple<ModConfig>? modTuple)
    {
        _modTuple = modTuple;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        if (_modTuple == null)
            return false;

        return !string.IsNullOrEmpty(GetWebsiteUrl());
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        ProcessExtensions.OpenHyperlink(GetWebsiteUrl()!);
    }

    private string? GetWebsiteUrl()
    {
        if (_modTuple == null)
            return null;

        if (!string.IsNullOrEmpty(_modTuple.Config.ProjectUrl))
        {
            return _modTuple.Config.ProjectUrl;
        }

        if (_ghResolver.TryGetConfiguration<GitHubReleasesUpdateResolverFactory.GitHubConfig>(_modTuple, out var ghConfig))
        {
            return $"https://www.github.com/{ghConfig.UserName}/{ghConfig.RepositoryName}";
        }

        if (_gbResolver.TryGetConfiguration<GameBananaUpdateResolverFactory.GameBananaConfig>(_modTuple, out var gbConfig))
        {
            return $"https://www.gamebanana.com/{gbConfig.ItemType.ToLowerInvariant()}s/{gbConfig.ItemId}";
        }

        return null;
    }
}