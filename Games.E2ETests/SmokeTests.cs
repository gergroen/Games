using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Games.E2ETests;

[TestClass]
public class SmokeTests
{
    private const string BaseUrl = "http://localhost:5080";
    private static readonly HttpClient httpClient = new();

    [TestMethod]
    public async Task Application_ShouldRespond()
    {
        try
        {
            var response = await httpClient.GetAsync(BaseUrl);
            Assert.IsTrue(response.IsSuccessStatusCode, 
                $"Application should respond successfully. Status: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Failed to connect to application at {BaseUrl}: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task Application_ShouldServeIndexPage()
    {
        try
        {
            var response = await httpClient.GetAsync(BaseUrl);
            var content = await response.Content.ReadAsStringAsync();
            
            Assert.IsTrue(response.IsSuccessStatusCode, 
                $"Index page should load successfully. Status: {response.StatusCode}");
            Assert.IsTrue(content.Contains("Games"), 
                "Index page should contain the application title 'Games'");
            Assert.IsTrue(content.Contains("blazor"), 
                "Index page should contain Blazor framework references");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Failed to load index page: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task TanksPage_ShouldBeAccessible()
    {
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/tanks");
            Assert.IsTrue(response.IsSuccessStatusCode, 
                $"Tanks page should be accessible. Status: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Failed to access tanks page: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task StaticAssets_ShouldBeAccessible()
    {
        try
        {
            // Test CSS files
            var cssResponse = await httpClient.GetAsync($"{BaseUrl}/css/app.css");
            Assert.IsTrue(cssResponse.IsSuccessStatusCode, 
                $"CSS assets should be accessible. Status: {cssResponse.StatusCode}");

            // Test JavaScript files
            var jsResponse = await httpClient.GetAsync($"{BaseUrl}/_framework/blazor.webassembly.js");
            Assert.IsTrue(jsResponse.IsSuccessStatusCode, 
                $"Blazor JS should be accessible. Status: {jsResponse.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Failed to access static assets: {ex.Message}");
        }
    }
}