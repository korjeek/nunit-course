using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Tests.Core.ApiClients;
using Kontur.BigLibrary.Tests.Core.Helpers.StringGenerator;
using Newtonsoft.Json;

namespace Kontur.BigLibrary.Tests.UI.PW.Helpers;

public class BooksTestDataService
{
    private readonly AuthApiClient _auth;
    private readonly BooksApiClient _booksApi;

    public BooksTestDataService(AuthApiClient auth, BooksApiClient booksApi)
    {
        _auth = auth;
        _booksApi = booksApi;
    }
    public string? ValidImagePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Helpers", "Files", "image.jpg");
    public string InvalidImagePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Helpers", "Files", "invalidImage.jpg");

    public string CreateUserAndGetToken(string? email, string? password)
    {
        var result = _auth.RegisterUser(email, password);

        if (!result.IsSuccessful)
        {
            throw new Exception(
                $"Ошибка при создании пользователя {email}\n" +
                $"{result.Content}\n{result.ErrorMessage}");
        }

        return JsonConvert.DeserializeObject<AuthResult>(result.Content!)!.Token;
    }
    
    public string GetUserToken(string? email, string? password)
    {
        var result = _auth.LoginUser(email, password);

        if (!result.IsSuccessful)
        {
            throw new Exception(
                $"Ошибка при логине пользователя {email}\n" +
                $"{result.Content}");
        }

        return JsonConvert.DeserializeObject<AuthResult>(result.Content!)!.Token;
    }

    public string GetOrCreateUserAndGetToken(string? email, string? password)
    {
        var result = _auth.LoginUser(email, password);
        if (!result.IsSuccessful)
        {
            return CreateUserAndGetToken(email, password);
        }
        
        return JsonConvert.DeserializeObject<AuthResult>(result.Content!)!.Token;
    }
    
    public Book CreateBook(string bookName, string author)
    {
        var token = CreateUserAndGetToken(StringGenerator.GetEmail(), StringGenerator.GetValidPassword());
        var imageId = _booksApi.CreateImage(ValidImagePath, token).Content;
        var book = new Book
        {
            Name = bookName,
            Description = $"DefaultDescription of {bookName}",
            Count = 1,
            Price = "123",
            Author = author,
            ImageId = int.Parse(imageId),
            RubricId = 1,
            IsDeleted = false,
        };
        var clientResult = _booksApi.AddBookToLibrary(book, token);
        return JsonConvert.DeserializeObject<Book>(clientResult.Content!);
    }

    public void SetBookAsBusy(int? bookId)
    {
        var userEmail = StringGenerator.GetEmail();
        var token = CreateUserAndGetToken(userEmail, StringGenerator.GetValidPassword());

        _booksApi.CheckoutBook(bookId.ToString(), userEmail, token);
    }
    
    public void SetBookAsBusyByUser(int? bookId, string? email, string? password)
    {
        var token = GetUserToken(email, password);
        _booksApi.CheckoutBook(bookId.ToString(), email, token);
    }

    public BookSummary[] GetAllBooks(string? email, string? password)
    {
        var token = CreateUserAndGetToken(email ?? StringGenerator.GetEmail(), password ?? StringGenerator.GetValidPassword());
        var clientResult = _booksApi.GetAllBooksFromLibrary(token);
        return JsonConvert.DeserializeObject<BookSummary[]>(clientResult.Content!);
    }
}