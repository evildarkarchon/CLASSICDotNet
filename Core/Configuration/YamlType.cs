namespace CLASSIC.Core.Configuration;

public enum YamlType
{
    /// <summary>
    /// CLASSIC Data/databases/CLASSIC Main.yaml
    /// </summary>
    Main,

    /// <summary>
    /// CLASSIC Settings.yaml
    /// </summary>
    Settings,

    /// <summary>
    /// CLASSIC Ignore.yaml
    /// </summary>
    Ignore,

    /// <summary>
    /// CLASSIC Data/databases/CLASSIC {Game}.yaml
    /// </summary>
    Game,

    /// <summary>
    /// CLASSIC Data/CLASSIC {Game} Local.yaml
    /// </summary>
    GameLocal,

    /// <summary>
    /// tests/test_settings.yaml
    /// </summary>
    Test
}