namespace Give_Me_The_Data
{
    internal class Config
    {
        public static async Task<string[]> ReadConfigFileAsync(string configFilePath)
        {
            using (var streamReader = new StreamReader(configFilePath))
            {
                string fileContents = await streamReader.ReadToEndAsync();
                string[] lines = fileContents.Split('\n');
                return lines;
            }
        }

        public static async Task<string> GetIPAdressAsync(string configFilePath)
        {
            string ip = "-";
            foreach (string line in await ReadConfigFileAsync(configFilePath))
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 2)
                {
                    string key_env = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key_env)
                    {
                        case "IP":
                            ip = value;
                            break;
                    }
                }

            }
            return ip;
        }


        public static async Task<string> GetPortAsync(string configFilePath)
        {
            string port = "-";
            foreach (string line in await ReadConfigFileAsync(configFilePath))
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 2)
                {
                    string key_env = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key_env)
                    {
                        case "PORT":
                            port = value;
                            break;
                    }
                }

            }
            return port;
        }
    }
}
