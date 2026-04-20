using FluentAssertions;
using Microsoft.Playwright;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

public class PageTests
{
    private IPage _page;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions { Headless = false };
        var browser = await playwright.Chromium.LaunchAsync(launchOptions);
        var context = await browser.NewContextAsync();
        _page = await context.NewPageAsync();
        
        await _page.GotoAsync("http://localhost:5000/");
        await Login();
    }

    private async Task Login()
    {
        var email = _page.Locator("input#email[type=email]");
        await email.FillAsync("test@mail.com");
        
        var password = _page.Locator("input#password[type=password]");
        await password.FillAsync("Test123456!");
        
        var registrationButton = _page.Locator("button[type=submit]");
        await registrationButton.ClickAsync();
    }
    
    [Test]
    public async Task BooksListPage_Success()
    {
        var searchString = _page.GetByPlaceholder("найти по названию, автору или рубрике");
        await searchString.FillAsync("Оптимизация игр");
        await searchString.PressAsync("Enter");
        
        var allBooks = _page.Locator("a[data-tid^='bookItem']");
        
        await Assertions.Expect(allBooks).ToHaveCountAsync(1);
        await Assertions.Expect(allBooks).ToHaveAttributeAsync("data-tid", "bookItem-Optimizatsiya_igr_v_Unity_5");
    }

    [Test]
    public async Task SecondAvailableBook_Success()
    {
        var allBooks = _page.Locator("a[data-tid^='bookItem']");
        var secondBook = allBooks.Nth(1);
        
        await Assertions.Expect(secondBook).ToHaveAttributeAsync("data-tid", "bookItem-Rukovodstvo_k_svodu_znaniy_po_upravleniyu_proektami");
    }

    [Test]
    public async Task SecondAvailableBook_Fail()
    {
        var rubricMenu = _page.Locator("input[type='checkbox']");
        await rubricMenu.ClickAsync();
        
        var rubricLinks = _page.Locator("a[href^='/rubric/']");
        await Assertions.Expect(rubricLinks).ToHaveCountAsync(35); 
    }

    [Test]
    public async Task GetBook_Success()
    {
        var allBooks = _page.Locator("a[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await allBooks.ClickAsync();
        
        await _page
            .Locator("button:has-text('Взять книгу')")
            .ClickAsync();

        var stateLabel = _page.Locator("div[data-tid='StateLabelBusy']");
        await Assertions.Expect(stateLabel).ToHaveTextAsync("ЗАНЯТА");
        
        await _page
            .Locator("button:has-text('Вернуть книгу')")
            .ClickAsync();
    }

    [Test]
    public async Task ReturnToAllBooks_Success()
    {
        var allBooks = _page.Locator("a[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await allBooks.ClickAsync();
        
        await _page.Locator("a:has-text('Все книги')")
            .ClickAsync(new() { Force = true });
        
        var mainTitle = _page.Locator("a[data-tid='titleLink']");
        var mainTitleText = await mainTitle.TextContentAsync();

        mainTitleText.Should().Be("Библиотека");
    }

    [Test]
    public async Task ModalWindowTitle_Success()
    {
        await _page.Locator("button[data-tid='book-add']")
            .ClickAsync();
        
        var modal = _page.Locator("h5.modal-title");
        await Assertions.Expect(modal).ToBeVisibleAsync();
    }
    
    [Test]
    public async Task ModalWindowFieldName_Success()
    {
        await _page.Locator("button[data-tid='book-add']")
            .ClickAsync();
        
        var fieldName = _page.Locator("label[for='bookName']");
        await Assertions.Expect(fieldName).ToBeVisibleAsync();
        
        var field = _page.Locator("input[id='bookName']");
        await Assertions.Expect(field).ToBeVisibleAsync();
    }

    [Test]
    public async Task ModalWindowFieldValue_Success()
    {
        // 1. Считаем сколько книг СЕЙЧАС (до добавления)
        var allBooksLocator = _page.Locator("a[data-tid^='bookItem']");
        var countBefore = await allBooksLocator.CountAsync();
    
        await _page.Locator("button[data-tid='book-add']").ClickAsync();
    
        // ... заполнение полей ...
        var nameField = _page.Locator("input[id='bookName']");
        await _page.Locator("input[id='bookAuthor']").FillAsync("tester");
        await _page.Locator("textarea[id='bookDescription']").FillAsync("description");
        await _page.Locator("#bookImageFile").SetInputFilesAsync(Path.Combine(AppContext.BaseDirectory, "../../../Helpers/Files/image.jpg"));
        
        await _page.Locator("button[data-tid='add-book-button']").ClickAsync();
    
        // 2. Используем Expect с автоматическим ожиданием обновления списка
        // Мы ожидаем, что количество станет countBefore + 1
        await Assertions.Expect(allBooksLocator).ToHaveCountAsync(countBefore + 1);
    }
}