using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Exceptions;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.ImageService;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class BookServiceTest
{
    private static readonly IServiceProvider container = new ContainerForBdTests().Build();
    private static readonly IBookService bookService = container.GetRequiredService<IBookService>();
    private static readonly IImageService imageService = container.GetRequiredService<IImageService>();

    [Test]
    public async Task SaveBookAsync_ReturnSameBook_WhenSaveCorrectBook()
    {
        var imageForSave = new Image { Data = [] };
        var image = await imageService.SaveAsync(imageForSave, CancellationToken.None).ConfigureAwait(false);

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D.Ullman, Jennifer Widom",
            RubricId = 1,
            ImageId = image.Id!.Value,
            Description = "New_book"
        };
        
        var result = await bookService.SaveBookAsync(book, CancellationToken.None);
        
        result.Name.Should().Be(book.Name);
        result.Author.Should().Be(book.Author);
        result.RubricId.Should().Be(book.RubricId);
        result.ImageId.Should().Be(image.Id);
        result.Description.Should().Be(book.Description);
    }

    [Test]
    public async Task SaveBookAsync_ThrowsValidationException_WhenRubricNotExists()
    {
        var imageForSave = new Image { Data = [] };
        var image = await imageService.SaveAsync(imageForSave, CancellationToken.None).ConfigureAwait(false);
        
        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D.Ullman, Jennifer Widom",
            RubricId = -1,
            ImageId = image.Id!.Value,
            Description = "New_book"
        };
        
        await bookService.Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая рубрика.");
    }
    
    [Test]
    public async Task SaveBookAsync_ThrowsValidationException_WhenImageNotExists()
    {
        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D.Ullman, Jennifer Widom",
            RubricId = 1,
            ImageId = -1,
            Description = "New_book"
        };
        
        await bookService.Invoking(async x => await x.SaveBookAsync(book, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая картинка.");
    }
}