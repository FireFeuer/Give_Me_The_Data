using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;


// код конфигурационного файла config.env
partial class Program
{

    static async Task Main(string[] args)
    {
        string path = Directory.GetCurrentDirectory();

        string pathToEnvFile = Path.Combine(path, "config.env");
        string ip = "";
        int port = 0;
        string autoload = "";

        if (!File.Exists(pathToEnvFile))
        {
            await File.WriteAllTextAsync(pathToEnvFile, $"IP={GetLocalIPAddress()}\r\nPORT=3333\nAUTOLOAD=TRUE");
        }
        using (var streamReader = new StreamReader(pathToEnvFile))
        {
            string fileContents = await streamReader.ReadToEndAsync();
            string[] lines = fileContents.Split('\n');

            foreach (string line in lines)
            {
                string[] keyValue = line.Split('=');
                if (keyValue.Length == 3 || keyValue.Length == 2)
                {
                    string key_env = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    switch (key_env)
                    {
                        case "IP":
                            ip = value;
                            break;
                        case "PORT":
                            try
                            {
                                port = int.Parse(value);
                            }
                            catch
                            {
                                Console.WriteLine($"Используемый порт ({value}) имеет не верный формат, пожалуйста изменить его в файле config.env и перезапустите программу");
                                return;
                            }

                            break;
                        case "AUTOLOAD":
                            autoload = value;
                            break;
                    }
                }
            }

            Console.WriteLine(autoload);
            if (autoload == "TRUE")
            {
                const string applicationName = "PCInfo";
                const string pathRegistryKeyStartup =
                            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

                using (RegistryKey registryKeyStartup = Registry.CurrentUser.OpenSubKey(pathRegistryKeyStartup, true))
                {
                    registryKeyStartup.SetValue(
                        applicationName,
                        string.Format("\"{0}\"", Assembly.GetExecutingAssembly().Location));
                }
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
                List<Dictionary<string, string>> dictitionariesList = await JsonSerializer.DeserializeAsync<List<Dictionary<string, string>>>(stream, options);
                client.Close();


                foreach (Dictionary<string, string> dict in dictitionariesList)
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
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}


