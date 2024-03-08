namespace Copy
{
    internal class Program
    {
        internal static Config Config = Config.Load(Config.ConfigPath);
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "config":
                        File.WriteAllText(Config.SchemePath, Config.GetScheme());
                        File.WriteAllText(Config.ConfigPath, Config.GetDefault());
                        break;
                    default:
                        Config.ConfigPath = args[0];
                        break;
                }
            }
        }
    }
}
