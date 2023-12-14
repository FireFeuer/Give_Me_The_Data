using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;


// код конфигурационного файла config.env
partial class Program
{
    static async Task Main(string[] args)
    {
        string ip = "";
        int port = 0;
        string configFilePath = await CreateConfigFile();

        Dictionary<string, string> network = await ReadConfigFile(configFilePath);

        ip = network["ip"];
        try
        {
            port = int.Parse(network["port"]);
        }
        catch
        {
            Console.WriteLine($"Используемый порт ({network["port"]}) имеет не верный формат, пожалуйста изменить его в файле config.env и перезапустите программу");
        }


        IPAddress ipAddress = IPAddress.None;
        try
        {
            ipAddress = IPAddress.Parse(ip);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Используемый IP-Адрес ({ip}) имеет не верный формат, пожалуйста изменить его в файле config.env и перезапустите программу");
            Console.ReadKey();
            return;
        }


        TcpListener server = new TcpListener(ipAddress, port);
        try
        {
            server.Start();
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Используемый порт ({port}) занят, пожалуйста изменить его в файле config.env и перезапустите программу");
            Console.ReadKey();
            return;
        }


        Console.WriteLine($"" +
            $"Сервер запущен\n" +
            $"Ip - {ip}\n" +
            $"Порт - {port}\n" +
            "Чтобы изменить эти параметры перейдите в файл config.env");


        while (true)
        {
            List<Dictionary<string, string>> dictitionariesList = await GetClientData(server);
            foreach (Dictionary<string, string> dict in dictitionariesList)
            {
                await SaveClientData(dict);
            }
        }
    }

    public static async Task<string> GetLocalIPAddress()
    {
        var host = await Dns.GetHostEntryAsync(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Console.WriteLine(ip.ToString());
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private async static Task<string> CreateConfigFile()
    {
        string path = Directory.GetCurrentDirectory();
        string pathToEnvFile = Path.Combine(path, "config.env");
        if (!File.Exists(pathToEnvFile))
        {
            await File.WriteAllTextAsync(pathToEnvFile, $"IP={await GetLocalIPAddress()}\r\nPORT=3333");
        }
        return pathToEnvFile;
    }

    private async static Task<Dictionary<string, string>> ReadConfigFile(string configFilePath)
    {
        using (var streamReader = new StreamReader(configFilePath))
        {
            string fileContents = await streamReader.ReadToEndAsync();
            string[] lines = fileContents.Split('\n');
            Dictionary<string, string> network = new Dictionary<string, string>(){
                { "ip", "-" },
                {"port","-" }
            };

            foreach (string line in lines)
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 2)
                {
                    string key_env = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key_env)
                    {
                        case "IP":
                            network["ip"] = value;
                            break;
                        case "PORT":
                            network["port"] = value;
                            break;
                    }
                }

            }
            return network;
        }
    }

    private async static Task<List<Dictionary<string, string>>> GetClientData(TcpListener server)
    {
        List<Dictionary<string, string>> clientData = new List<Dictionary<string, string>>();
        TcpClient client = await server.AcceptTcpClientAsync(); // Принимаем подключение от клиента

        // Читаем данные от клиента
        StreamReader reader = new StreamReader(client.GetStream());
        string data = "";
        try
        {
            data = await reader.ReadToEndAsync();
        }
        catch
        {
            Console.WriteLine("Поизощла ошибка на стороне отправителя");
        }

        // Создаем настройки для десириализации
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };

        data = data.Remove(data.LastIndexOf(","), 1);
        Console.WriteLine(data);

        byte[] byteArray = Encoding.UTF8.GetBytes(data);
        MemoryStream stream = new MemoryStream(byteArray);

        // Десиарилизируем полученную строку в словарь 
        clientData = await JsonSerializer.DeserializeAsync<List<Dictionary<string, string>>>(stream, options);
        client.Close();

        return clientData;
    }

    private async static Task SaveClientData(Dictionary<string, string> dict)
    {
        string fullInfo = $"" +
                   $"{dict["name"]}\n" +
                   $"{dict["driveInfo"]}\n" +
                   $"Операционная система - {dict["os"]}\n" +
                   $"{dict["IsWindowsUpdateNeeded"]}\n\n" +
                   $"Список установленных программ\n" +
                   $"{dict["programNames"]}\n\n" +
                   $"Список ошибок системы\n" +
                   $"{dict["eventsSystem"]}\n" +
                   $"Список ошибок приложений\n" +
                   $"{dict["eventsApplication"]}\n";

        Console.WriteLine(fullInfo);

        // Сохраняем данные в текстовый файл
        string fileName = $"{dict["name"]} инфо - {dict["time"]}.txt";

        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        folderPath = Path.Combine(folderPath, "PCInfo");
        folderPath = Path.Combine(folderPath, dict["name"]);
        string fullpath = Path.Combine(folderPath, fileName);

        DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }

        await File.WriteAllTextAsync(fullpath, fullInfo);
    }
}


