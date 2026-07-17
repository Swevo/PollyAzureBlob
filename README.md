# PollyAzureBlob

[![NuGet](https://img.shields.io/nuget/v/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob)
[![CI](https://github.com/Swevo/PollyAzureBlob/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyAzureBlob/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Polly v8 resilience for Azure Blob Storage** — retry, timeout, and circuit-breaker for `BlobClient` and `BlobContainerClient` with a single `WithPolly(pipeline)` call. Drop-in decorator, zero changes to your existing blob code.

```csharp
// Before
await blobClient.UploadAsync(stream, overwrite: true);

// After — automatic retry + timeout on every operation
var resilient = blobClient.WithPolly(pipeline);
await resilient.UploadAsync(stream, overwrite: true);
```

---

## Installation

```bash
dotnet add package PollyAzureBlob
```

Targets **net6.0**, **net8.0**, and **net9.0**.
Dependencies: `Polly.Core 8.*`, `Azure.Storage.Blobs 12.*`, `Microsoft.Extensions.DependencyInjection.Abstractions 8.*`

---

## Quick start

### BlobClient

```csharp
using PollyAzureBlob;

var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex =>
            ex.Status is 408 or 429 or 500 or 502 or 503 or 504),
    })
    .AddTimeout(TimeSpan.FromSeconds(30))
    .Build();

var resilient = blobClient.WithPolly(pipeline);

// Upload
await resilient.UploadAsync(stream, overwrite: true);

// Download
var result = await resilient.DownloadContentAsync();
var data = result.Value.Content;

// Delete
await resilient.DeleteIfExistsAsync();

// Check existence
var exists = await resilient.ExistsAsync();
```

### BlobContainerClient

```csharp
var resilientContainer = containerClient.WithPolly(pipeline);

// Create container
await resilientContainer.CreateIfNotExistsAsync();

// Upload blob via container
await resilientContainer.UploadBlobAsync("report.pdf", stream);

// List blobs
var blobs = await resilientContainer.GetBlobsAsync(prefix: "reports/");

// Get a resilient BlobClient — shares the same pipeline
var resilientBlob = resilientContainer.GetBlobClient("report.pdf");
var content = await resilientBlob.DownloadContentAsync();
```

### Dependency injection

```csharp
// Program.cs
builder.Services.AddSingleton(_ =>
    new BlobServiceClient(builder.Configuration["AzureStorage:ConnectionString"]));

builder.Services.AddPollyAzureBlob(pipeline =>
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex =>
                ex.Status is 408 or 429 or 500 or 502 or 503 or 504),
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15),
        }));

// Repository
public class BlobRepository(BlobContainerClient container, ResiliencePipeline pipeline)
{
    public Task UploadAsync(string name, Stream content) =>
        container.WithPolly(pipeline).UploadBlobAsync(name, content);

    public async Task<BinaryData> DownloadAsync(string name) =>
        (await container.WithPolly(pipeline).GetBlobClient(name).DownloadContentAsync()).Value.Content;
}
```

---

## Supported operations

### ResilientBlobClient

| Method | Description |
|--------|-------------|
| `UploadAsync(Stream, ...)` | Upload from stream |
| `UploadAsync(BinaryData, ...)` | Upload from BinaryData |
| `DownloadContentAsync` | Download blob content |
| `DownloadToAsync` | Download to stream |
| `DeleteAsync` | Delete blob |
| `DeleteIfExistsAsync` | Delete blob if it exists |
| `ExistsAsync` | Check if blob exists |
| `GetPropertiesAsync` | Get blob properties |
| `SetHttpHeadersAsync` | Set HTTP headers |
| `SetMetadataAsync` | Set metadata |
| `CreateSnapshotAsync` | Create blob snapshot |

### ResilientBlobContainerClient

| Method | Description |
|--------|-------------|
| `GetBlobClient(name)` | Get a `ResilientBlobClient` sharing the same pipeline |
| `CreateAsync` | Create container |
| `CreateIfNotExistsAsync` | Create container if not exists |
| `DeleteAsync` | Delete container |
| `DeleteIfExistsAsync` | Delete container if exists |
| `ExistsAsync` | Check if container exists |
| `GetPropertiesAsync` | Get container properties |
| `UploadBlobAsync` | Upload blob by name |
| `DeleteBlobAsync` | Delete blob by name |
| `DeleteBlobIfExistsAsync` | Delete blob by name if exists |
| `GetBlobsAsync` | List blobs (returns `List<BlobItem>`) |

---

## Recommended transient exceptions

Azure Blob Storage throws `RequestFailedException`. These HTTP status codes are safe to retry:

```csharp
ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex =>
    ex.Status is 408   // Request Timeout
              or 429   // Too Many Requests (throttled)
              or 500   // Internal Server Error
              or 502   // Bad Gateway
              or 503   // Service Unavailable
              or 504)  // Gateway Timeout
```

---

## Pipeline order

```
[Timeout] → [Retry] → [Circuit Breaker] → [Azure Blob Storage]
```

```csharp
pipeline
    .AddTimeout(TimeSpan.FromSeconds(30))   // 1. Overall deadline
    .AddRetry(retryOptions)                 // 2. Retry transient failures
    .AddCircuitBreaker(cbOptions)           // 3. Open circuit under sustained failures
```

---

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers — expose circuit-breaker state (Closed, HalfOpen, Open, Isolated) as /health endpoint responses |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Backoff delay strategies for Polly v8 resilience pipelines |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience pipelines for Entity Framework Core — wrap every EF Core query and SaveChanges with retry, timeout and circuit-breaker via a single AddPollyResilience() call |
| [PollyMailKit](https://www.nuget.org/packages/PollyMailKit) | [![Downloads](https://img.shields.io/nuget/dt/PollyMailKit.svg)](https://www.nuget.org/packages/PollyMailKit) | Polly v8 resilience pipelines for MailKit — retry, timeout, and circuit-breaker for SmtpClient.SendAsync and any MailKit SMTP operation |
| [PollyMassTransit](https://www.nuget.org/packages/PollyMassTransit) | [![Downloads](https://img.shields.io/nuget/dt/PollyMassTransit.svg)](https://www.nuget.org/packages/PollyMassTransit) | Polly v8 resilience pipelines for MassTransit — retry, timeout, and circuit-breaker for IBus.Publish and ISendEndpointProvider.Send |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI API calls |
| [PollyAzureEventHub](https://www.nuget.org/packages/PollyAzureEventHub) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureEventHub.svg)](https://www.nuget.org/packages/PollyAzureEventHub) | Polly v8 resilience pipelines for Azure Event Hubs — retry, timeout, and circuit-breaker for EventHubProducerClient and EventHubConsumerClient |
| [PollyElasticsearch](https://www.nuget.org/packages/PollyElasticsearch) | [![Downloads](https://img.shields.io/nuget/dt/PollyElasticsearch.svg)](https://www.nuget.org/packages/PollyElasticsearch) | Polly v8 resilience pipelines for Elastic.Clients.Elasticsearch 8+ — retry, timeout, and circuit-breaker for any Elasticsearch operation, plus a built-in ElasticTransientErrors predicate covering rate limiting (429), service unavailability (503), gateway timeouts (504), and connection failures |
| [PollyHangfire](https://www.nuget.org/packages/PollyHangfire) | [![Downloads](https://img.shields.io/nuget/dt/PollyHangfire.svg)](https://www.nuget.org/packages/PollyHangfire) | Polly v8 resilience pipelines for Hangfire — retry, timeout, and circuit-breaker for IBackgroundJobClient.Enqueue and Schedule |
| [PollyCosmosDb](https://www.nuget.org/packages/PollyCosmosDb) | [![Downloads](https://img.shields.io/nuget/dt/PollyCosmosDb.svg)](https://www.nuget.org/packages/PollyCosmosDb) | Polly v8 resilience pipelines for Azure Cosmos DB — retry, timeout, and circuit-breaker for Container operations, plus a built-in CosmosTransientErrors predicate covering rate limiting (429), timeouts (408), partition failovers (410), and service unavailability (503) |
| [PollySendGrid](https://www.nuget.org/packages/PollySendGrid) | [![Downloads](https://img.shields.io/nuget/dt/PollySendGrid.svg)](https://www.nuget.org/packages/PollySendGrid) | Polly v8 resilience pipelines for SendGrid — retry, timeout, and circuit-breaker for ISendGridClient.SendEmailAsync |
| [PollyMongo](https://www.nuget.org/packages/PollyMongo) | [![Downloads](https://img.shields.io/nuget/dt/PollyMongo.svg)](https://www.nuget.org/packages/PollyMongo) | Polly v8 resilience pipelines for MongoDB.Driver — wrap Find, InsertOne, UpdateOne, DeleteOne and other IMongoCollection calls with retry, timeout, circuit-breaker, and more using a single ResilientMongoCollection decorator |
| [PollyDapper](https://www.nuget.org/packages/PollyDapper) | [![Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper) | Polly v8 resilience pipelines for Dapper — wrap QueryAsync, ExecuteAsync, and other Dapper calls with retry, timeout, circuit-breaker, and more using a single ResilientDbConnection decorator |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience pipelines for MediatR — add retry, timeout, circuit-breaker, rate-limiting, hedging, and chaos engineering to any MediatR request handler with a single line of DI registration |
| [PollySqlClient](https://www.nuget.org/packages/PollySqlClient) | [![Downloads](https://img.shields.io/nuget/dt/PollySqlClient.svg)](https://www.nuget.org/packages/PollySqlClient) | Polly v8 resilience pipelines for Microsoft.Data.SqlClient (SQL Server and Azure SQL) — retry, timeout, and circuit-breaker for SqlConnection queries and commands, plus a built-in SqlServerTransientErrors predicate covering all common SQL Server and Azure SQL transient error numbers |
| [PollyAzureKeyVault](https://www.nuget.org/packages/PollyAzureKeyVault) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureKeyVault.svg)](https://www.nuget.org/packages/PollyAzureKeyVault) | Polly v8 resilience pipelines for Azure Key Vault — retry, timeout, and circuit-breaker for SecretClient, KeyClient, and CertificateClient |
| [PollyAzureQueueStorage](https://www.nuget.org/packages/PollyAzureQueueStorage) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureQueueStorage.svg)](https://www.nuget.org/packages/PollyAzureQueueStorage) | Polly v8 resilience pipelines for Azure Queue Storage — retry, timeout, and circuit-breaker for Azure.Storage.Queues QueueClient |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureServiceBus.svg)](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience for Azure Service Bus — retry, circuit breaker, and timeout for sending and receiving messages |
| [PollyAzureTableStorage](https://www.nuget.org/packages/PollyAzureTableStorage) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureTableStorage.svg)](https://www.nuget.org/packages/PollyAzureTableStorage) | Polly v8 resilience pipelines for Azure Table Storage — retry, timeout, and circuit-breaker for Azure.Data.Tables TableClient |
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | [![Downloads](https://img.shields.io/nuget/dt/PollyChaos.svg)](https://www.nuget.org/packages/PollyChaos) | Chaos engineering and fault-injection resilience strategies for Polly v8 pipelines |

## 💼 Need .NET consulting?

The author of this package is available for consulting on **Polly v8 resilience**, **Azure cloud architecture**, and **clean .NET design**.

**[→ solidqualitysolutions.com](https://www.solidqualitysolutions.com/)** · **[LinkedIn](https://www.linkedin.com/in/justbannister/)**
## License

MIT
