using FluentAssertions;
using Microsoft.Playwright;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[NonParallelizable]
[WithAuth("test@mail.com", "Test123456!")]
public class PageTests : TestBase
{
    private IPage _page;
    
    [SetUp]
    public async Task SetUp()
    {
        _page = await Navigation.GoToUrlAsync("http://localhost:5000/");
    }

    [Test]
    public async Task BooksList_page_Success()
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
    public async Task BookListToggle_Success()
    {
        var toggleLabel = _page.Locator("label:has(input[type='checkbox'])");
        await toggleLabel.ClickAsync();
        
        var toggleCheckbox = _page.Locator("input[type='checkbox']");
        await Assertions.Expect(toggleCheckbox).ToBeCheckedAsync();
        
        var takenBookLabels = _page.Locator("[data-tid^='bookItem'] [data-tid='StateLabelFree']", new()
        {
            HasText = "Занята"
        });
        
        await Assertions.Expect(takenBookLabels).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GetBook_Success()
    {
        var allBooks = _page.Locator("a[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await allBooks.ClickAsync();
        
        await _page.Locator("button:has-text('Взять книгу')").ClickAsync();

        var stateLabel = _page.Locator("div[data-tid='StateLabelBusy']");
        await Assertions.Expect(stateLabel).ToHaveTextAsync("ЗАНЯТА");
        
        await _page.Locator("button:has-text('Вернуть книгу')").ClickAsync();
    }

    [Test]
    public async Task ReturnToAllBooks_Success()
    {
        var allBooks = _page.Locator("a[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await allBooks.ClickAsync();
        
        await _page.Locator("a:has-text('Все книги')").ClickAsync(new() { Force = true });
        
        var mainTitle = _page.Locator("a[data-tid='titleLink']");
        var mainTitleText = await mainTitle.TextContentAsync();

        mainTitleText.Should().Be("Библиотека");
    }

    [Test]
    public async Task ModalWindowTitle_Success()
    {
        await _page.Locator("button[data-tid='book-add']").ClickAsync();
        
        var modal = _page.Locator("h5.modal-title");
        await Assertions.Expect(modal).ToBeVisibleAsync();
    }
    
    [Test]
    public async Task ModalWindowFieldName_Success()
    {
        await _page.Locator("button[data-tid='book-add']").ClickAsync();
        
        var fieldName = _page.Locator("label[for='bookName']");
        await Assertions.Expect(fieldName).ToBeVisibleAsync();
        
        var field = _page.Locator("input[id='bookName']");
        await Assertions.Expect(field).ToBeVisibleAsync();
    }

    [Test]
    public async Task ModalWindowFieldValue_Success()
    {
        const string uniqueName = "test-book";
        await _page.Locator("button[data-tid='book-add']").ClickAsync();
        
        await _page.Locator("input[id='bookName']").FillAsync($"{uniqueName}");
        await _page.Locator("input[id='bookAuthor']").FillAsync("tester");
        await _page.Locator("textarea[id='bookDescription']").FillAsync("description");
        
        await _page.Locator("#bookImageFile")
            .SetInputFilesAsync(TestData.ValidImagePath!);
        
        var newBook = _page.Locator($"a[data-tid^='bookItem']:has-text('{uniqueName}')");
        await Assertions.Expect(newBook).ToBeVisibleAsync();
    }
}