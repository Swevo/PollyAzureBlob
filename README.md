# PollyAzureBlob

[![NuGet](https://img.shields.io/nuget/v/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyAzureBlob.svg)](https://www.nuget.org/packages/PollyAzureBlob)
[![CI](https://github.com/Swevo/PollyAzureBlob/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyAzureBlob/actions)

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

## Related packages

| Package | Downloads | Description |
|---|---|---|
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | [![Downloads](https://img.shields.io/nuget/dt/PollyAzureServiceBus.svg)](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience for Azure Service Bus senders and receivers |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | [![Downloads](https://img.shields.io/nuget/dt/PollyEFCore.svg)](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollyDapper](https://www.nuget.org/packages/PollyDapper) | [![Downloads](https://img.shields.io/nuget/dt/PollyDapper.svg)](https://www.nuget.org/packages/PollyDapper) | Polly v8 resilience for Dapper queries and commands |
| [PollyMongo](https://www.nuget.org/packages/PollyMongo) | [![Downloads](https://img.shields.io/nuget/dt/PollyMongo.svg)](https://www.nuget.org/packages/PollyMongo) | Polly v8 resilience for MongoDB.Driver |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | [![Downloads](https://img.shields.io/nuget/dt/PollyMediatR.svg)](https://www.nuget.org/packages/PollyMediatR) | Polly v8 resilience pipelines for MediatR |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyBackoff](https://www.nuget.org/packages/PollyBackoff) | [![Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff) | Jitter, linear & custom backoff for Polly v8 retry |
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | [![Downloads](https://img.shields.io/nuget/dt/PollyChaos.svg)](https://www.nuget.org/packages/PollyChaos) | Fault & latency injection (Simmy for Polly v8) |

---

## License

MIT
