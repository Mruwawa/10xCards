using Microsoft.Extensions.Configuration;
using Mongo2Go;
using _10xCards.backend.Infrastructure;

namespace _10xCards.backend.Tests;

public class MongoFixture : IDisposable
{
    private readonly MongoDbRunner? _runner;
    public MongoSettings Settings { get; }
    public IConfiguration Configuration { get; }

    public MongoFixture()
    {
        var external = Environment.GetEnvironmentVariable("MONGO_TEST_CONN");
        if (!string.IsNullOrWhiteSpace(external))
        {
            // CI: użyj kontenera serwisowego (szybciej, mniej kruchych pobrań binariów)
            Settings = new MongoSettings { ConnectionString = external!, Database = $"testdb_{Guid.NewGuid()}" };
        }
        else
        {
            // Lokalnie: lekka instancja Mongo2Go
            _runner = MongoDbRunner.Start(singleNodeReplSet: false); // replSet niepotrzebny dla naszych testów
            Settings = new MongoSettings { ConnectionString = _runner.ConnectionString, Database = $"testdb_{Guid.NewGuid()}" };
        }

        var inMemory = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test_key_12345678901234567890_extra_len_" // >=32 bytes
        };
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory!).Build();
    }

    public void Dispose()
    {
        _runner?.Dispose();
    }
}
