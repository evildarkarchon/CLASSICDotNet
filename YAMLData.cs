namespace CLASSICDotNet
{
    public class YAMLData
    {
        public YamlCache CLASSIC_Main { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Main.yaml");
        public YamlCache CLASSIC_Fallout4 { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Fallout4.yaml");
        public YamlCache CLASSIC_Settings { get; set; } = new YamlCache("CLASSIC Settings.yaml");
        public YamlCache CLASSIC_Ignore { get; set; } = new YamlCache("CLASSIC Ignore.yaml");
        public YamlCache CLASSIC_FO4_Local { get; set; } = new YamlCache("CLASSIC Data/CLASSIC Fallout4 Local.yaml");
        public YamlCache CLASSIC_FO4 { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Fallout4.yaml");

        public dynamic? SettingsCheck(string key)
        {
            return CLASSIC_Settings.ReadOrUpdateEntry($"CLASSIC_Settings.{key}");
        }
    }
    public class YamlInstance
    {
        public static YAMLData Data { get; set; } = new YAMLData();
    }
}
