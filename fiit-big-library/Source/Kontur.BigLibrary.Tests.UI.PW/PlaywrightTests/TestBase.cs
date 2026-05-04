using System.Collections.Concurrent;
using Kontur.BigLibrary.Tests.UI.PW.Helpers;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Factories;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework.Interfaces;
using SkbKontur.NUnit.Retries.CiService;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

/// <summary>
/// Базовый класс для всех тестовых классов.
/// Предоставляет общую функциональность для управления зависимостями
/// и взаимодействия с Playwright страницами.
/// </summary>
[RetryOnCiService(3)]
[Parallelizable(ParallelScope.All)]
public abstract class TestBase
{
    public BooksTestDataService TestData => ServiceProvider.GetRequiredService<BooksTestDataService>();
    private IPlaywrightPageGetter PwPageGetter => ServiceProvider.GetRequiredService<IPlaywrightPageGetter>();
    private IPageFactory PageFactory => ServiceProvider.GetRequiredService<IPageFactory>();
    public Navigation Navigation => ServiceProvider.GetRequiredService<Navigation>();
    
    /// <summary>
    /// Корневой провайдер, инициализируемый при создании экземпляра TestBase.
    /// Содержит конфигурацию DI контейнера с зарегистрированными сервисами Playwright.
    /// </summary>
    private readonly IServiceProvider _serviceProvider
        = new ServiceCollection()
            .AddPlaywright()
            .AddTestDataHelpers()
            .BuildServiceProvider();

    /// <summary>
    /// Потокобезопасный словарь для хранения scopes, сопоставленных с идентификаторами тестов.
    /// Гарантирует изоляцию зависимостей между тестами и правильное время жизни объектов.
    /// </summary>
    private static readonly ConcurrentDictionary<string, IServiceScope> ScopeByTestLink = new();

    /// <summary>
    /// Получает экземпляр IServiceProvider для текущего теста.
    /// Для каждого теста создается отдельная область сервисов (scope),
    /// которая автоматически удаляется после завершения теста.
    /// </summary>
    /// <returns>Провайдер для текущего теста</returns>
    protected IServiceProvider ServiceProvider
        => ScopeByTestLink.GetOrAdd(TestContext.CurrentContext.Test.ID, _ => _serviceProvider.CreateScope())
            .ServiceProvider;

    /// <summary>
    /// Выполняется после каждого теста для очистки ресурсов.
    /// Удаляет и освобождает scope, связанную с завершенным тестом.
    /// </summary>
    [TearDown]
    public async Task CloseScope()
    {
        await WriteLogIfTestFailedAsync();
        
        if (ScopeByTestLink.TryRemove(TestContext.CurrentContext.Test.ID, out var scope))
        {
            if (scope is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                scope.Dispose();
        }
    }

    private async Task WriteLogIfTestFailedAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed) return;
        
        await TakeScreenshotOnFailureAsync();
        await LogVideoIfTestFailedAsync();
    }
    
    private async Task TakeScreenshotOnFailureAsync()
    {
        var pwPage = await PwPageGetter.GetAsync();
        
        var testName = TestContext.CurrentContext.Test.Name;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        var screenshotsDir = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "screenshots");

        Directory.CreateDirectory(screenshotsDir);

        var filePath = Path.Combine(
            screenshotsDir,
            $"{testName}_{timestamp}.png");

        await pwPage.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true
        });

        TestContext.WriteLine($"Screenshot: {filePath}");
    }
    
    private async Task LogVideoIfTestFailedAsync()
    {
        var pwPage = await PwPageGetter.GetAsync();
        
        var video = pwPage.Video;
        if (video == null) return;

        var path = await video.PathAsync();
        TestContext.WriteLine($"Video: {path}");
    }
    
    public async Task LogoutViaTokenAsync()
    {
        var page = await PwPageGetter.GetAsync(); 
    
        await page.EvaluateAsync("() => localStorage.removeItem('jwtToken')");
        await Navigation.GoToPageAsync<LoginPage>();
    }
}