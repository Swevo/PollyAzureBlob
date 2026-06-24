// <copyright file="PollyAzureBlobServiceCollectionExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

namespace PollyAzureBlob.Tests;

public class PollyAzureBlobServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPollyAzureBlob_WithBuilder_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        services.AddPollyAzureBlob(pipeline => pipeline.AddTimeout(TimeSpan.FromSeconds(5)));

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetService<ResiliencePipeline>();

        Assert.NotNull(pipeline);
    }

    [Fact]
    public void AddPollyAzureBlob_WithPrebuiltPipeline_RegistersResiliencePipelineSingleton()
    {
        var services = new ServiceCollection();
        var prebuilt = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        services.AddPollyAzureBlob(prebuilt);

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetService<ResiliencePipeline>();

        Assert.Same(prebuilt, pipeline);
    }

    [Fact]
    public void AddPollyAzureBlob_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Assert.Throws<ArgumentNullException>(() => services!.AddPollyAzureBlob(_ => { }));
    }

    [Fact]
    public void AddPollyAzureBlob_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Action<ResiliencePipelineBuilder>? configure = null;
        Assert.Throws<ArgumentNullException>(() => services.AddPollyAzureBlob(configure!));
    }
}
