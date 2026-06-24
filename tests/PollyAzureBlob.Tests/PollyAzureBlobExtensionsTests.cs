// <copyright file="PollyAzureBlobExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob.Tests;

public class PollyAzureBlobExtensionsTests
{
    private readonly ResiliencePipeline _pipeline = ResiliencePipeline.Empty;

    [Fact]
    public void WithPolly_NullBlobClient_ThrowsArgumentNullException()
    {
        BlobClient? client = null;
        Assert.Throws<ArgumentNullException>(() => client!.WithPolly(_pipeline));
    }

    [Fact]
    public void WithPolly_NullBlobClientPipeline_ThrowsArgumentNullException()
    {
        var client = new BlobClient(new Uri("https://account.blob.core.windows.net/container/blob"));
        ResiliencePipeline? pipeline = null;
        Assert.Throws<ArgumentNullException>(() => client.WithPolly(pipeline!));
    }

    [Fact]
    public void WithPolly_ValidBlobClient_ReturnsResilientBlobClient()
    {
        var client = new BlobClient(new Uri("https://account.blob.core.windows.net/container/blob"));
        var result = client.WithPolly(_pipeline);
        Assert.NotNull(result);
        Assert.IsType<ResilientBlobClient>(result);
    }

    [Fact]
    public void WithPolly_NullBlobContainerClient_ThrowsArgumentNullException()
    {
        BlobContainerClient? client = null;
        Assert.Throws<ArgumentNullException>(() => client!.WithPolly(_pipeline));
    }

    [Fact]
    public void WithPolly_NullBlobContainerClientPipeline_ThrowsArgumentNullException()
    {
        var client = new BlobContainerClient(new Uri("https://account.blob.core.windows.net/container"));
        ResiliencePipeline? pipeline = null;
        Assert.Throws<ArgumentNullException>(() => client.WithPolly(pipeline!));
    }

    [Fact]
    public void WithPolly_ValidBlobContainerClient_ReturnsResilientBlobContainerClient()
    {
        var client = new BlobContainerClient(new Uri("https://account.blob.core.windows.net/container"));
        var result = client.WithPolly(_pipeline);
        Assert.NotNull(result);
        Assert.IsType<ResilientBlobContainerClient>(result);
    }

    [Fact]
    public void ResilientBlobClient_ExposesUri()
    {
        var uri = new Uri("https://account.blob.core.windows.net/container/blob");
        var result = new BlobClient(uri).WithPolly(_pipeline);
        Assert.Equal(uri, result.Uri);
    }

    [Fact]
    public void ResilientBlobContainerClient_GetBlobClient_ReturnsResilientBlobClient()
    {
        var client = new BlobContainerClient(new Uri("https://account.blob.core.windows.net/container"));
        var container = client.WithPolly(_pipeline);
        var blob = container.GetBlobClient("myblob.txt");
        Assert.NotNull(blob);
        Assert.IsType<ResilientBlobClient>(blob);
    }
}
