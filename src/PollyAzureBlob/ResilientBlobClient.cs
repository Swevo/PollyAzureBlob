// <copyright file="ResilientBlobClient.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob;

/// <summary>
/// A decorator around <see cref="BlobClient"/> that executes every blob operation inside a
/// Polly v8 <see cref="ResiliencePipeline"/>. Create one via
/// <see cref="PollyAzureBlobExtensions.WithPolly(BlobClient, ResiliencePipeline)"/>.
/// </summary>
public sealed class ResilientBlobClient(BlobClient inner, ResiliencePipeline pipeline)
{
    /// <summary>Gets the URI of the blob.</summary>
    public Uri Uri => inner.Uri;

    /// <summary>Gets the name of the blob.</summary>
    public string Name => inner.Name;

    /// <summary>Gets the name of the container.</summary>
    public string BlobContainerName => inner.BlobContainerName;

    /// <summary>
    /// Uploads <paramref name="content"/> to the blob, optionally overwriting an existing blob.
    /// </summary>
    public Task<Response<BlobContentInfo>> UploadAsync(
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContentInfo>>(
                inner.UploadAsync(content, overwrite, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Uploads <paramref name="content"/> to the blob, optionally overwriting an existing blob.
    /// </summary>
    public Task<Response<BlobContentInfo>> UploadAsync(
        BinaryData content,
        bool overwrite = false,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobContentInfo>>(
                inner.UploadAsync(content, overwrite, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Downloads the blob content.
    /// </summary>
    public Task<Response<BlobDownloadResult>> DownloadContentAsync(
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobDownloadResult>>(
                inner.DownloadContentAsync(ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Downloads the blob content to <paramref name="destination"/>.
    /// </summary>
    public Task<Response> DownloadToAsync(
        Stream destination,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response>(
                inner.DownloadToAsync(destination, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the blob.
    /// </summary>
    public Task<Response> DeleteAsync(
        DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response>(
                inner.DeleteAsync(snapshotsOption, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Deletes the blob if it exists.
    /// </summary>
    public Task<Response<bool>> DeleteIfExistsAsync(
        DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<bool>>(
                inner.DeleteIfExistsAsync(snapshotsOption, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Returns <c>true</c> if the blob exists.
    /// </summary>
    public Task<Response<bool>> ExistsAsync(
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<bool>>(
                inner.ExistsAsync(ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Returns the blob's properties.
    /// </summary>
    public Task<Response<BlobProperties>> GetPropertiesAsync(
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobProperties>>(
                inner.GetPropertiesAsync(conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Sets the blob's HTTP headers.
    /// </summary>
    public Task<Response<BlobInfo>> SetHttpHeadersAsync(
        BlobHttpHeaders? httpHeaders = null,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobInfo>>(
                inner.SetHttpHeadersAsync(httpHeaders, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Sets the blob's metadata.
    /// </summary>
    public Task<Response<BlobInfo>> SetMetadataAsync(
        IDictionary<string, string> metadata,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobInfo>>(
                inner.SetMetadataAsync(metadata, conditions, ct)),
            cancellationToken).AsTask();

    /// <summary>
    /// Creates a snapshot of the blob.
    /// </summary>
    public Task<Response<BlobSnapshotInfo>> CreateSnapshotAsync(
        IDictionary<string, string>? metadata = null,
        BlobRequestConditions? conditions = null,
        CancellationToken cancellationToken = default) =>
        pipeline.ExecuteAsync(
            ct => new ValueTask<Response<BlobSnapshotInfo>>(
                inner.CreateSnapshotAsync(metadata, conditions, ct)),
            cancellationToken).AsTask();
}
