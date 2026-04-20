using System.Net;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Tests.Core.ApiClients;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;

namespace Tests.ApiSystem;

public class BooksApiTests : BooksApiTestBase
{
    [OneTimeSetUp]
    public void SetUp()
    {
        authApiClient = new AuthApiClient();
        booksApiClient = new BooksApiClient();
    }

    [Test]
    public void AddNewBook_Correct_Success()
    {
        // Arrange
        var (_, _, token) = CreateUser();
        var book = CreateBook(token);
        
        // Act
        var response = booksApiClient.AddBookToLibrary(book, token);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        var savedBook = JsonConvert.DeserializeObject<Book>(response.Content!);
        savedBook.Should().BeEquivalentTo(book);
    }
    
    [Test]
    public void EnqueueBook_BookIsFree_Success()
    {
        // Arrange
        var (email, _, token) = CreateUser();
        var bookId = CreateBook(token).Id.ToString()!;

        // Act
        var response = booksApiClient.EnqueueBook(bookId, email, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Книга свободна.");
        
        var queue = GetReadersInQueue(bookId, token);
        queue.Should().BeEmpty();
    }
    
    [Test]
    public void EnqueueBook_BookIsCheckedOutByAnotherUser_Success()
    {
        // Arrange
        var (email1, _, token1) = CreateUser();
        var (email2, _, token2) = CreateUser();
        var bookId = CreateBook(token1).Id.ToString()!;
        
        booksApiClient.CheckoutBook(bookId, email1, token1);
        
        // Act
        var response = booksApiClient.EnqueueBook(bookId, email2, token2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Вы встали в очередь.");
        
        var queue = GetReadersInQueue(bookId, token2);
        queue.Should().Contain(r => r.UserName == email2);
    }
    
    [Test]
    public void EnqueueBook_CurrentUserAlreadyInQueue_ShouldNotDuplicate()
    {
        // Arrange
        var (email1, _, token1) = CreateUser();
        var (email2, _, token2) = CreateUser();
        var bookId = CreateBook(token1).Id.ToString()!;
        
        booksApiClient.CheckoutBook(bookId, email1, token1);
        booksApiClient.EnqueueBook(bookId, email2, token2);

        // Act
        var response = booksApiClient.EnqueueBook(bookId, email2, token2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Вы уже стоите в очереди.");
        
        var queue = GetReadersInQueue(bookId, token2);
        queue!.Count(r => r.UserName == email2).Should().Be(1);
    }

    [Test]
    public void EnqueueBook_BookIsCheckedOutByCurrentUser_ShouldNotAddToQueue()
    {
        // Arrange
        var (email, _, token) = CreateUser();
        var bookId = CreateBook(token).Id.ToString()!;
        booksApiClient.CheckoutBook(bookId, email, token);
        
        // Act
        var response = booksApiClient.EnqueueBook(bookId, email, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Вы уже взяли эту книгу.");
        
        var queue = GetReadersInQueue(bookId, token);
        queue.Should().BeEmpty();
    }
    
    [Test]
    public void CheckoutBook_BookIsFreeNoQueue_Success()
    {
        // Arrange
        var (email, _, token) = CreateUser();
        var bookId = CreateBook(token).Id.ToString()!;

        // Act
        var response = booksApiClient.CheckoutBook(bookId, email, token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Вы взяли книгу.");
        
        var readers = GetBookReaders(bookId, token);
        readers.Should().Contain(r => r.UserName == email);
    }

    [Test]
    public void CheckoutBook_BookIsCheckedOutByAnotherUser_ShouldFail()
    {
        // Arrange
        var (email1, _, token1) = CreateUser();
        var (email2, _, token2) = CreateUser();
        var bookId = CreateBook(token1).Id.ToString()!;
        
        booksApiClient.CheckoutBook(bookId, email1, token1);
        
        // Act
        var response = booksApiClient.CheckoutBook(bookId, email2, token2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Книга занята.");
        
        var readers = GetBookReaders(bookId, token2);
        readers.Should().NotContain(r => r.UserName == email2);
    }

    [Test]
    public void CheckoutBook_BookIsFreeFirstInQueueIsAnotherUser_ShouldGiveToFirstInQueue()
    {
        // Arrange
        var (email1, _, token1) = CreateUser();
        var (email2, _, token2) = CreateUser();
        var (email3, _, token3) = CreateUser();
        var bookId = CreateBook(token1).Id.ToString()!;
        
        booksApiClient.CheckoutBook(bookId, email1, token1);
        booksApiClient.EnqueueBook(bookId, email2, token2);
        booksApiClient.ReturnBook(bookId, email1, token1);
        // Act
        var response = booksApiClient.CheckoutBook(bookId, email3, token3);
        var response2 = booksApiClient.CheckoutBook(bookId, email2, token2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Не ваша очередь.");
        var readers = GetBookReaders(bookId, token1);
        readers.Should().NotContain(r => r.UserName == email3);
        
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response2, "Вы взяли книгу.");
        readers = GetBookReaders(bookId, token2);
        readers.Should().Contain(r => r.UserName == email2);
    }

    [Test]
    public void CheckoutBook_BookReturnedCurrentUserIsFirstInQueue_Success()
    {
        // Arrange
        var (email1, _, token1) = CreateUser();
        var (email2, _, token2) = CreateUser();
        var bookId = CreateBook(token1).Id.ToString()!;
        
        booksApiClient.CheckoutBook(bookId, email1, token1);
        booksApiClient.EnqueueBook(bookId, email2, token2);
        booksApiClient.ReturnBook(bookId, email1, token1);

        // Act
        var response = booksApiClient.CheckoutBook(bookId, email2, token2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AsserResponseByMessage(response, "Вы взяли книгу.");
        
        var readers = GetBookReaders(bookId, token2);
        readers.Should().Contain(r => r.UserName == email2);
    }

    private static void AsserResponseByMessage(RestResponse response, string msg)
    {
        response.Content.Should().NotBeNull();
        var answer = JsonConvert.DeserializeAnonymousType(response.Content!, new { message = "" });
        answer!.message.Should().BeEquivalentTo(msg);
    }
}