using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Unicode;

IPAddress ipAddress = IPAddress.Parse("26.194.255.228");
int port = 3333;
TcpListener server = new TcpListener(ipAddress, port);
server.Start();

while (true)
{
    TcpClient client = server.AcceptTcpClient(); // Принимаем подключение от клиента

    // Читаем данные от клиента
    StreamReader reader = new StreamReader(client.GetStream());
    string data = reader.ReadToEnd();
    Console.WriteLine();

    // Создаем настройки для десириализации
    var options = new JsonSerializerOptions
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    // Десиарилизируем полученную строку в словарь 
    Dictionary<string, string> dict = JsonSerializer.Deserialize<Dictionary<string, string>>(data, options);

    client.Close();

    // Сохраняем данные в текстовый файл
    string fileName = $"{dict["name"]} инфо - {DateTime.Now.ToString("dd.MM.yyyy HH.mm")}.txt";

    string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    folderPath = Path.Combine(folderPath, "PCInfo");
    folderPath = Path.Combine(folderPath, dict["name"]);
    string fullpath = Path.Combine(folderPath, fileName);

    DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

    if (!dirInfo.Exists)
    {
        dirInfo.Create();
    }




    File.WriteAllText(fullpath, data);
    Console.WriteLine(data);
}
