namespace Core;

/// <summary>
/// Незмінний набір даних про середовище виконання застосунку.
/// </summary>
public record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string Architecture,
    string DotnetVersion,
    string Runtime,
    string AppDirectory,
    string CurrentDirectory,
    string Domain
);