using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Events;
using Kontur.BigLibrary.Service.Exceptions;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Service.Services.EventService;
using Kontur.BigLibrary.Service.Services.ImageService;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class BookServiceMockTest
{
    private IBookService _bookService;
    private IBookRepository _bookRepositoryMock;
    private IImageService _imageServiceMock;
    private IEventService _eventServiceMock;
    private ISynonymMaker _synonymMakerMock;
    
    [SetUp]
    public void Setup()
    {
        _bookRepositoryMock = Substitute.For<IBookRepository>();
        _imageServiceMock = Substitute.For<IImageService>();
        _eventServiceMock = Substitute.For<IEventService>();
        _synonymMakerMock = Substitute.For<ISynonymMaker>();

        var services = new ServiceCollection();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped(_ => _bookRepositoryMock);
        services.AddScoped(_ => _imageServiceMock);
        services.AddScoped(_ => _eventServiceMock);
        services.AddScoped(_ => _synonymMakerMock);
        
        var serviceProvider = services.BuildServiceProvider();
        _bookService = serviceProvider.GetRequiredService<IBookService>();
    }
    
    [Test]
    public async Task SaveBookAsync_ShouldSaveBookAndPublishEvent_WhenValidBook()
    {
        const int rubricId = 1;
        const int imageId = 1;
        const int expectedBookId = 500;
        
        var rubric = new Rubric { Id = rubricId, Name = "Test Rubric" };
        var image = new Image 
        { 
            Id = imageId, 
            Data = [] 
        };
        
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
        
        _imageServiceMock
            .GetAsync(imageId, null, Arg.Any<CancellationToken>())
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
        
        await _eventServiceMock
            .Received(1)
            .PublishEventAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
        
        result.Id.Should().Be(expectedBookId);
        result.Name.Should().Be(book.Name);
        result.Author.Should().Be(book.Author);
        result.RubricId.Should().Be(book.RubricId);
        result.ImageId.Should().Be(book.ImageId);
        result.Description.Should().Be(book.Description);
    }
    
    [Test]
    public async Task SaveBookAsync_ShouldThrowValidationException_WhenRubricNotExists()
    {
        const int imageId = 1;
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
        
        _imageServiceMock
            .GetAsync(imageId, null, Arg.Any<CancellationToken>())
            .Returns(image);
        
        await _bookService
            .Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая рубрика.");
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        
        await _eventServiceMock
            .Received(0)
            .PublishEventAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
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
        
        _imageServiceMock
            .GetAsync(nonExistentImageId, null, Arg.Any<CancellationToken>())
            .Returns((Image)null);
        
        await _bookService
            .Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая картинка.");
        
        await _bookRepositoryMock
            .Received(0)
            .SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
        
        await _eventServiceMock
            .Received(0)
            .PublishEventAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>());
    }
}