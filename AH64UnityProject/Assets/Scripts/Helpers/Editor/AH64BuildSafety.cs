using System;
using System.Linq;

/// <summary>Building assets must be safe in isolated checkouts and while the game is open.</summary>
public static class AH64BuildSafety
{
    // Explicit command-line opt-in; never persisted in editor preferences or project assets.
    // Normal menu, batch, Phase4.request and effect builds only write inside their checkout.
    public static bool ProfileDeploymentRequested =>
        Environment.GetCommandLineArgs().Contains("-ah64InstallToProfile");
}