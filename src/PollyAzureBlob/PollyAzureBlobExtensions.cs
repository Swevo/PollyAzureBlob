// <copyright file="PollyAzureBlobExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob;

/// <summary>
/// Extension methods for wrapping Azure Blob Storage clients with a Polly v8 resilience pipeline.
/// </summary>
public static class PollyAzureBlobExtensions
{
    /// <summary>
    /// Wraps <paramref name="client"/> in a <see cref="ResilientBlobClient"/> that executes
    /// every blob operation inside the supplied <paramref name="pipeline"/>.
    /// </summary>
    public static ResilientBlobClient WithPolly(
        this BlobClient client,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(pipeline);

        return new ResilientBlobClient(client, pipeline);
    }

    /// <summary>
    /// Wraps <paramref name="client"/> in a <see cref="ResilientBlobContainerClient"/> that
    /// executes every container operation inside the supplied <paramref name="pipeline"/>.
    /// </summary>
    public static ResilientBlobContainerClient WithPolly(
        this BlobContainerClient client,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(pipeline);

        return new ResilientBlobContainerClient(client, pipeline);
    }
}
