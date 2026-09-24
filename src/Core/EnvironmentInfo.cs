using System.Runtime.InteropServices;

namespace Core;

/// <summary>
/// Відповідає лише за збір даних про середовище виконання.
/// Друк результату — не завдання цього класу (це робить Cli).
/// </summary>
public static class EnvironmentInfo
{
    public static EnvironmentReport Collect()
    {
        return new EnvironmentReport(
            OsDescription: RuntimeInformation.OSDescription,
            OsVersion: Environment.OSVersion.ToString(),
            Architecture: RuntimeInformation.ProcessArchitecture.ToString(),
            DotnetVersion: Environment.Version.ToString(),
            Runtime: RuntimeInformation.FrameworkDescription,
            AppDirectory: AppContext.BaseDirectory,
            CurrentDirectory: Environment.CurrentDirectory,
            Domain: "Бібліотека (Book, BookCopy, Reader, Loan)"
        );
    }
}