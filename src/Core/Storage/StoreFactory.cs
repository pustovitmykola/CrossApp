using Core.Abstractions;

namespace Core.Storage;

/// <summary>ДОДАТКОВЕ 3: централізований вибір і комбінування реалізацій за аргументами.</summary>
public static class StoreFactory
{
    public const string DefaultFilePath = "data/catalog.json";

    public static IBookCopyStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        IBookCopyStore store = useFile
            ? new FileBookCopyStore(DefaultFilePath)
            : new InMemoryBookCopyStore(SampleData.Copies());

        if (useCache)
            store = new CachingBookCopyStore(store);

        return store;
    }
}