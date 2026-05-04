using Kontur.BigLibrary.Tests.Core.ApiClients;
using Kontur.BigLibrary.Tests.Core.Helpers.StringGenerator;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[WithAuth("test@mail.com", "Test123456!")]
public class Homework3Tests: TestBase
{
    [Test]
    public async Task BookCreatedByUser1_ShouldBeVisibleToUser2()
    {
        var bookName = StringGenerator.GetRandomString();
        var bookAuthor = StringGenerator.GetRandomString();
        var book = TestData.CreateBook(bookName, bookAuthor);
        
        var user2Email = "user2@test.com";
        var user2Password = "Password123!";
        
        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        var bookModal = await mainPage.AddBookButton.ClickAndOpenModalAsync<AddBookModal>();
        
        await bookModal.FillAndSubmitAsync(book, "Администрирование", TestData.ValidImagePath!);
        
        await LogoutViaTokenAsync();
        
        var loginPage = await Navigation.GoToPageAsync<LoginPage>();
        await loginPage.LoginAsync(user2Email, user2Password);
        
        mainPage = await Navigation.GoToPageAsync<MainPage>();
        
        var bookItem = mainPage.BookList.GetBookItem(bookName);
    
        Assert.That(bookItem, Is.Not.Null);
    }
    
    
    [Test]
    public async Task CreateBookViaUI_ShouldBeFoundInApiSummary()
    {
        var bookName = StringGenerator.GetRandomString();
        var bookAuthor = StringGenerator.GetRandomString();
        var book = TestData.CreateBook(bookName, bookAuthor);
        
        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        var bookModal = await mainPage.AddBookButton.ClickAndOpenModalAsync<AddBookModal>();
    
        await bookModal.FillAndSubmitAsync(book, "Администрирование", TestData.ValidImagePath!);
        
        var token = TestData.GetOrCreateUserAndGetToken("test@mail.com", "Test123456!");
        var apiClient = ServiceProvider.GetRequiredService<BooksApiClient>();
        var response = apiClient.GetAllBooksFromLibrary(token);
    
        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessful, Is.True, "API запрос не удался");
            Assert.That(response.Content, Does.Contain(bookName), "Книга не найдена в API");
        });
    }
}