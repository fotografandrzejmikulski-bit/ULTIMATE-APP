using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UltimateApp.Application.Models;
using UltimateApp.Infrastructure.Models;
using Xunit;

namespace UltimateApp.Tests;

public class ModelManagerTests
{
    private static string TempDir() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    // -----------------------------------------------------------------------
    // SHA256 validation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ValidateSha256_NullExpected_ReturnsTrue()
    {
        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "any content");
        try
        {
            Assert.True(await ModelManager.ValidateSha256Async(file, null));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ValidateSha256_EmptyExpected_ReturnsTrue()
    {
        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "any content");
        try
        {
            Assert.True(await ModelManager.ValidateSha256Async(file, ""));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ValidateSha256_CorrectHash_ReturnsTrue()
    {
        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "hello");
        // SHA256 of UTF-8 "hello"
        const string expected = "2CF24DBA5FB0A30E26E83B2AC5B9E29E1B161E5C1FA7425E73043362938B9824";
        try
        {
            Assert.True(await ModelManager.ValidateSha256Async(file, expected));
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task ValidateSha256_WrongHash_ReturnsFalse()
    {
        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "hello");
        const string wrong = "0000000000000000000000000000000000000000000000000000000000000000";
        try
        {
            Assert.False(await ModelManager.ValidateSha256Async(file, wrong));
        }
        finally { File.Delete(file); }
    }

    // -----------------------------------------------------------------------
    // GetStatus – not downloaded
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStatus_WhenFilesNotPresent_ReturnsBothNotDownloaded()
    {
        var cacheDir = TempDir();
        var manager = BuildManager(new HttpClient(), cacheDir);
        var (llm, spec) = await manager.GetStatusAsync();
        Assert.Equal(ModelDownloadState.NotDownloaded, llm.State);
        Assert.Equal(ModelDownloadState.NotDownloaded, spec.State);
        Directory.Delete(cacheDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Parallel download with mock HttpClient
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DownloadModels_MockHttp_BothFilesCreated()
    {
        var cacheDir = TempDir();
        var content = "fake gguf data"u8.ToArray();
        var handler = new MockHttpMessageHandler(content);
        var http = new HttpClient(handler);

        var manager = BuildManager(http, cacheDir);
        await manager.DownloadModelsAsync(null, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(cacheDir, "llm.gguf")));
        Assert.True(File.Exists(Path.Combine(cacheDir, "specialist.gguf")));

        Directory.Delete(cacheDir, recursive: true);
    }

    [Fact]
    public async Task DownloadModels_MockHttp_StatusIsDownloadedAfter()
    {
        var cacheDir = TempDir();
        var content = "fake model"u8.ToArray();
        var handler = new MockHttpMessageHandler(content);
        var http = new HttpClient(handler);

        var manager = BuildManager(http, cacheDir);
        await manager.DownloadModelsAsync();

        var (llm, spec) = await manager.GetStatusAsync();
        Assert.Equal(ModelDownloadState.Downloaded, llm.State);
        Assert.Equal(ModelDownloadState.Downloaded, spec.State);

        Directory.Delete(cacheDir, recursive: true);
    }

    [Fact]
    public async Task DownloadModels_SkipsIfAlreadyPresent_NoCacheExpectedHash()
    {
        var cacheDir = TempDir();
        Directory.CreateDirectory(cacheDir);
        await File.WriteAllBytesAsync(Path.Combine(cacheDir, "llm.gguf"), "existing"u8.ToArray());
        await File.WriteAllBytesAsync(Path.Combine(cacheDir, "specialist.gguf"), "existing"u8.ToArray());

        var calls = 0;
        var handler = new MockHttpMessageHandler("data"u8.ToArray(), () => calls++);
        var http = new HttpClient(handler);

        var manager = BuildManager(http, cacheDir);
        await manager.DownloadModelsAsync();

        // No download should happen because files exist and no hash check required
        Assert.Equal(0, calls);

        Directory.Delete(cacheDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ModelManager BuildManager(HttpClient http, string cacheDir)
    {
        var llm = new ModelInfo("LLM", "http://test/llm.gguf", "llm.gguf");
        var spec = new ModelInfo("Specialist", "http://test/specialist.gguf", "specialist.gguf");
        return new ModelManager(http, NullLogger<ModelManager>.Instance, llm, spec, cacheDir);
    }
}

/// <summary>Prosty mock HttpMessageHandler zwracający stałą treść.</summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly byte[] _content;
    private readonly Action? _onRequest;

    public MockHttpMessageHandler(byte[] content, Action? onRequest = null)
    {
        _content = content;
        _onRequest = onRequest;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _onRequest?.Invoke();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(_content)
        };
        return Task.FromResult(response);
    }
}
