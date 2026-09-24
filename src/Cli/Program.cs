using System.Text.Json;
using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool jsonMode = args.Contains("--json");

EnvironmentReport report = EnvironmentInfo.Collect();

if (jsonMode)
{
    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    Console.WriteLine(JsonSerializer.Serialize(report, jsonOptions));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Пустовіт М.Д., група ФЕІ-34");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)    : {report.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {report.Architecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {report.DotnetVersion}");
    Console.WriteLine($"Runtime             : {report.Runtime}");
    Console.WriteLine($"Каталог застосунку  : {report.AppDirectory}");
    Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Предметна область: {report.Domain} — облік видач примірників книг читачам");
}