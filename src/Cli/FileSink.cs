using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public void Notify(LendingChangedEventArgs e) =>
        File.AppendAllText(_path, LendingMessages.Format(e) + Environment.NewLine);
}