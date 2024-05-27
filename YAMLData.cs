namespace CLASSICDotNet
{
    public class YAMLData
    {
        public static YamlCache CLASSIC_Main { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Main.yaml");
        public static YamlCache CLASSIC_Fallout4 { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Fallout4.yaml");
        public static YamlCache CLASSIC_Settings { get; set; } = new YamlCache("CLASSIC Settings.yaml");
        public static YamlCache CLASSIC_Ignore { get; set; } = new YamlCache("CLASSIC Ignore.yaml");
        public static YamlCache CLASSIC_FO4_Local { get; set; } = new YamlCache("CLASSIC Data/CLASSIC Fallout4 Local.yaml");
        public static YamlCache CLASSIC_FO4 { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Fallout4.yaml");

        public static dynamic? SettingsCheck(string key)
        {
            return CLASSIC_Settings.ReadOrUpdateEntry($"CLASSIC_Settings.{key}");
        }
    }
}
