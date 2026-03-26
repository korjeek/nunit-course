using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Events;
using Kontur.BigLibrary.Service.Exceptions;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Service.Services.EventService.Repository;
using Kontur.BigLibrary.Service.Services.ImageService.Repository;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class BookServiceMockTest
{
    private static readonly IServiceProvider container = new ContainerForMockTests().Build();
    
    private readonly IBookService _bookService = container.GetRequiredService<IBookService>();
    private readonly IBookRepository _bookRepositoryMock = container.GetRequiredService<IBookRepository>();
    private readonly IImageRepository _imageRepositoryMock = container.GetRequiredService<IImageRepository>();
    private readonly IEventRepository _eventRepositoryMock = container.GetRequiredService<IEventRepository>();
    
    [SetUp]
    public void Setup()
    {
        _bookRepositoryMock.ClearReceivedCalls();
        _imageRepositoryMock.ClearReceivedCalls();
        _eventRepositoryMock.ClearReceivedCalls();
        
        _bookRepositoryMock.GetRubricAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Rubric>(null));
        _imageRepositoryMock.GetAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Image>(null));
    }
    
    [Test]
    public async Task SaveBookAsync_ShouldSaveBookAndPublishEvent_WhenValidBook()
    {
        const int rubricId = 1;
        const int imageId = 100;
        const int expectedBookId = 500;
        const string expectedSynonym = "Database_Systems_The_Complete_Book";
        
        var rubric = new Rubric { Id = rubricId, Name = "Test Rubric" };
        var image = new Image { Id = imageId, Data = [] };
        
        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D.Ullman, Jennifer Widom",
            RubricId = rubricId,
            ImageId = imageId,
            Description = "New_book"
        };
        
        var savedBook = new Book
        {
            Id = expectedBookId,
            Name = book.Name,
            Author = book.Author,
            RubricId = book.RubricId,
            ImageId = book.ImageId,
            Description = book.Description
        };
        
        _bookRepositoryMock
            .GetRubricAsync(rubricId, Arg.Any<CancellationToken>())
            .Returns(rubric);
        
        _imageRepositoryMock
            .GetAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(image);
        
        _bookRepositoryMock
            .SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>())
            .Returns(savedBook);
        
        var result = await _bookService.SaveBookAsync(book, CancellationToken.None);
        
        await _bookRepositoryMock
            .Received(1)
            .SaveBookAsync(Arg.Is<Book>(b => 
                b.Name == book.Name &&
                b.Author == book.Author &&
                b.RubricId == book.RubricId &&
                b.ImageId == book.ImageId &&
                b.Description == book.Description), 
            Arg.Any<CancellationToken>());
        
        await _bookRepositoryMock
            .Received(1)
            .SaveBookIndexAsync(
                expectedBookId,
                Arg.Is<string>(fts => !string.IsNullOrEmpty(fts)),
                Arg.Is<string>(synonym => synonym == expectedSynonym),
                Arg.Any<CancellationToken>());
        
        await _eventRepositoryMock
            .Received(1)
            .SaveAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
        
        using (new AssertionScope())
        {
            result.Should().BeEquivalentTo(savedBook, options => options
                .ComparingByMembers<Book>());
        }
    }
    
    [Test]
    public async Task SaveBookAsync_ShouldThrowValidationException_WhenRubricNotExists()
    {
        const int imageId = 100;
        const int nonExistentRubricId = -1;
        
        var image = new Image { Id = imageId, Data = [] };
        
        var book = new Book
        {
            Name = "Test Book",
            Author = "Test Author",
            RubricId = nonExistentRubricId,
            ImageId = imageId,
            Description = "Test Description"
        };
        
        _bookRepositoryMock
            .GetRubricAsync(nonExistentRubricId, Arg.Any<CancellationToken>())
            .Returns((Rubric)null);
        
        _imageRepositoryMock
            .GetAsync(imageId, Arg.Any<CancellationToken>())
            .Returns(image);
        
        await _bookService
            .Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая рубрика.");
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookIndexAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _eventRepositoryMock
            .Received(0)
            .SaveAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
    }
    
    [Test]
    public async Task SaveBookAsync_ShouldThrowValidationException_WhenImageNotExists()
    {
        const int rubricId = 1;
        const int nonExistentImageId = -1;
        
        var rubric = new Rubric { Id = rubricId, Name = "Test Rubric" };
        
        var book = new Book
        {
            Name = "Test Book",
            Author = "Test Author",
            RubricId = rubricId,
            ImageId = nonExistentImageId,
            Description = "Test Description"
        };
        
        _bookRepositoryMock
            .GetRubricAsync(rubricId, Arg.Any<CancellationToken>())
            .Returns(rubric);
        
        _imageRepositoryMock
            .GetAsync(nonExistentImageId, Arg.Any<CancellationToken>())
            .Returns((Image)null);
        
        await _bookService
            .Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая картинка.");
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookIndexAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        await _eventRepositoryMock
            .Received(0)
            .SaveAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
    }
}