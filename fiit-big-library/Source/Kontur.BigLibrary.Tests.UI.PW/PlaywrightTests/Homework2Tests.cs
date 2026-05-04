using FluentAssertions;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[NonParallelizable]
[WithAuth("test@mail.com", "Test123456!")]
public class Homework2Tests: TestBase
{
    [Test]
    public async Task BookFilter_WhenOnlyAvailableSelected_ThenShowsOnlyFreeBooks()
    {
        var page = await Navigation.GoToPageAsync<MainPage>();
        var startCount = await page.BookList.GetBooksCountAsync();
        
        var book = page.BookList.GetBookItem("Optimizatsiya_igr_v_Unity_5");
        var bookPage = await book.ClickAndOpenPageAsync<BookPage>();
        await bookPage.CheckoutBook.ClickAsync();
        page = await bookPage.AllBooks.ClickAndOpenPageAsync<MainPage>();
        
        await page.FreeOnlyFilter.ClickAsync();
        var finalCount = await page.BookList.GetBooksCountAsync();
        
        startCount.Should().Be(finalCount + 1);
        
        book = page.BookList.GetBookItem("Optimizatsiya_igr_v_Unity_5");
        bookPage = await book.ClickAndOpenPageAsync<BookPage>();
        await bookPage.ReturnBook.ClickAsync();
    }
    
    [Test]
    public async Task ViewModeToggle_WhenSwitched_ChangesLayout()
    {
        var page = await Navigation.GoToPageAsync<MainPage>();
        await page.ChangeView.ClickAsync();

        await page.BooksTable.WaitVisibleAsync();
    }
    
    [Test]
    public async Task BookDetails_WhenOpened_DisplaysValidInfo()
    {
        var page = await Navigation.GoToPageAsync<MainPage>();
        var book = page.BookList.GetBookItem("Optimizatsiya_igr_v_Unity_5");
        var bookPage = await book.ClickAndOpenPageAsync<BookPage>();
        
        await bookPage.BookName.CheckTextAsync("Оптимизация игр в Unity 5");
        await bookPage.BookAuthor.CheckTextAsync("Крис Дикинсон");
    }
    
    [Test]
    public async Task BookStatusChange_WhenToggled_ThenUpdatesCorrectly()
    {
        var page = await Navigation.GoToPageAsync<MainPage>();
        var book = page.BookList.GetBookItem("Optimizatsiya_igr_v_Unity_5");
        var bookPage = await book.ClickAndOpenPageAsync<BookPage>();

        await bookPage.FreeState.WaitVisibleAsync();
        
        await bookPage.CheckoutBook.ClickAsync();
        await bookPage.BusyState.WaitVisibleAsync();
        
        await bookPage.ReturnBook.ClickAsync();
        await bookPage.FreeState.WaitVisibleAsync();
    }
}