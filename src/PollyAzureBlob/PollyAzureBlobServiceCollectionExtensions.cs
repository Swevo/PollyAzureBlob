// <copyright file="PollyAzureBlobServiceCollectionExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob;

/// <summary>
/// Extension methods for registering a shared <see cref="ResiliencePipeline"/> with the
/// Microsoft dependency-injection container for use with Azure Blob Storage.
/// </summary>
public static class PollyAzureBlobServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ResiliencePipeline"/> singleton built from <paramref name="configure"/>,
    /// which can then be injected alongside <see cref="BlobClient"/> or
    /// <see cref="BlobContainerClient"/> and used via <see cref="PollyAzureBlobExtensions.WithPolly(BlobClient, ResiliencePipeline)"/>.
    /// </summary>
    public static IServiceCollection AddPollyAzureBlob(
        this IServiceCollection services,
        Action<ResiliencePipelineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ResiliencePipelineBuilder();
        configure(builder);
        return services.AddPollyAzureBlob(builder.Build());
    }

    /// <summary>
    /// Registers a pre-built <see cref="ResiliencePipeline"/> singleton that can be injected
    /// alongside <see cref="BlobClient"/> or <see cref="BlobContainerClient"/> and used via
    /// <see cref="PollyAzureBlobExtensions.WithPolly(BlobClient, ResiliencePipeline)"/>.
    /// </summary>
    public static IServiceCollection AddPollyAzureBlob(
        this IServiceCollection services,
        ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pipeline);

        services.AddSingleton(pipeline);
        return services;
    }
}
