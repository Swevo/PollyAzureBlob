// <copyright file="ResilientBlobContainerClient.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob;

/// <summary>
/// A decorator around <see cref="BlobContainerClient"/> that executes every container operation
/// inside a Polly v8 <see cref="ResiliencePipeline"/>. Create one via
/// <see cref="PollyAzureBlobExtensions.WithPolly(BlobContainerClient, ResiliencePipeline)"/>.
/// </summary>
public sealed class ResilientBlobContainerClient(BlobContainerClient inner, ResiliencePipeline pipeline)
{
    /// <summary>Gets the URI of the container.</summary>
    public Uri Uri => inner.Uri;

    /// <summary>Gets the name of the container.</summary>
    public string Name => inner.Name;

    /// <summary>
    /// Returns a <see cref="ResilientBlobClient"/> for the blob with the given <paramref name="blobName"/>,
    /// sharing the same resilience pipeline.
    /// </summary>
    public ResilientBlobClient GetBlobClient(string blobName) =>
        inner.GetBlobClient(blobName).WithPolly(pipeline);

    /// <summary>
    /// Creates the container.
    /// </summary>
    public Task<Response<BlobContainerInfo>> CreateAsync(
        PublicAccessType publicAccessType = PublicAccessType.None,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContainerInfo>>(
                inner.CreateAsync(publicAccessType, metadata, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Creates the container if it does not already exist.
    /// </summary>
    public Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(
        PublicAccessType publicAccessType = PublicAccessType.None,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContainerInfo>>(
                inner.CreateIfNotExistsAsync(publicAccessType, metadata, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the container.
    /// </summary>
    public Task<Response> DeleteAsync(
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response>(
                inner.DeleteAsync(conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the container if it exists.
    /// </summary>
    public Task<Response<bool>> DeleteIfExistsAsync(
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<bool>>(
                inner.DeleteIfExistsAsync(conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Returns <c>true</c> if the container exists.
    /// </summary>
    public Task<Response<bool>> ExistsAsync(
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<bool>>(
                inner.ExistsAsync(ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Returns the container's properties.
    /// </summary>
    public Task<Response<BlobContainerProperties>> GetPropertiesAsync(
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContainerProperties>>(
                inner.GetPropertiesAsync(conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Uploads a blob with the given <paramref name="blobName"/> from <paramref name="content"/>.
    /// </summary>
    public Task<Response<BlobContentInfo>> UploadBlobAsync(
        string blobName,
        Stream content,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContentInfo>>(
                inner.UploadBlobAsync(blobName, content, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Uploads a blob with the given <paramref name="blobName"/> from <paramref name="content"/>.
    /// </summary>
    public Task<Response<BlobContentInfo>> UploadBlobAsync(
        string blobName,
        BinaryData content,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContentInfo>>(
                inner.UploadBlobAsync(blobName, content, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the blob with the given <paramref name="blobName"/>.
    /// </summary>
    public Task<Response> DeleteBlobAsync(
        string blobName,
        DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response>(
                inner.DeleteBlobAsync(blobName, snapshotsOption, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the blob with the given <paramref name="blobName"/> if it exists.
    /// </summary>
    public Task<Response<bool>> DeleteBlobIfExistsAsync(
        string blobName,
        DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<bool>>(
                inner.DeleteBlobIfExistsAsync(blobName, snapshotsOption, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Returns a list of blobs in the container matching <paramref name="traits"/> and
    /// <paramref name="states"/>.
    /// </summary>
    public async Task<List<BlobItem>> GetBlobsAsync(
        BlobTraits traits = BlobTraits.None,
        BlobStates states = BlobStates.None,
        string? prefix = null,
        CancellationToken cancellationToken = default) =>
        await pipeline.ExecuteAsync(async ct =>
        {
            var items = new List<BlobItem>();
            await foreach (var item in inner.GetBlobsAsync(traits, states, prefix, ct))
                items.Add(item);
            return items;
        }, cancellationToken);
}
