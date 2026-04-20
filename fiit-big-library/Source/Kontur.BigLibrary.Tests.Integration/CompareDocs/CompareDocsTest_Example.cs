using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.ImageService;
using Kontur.BigLibrary.Tests.Core.Helpers;
using Kontur.BigLibrary.Tests.Integration.BookServiceTests;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.CompareDocs;

[Parallelizable(ParallelScope.None)]
public class CompareDocsTestExample
{
    private static readonly IServiceProvider container = new ContainerForBdTests().Build();
    private static readonly IBookService bookService = container.GetRequiredService<IBookService>();
    private static readonly IImageService imageService = container.GetRequiredService<IImageService>();
    private int imageId;

    [OneTimeSetUp]
    public void SetUp()
    {
        var image = imageService
            .SaveAsync(new Image { Id = 1, Data = Array.Empty<byte>() }, new CancellationToken())
            .GetAwaiter().GetResult();
        imageId = image.Id!.Value;
    }

    [Test]
    public async Task Should_NotBeEmpty()
    {
        await CreateBook(); //Сохранение книги в БД

        var xmlResult =
            await bookService.ExportBooksToXmlAsync(CreateFilter(), CancellationToken.None); //выгружаем книги

        xmlResult.Should().NotBeEmpty(); //проверяем, что выгруженные данные не пустые
    }

    [Test]
    public async Task Should_Have_ExpectedInfo_RussianName()
    {
        var book = Books.RussianBook;
        await bookService.SaveBookAsync(book, CancellationToken.None); //создание книги с русскоязычным названием

        var xmlResult =
            await bookService.ExportBooksToXmlAsync(CreateFilter(book.Name), CancellationToken.None); //выгрузка данных
        var xDoc = XDocument.Parse(xmlResult);

        xDoc.Should().HaveElement("Book")
            .Which.Should().HaveElement("Title")
            .Which.Should().HaveValue(book.Name);
    }

    [Test]
    public async Task Should_NotContainBook_When_NoData()
    {
        var book = await CreateBook();

        var xmlResult = await bookService.ExportBooksToXmlAsync(CreateFilter(isBusy: true, query: book.Name), CancellationToken.None);

        xmlResult.Should()
            .Contain("<Books>").And
            .Contain("<ExportTime>").And
            .NotContainAny("<Book>");
    }

    [Test]
    public async Task Should_Have_ExpectedCountOfBooks()
    {
        for (var i = 0; i < 5; i++)
        {
            await CreateBook($"ExpectedCountOfBooks {i}");
        }

        var xmlResult = await bookService.ExportBooksToXmlAsync(CreateFilter(limit: 4, query:"ExpectedCountOfBooks"), CancellationToken.None);
        var xDoc = XDocument.Parse(xmlResult);

        xDoc.Should().HaveElement("Book", Exactly.Times(4));

        var count = new Regex("<Book>").Matches(xmlResult).Count;
        count.Should().Be(4);
    }

    [Test]
    public async Task Should_Be_ExpectedXML()
    {
        var book = CreateBook().GetAwaiter().GetResult();

        var exportTime = DateTime.Now;
        var xmlResult = await bookService.ExportBooksToXmlAsync(CreateFilter(query: book.Name), CancellationToken.None);
        var xDoc = XDocument.Parse(xmlResult);

        var expDoc = new XDocument(
            new XElement("Books",
                new XElement("ExportTime", exportTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new XElement("Book",
                    new XElement("Title", book.Name),
                    new XElement("Author", book.Author),
                    new XElement("Description", book.Description),
                    new XElement("RubricId", book.RubricId),
                    new XElement("ImageId", book.ImageId.ToString()),
                    new XElement("Price", book.Price),
                    new XElement("IsBusy", "false")
                )
            )
        );

        xDoc.Should().BeEquivalentTo(expDoc);
    }

    [Test]
    public async Task Should_Be_ExpectedXML_File()
    {
        for (var i = 0; i < 100; i++)
        {
            await imageService
                .SaveAsync(new Image { Id = i, Data = Array.Empty<byte>() }, new CancellationToken())
                .ConfigureAwait(false);
            await bookService.SaveBookAsync(
                new BookBuilder().WithId(i).WithName($"ExpectedXML_File {i}").WithAuthor($"Default author {i}").WithImage(i)
                    .Build(), CancellationToken.None);
        }

        var exportTime = DateTime.Now;
        var xmlResult = await bookService.ExportBooksToXmlAsync(CreateFilter(limit: 100, query: "ExpectedXML_File"), CancellationToken.None);
        var xDoc = XDocument.Parse(xmlResult);

        var expDoc = XDocument.Parse(
            File.ReadAllTextAsync(Path.Combine("Files", "exportBooks.xml")).GetAwaiter().GetResult()
                .Replace("*", exportTime.ToString("yyyy-MM-dd HH:mm:ss")));

        xDoc.Should().BeEquivalentTo(expDoc);
    }

    private async Task<Book> CreateBook(string name = null)
    {
        var book = new BookBuilder().WithName(name).WithImage(imageId).Build(); //создание книги
        await bookService.SaveBookAsync(book, CancellationToken.None); //Сохранение книги в БД
        return book;
    }

    private BookFilter CreateFilter(string query = "", string rubric = "", int? limit = 10, bool isBusy = false,
        BookOrder order = BookOrder.ByLastAdding, int offset = 0)
    {
        return new()
        {
            Query = query,
            RubricSynonym = rubric,
            IsBusy = isBusy,
            Limit = limit,
            Order = order,
            Offset = offset
        };
    }
    
    [Test]
    public async Task Should_Have_ExpectedOrder_XDocumentAndLinq()
    {
        const string query = "OrderTest_LINQ";
        var createdBooks = await CreateBooksWithPrefix(query);
        
        var expectedTitles = createdBooks
            .OrderByDescending(b => b.Id)
            .Select(b => b.Name)
            .ToList();
        
        var filter = CreateFilter(query: query, limit: 5, order: BookOrder.ByLastAdding);
        var xmlResult = await bookService.ExportBooksToXmlAsync(filter, CancellationToken.None);
        var xDoc = XDocument.Parse(xmlResult);
        var actualTitles = xDoc
            .Descendants("Book")
            .Elements("Title")
            .Select(t => t.Value)
            .ToList();

        actualTitles.Should().Equal(expectedTitles);
    }

    [Test]
    public async Task Should_Have_ExpectedOrder_Regex()
    {
        const string query = "OrderTest_Regex";
        var createdBooks = await CreateBooksWithPrefix(query);

        var expectedTitles = createdBooks
            .OrderByDescending(b => b.Id)
            .Select(b => b.Name)
            .ToList();
        
        var filter = CreateFilter(query: query, limit: 5, order: BookOrder.ByLastAdding);
        var xmlResult = await bookService.ExportBooksToXmlAsync(filter, CancellationToken.None);
        var matches = Regex.Matches(xmlResult, @"<Title>(.*?)</Title>");
        var actualTitles = matches.Select(m => m.Groups[1].Value).ToList();

        actualTitles.Should().Equal(expectedTitles);
    }

    [Test]
    public async Task Should_Have_ExpectedOrder_FullXDocumentComparison()
    {
        const string query = "OrderTest_Full";
        var createdBooks = await CreateBooksWithPrefix(query);
        
        var orderedBooks = createdBooks.OrderByDescending(b => b.Id).ToList();
        var exportTime = DateTime.Now;
        var expectedDoc = new XDocument(
            new XElement("Books",
                new XElement("ExportTime", exportTime.ToString("yyyy-MM-dd HH:mm:ss")),
                orderedBooks.Select(book =>
                    new XElement("Book",
                        new XElement("Title", book.Name),
                        new XElement("Author", book.Author),
                        new XElement("Description", book.Description),
                        new XElement("RubricId", book.RubricId),
                        new XElement("ImageId", book.ImageId.ToString()),
                        new XElement("Price", book.Price),
                        new XElement("IsBusy", "false")
                    )
                )
            )
        );
        
        var filter = CreateFilter(query: query, limit: 5, order: BookOrder.ByLastAdding);
        var xmlResult = await bookService.ExportBooksToXmlAsync(filter, CancellationToken.None);
        var actualDoc = XDocument.Parse(xmlResult);
        
        actualDoc.Should().BeEquivalentTo(expectedDoc);
    }

    private async Task<List<Book>> CreateBooksWithPrefix(string prefix = "", int count = 5)
    {
        var createdBooks = new List<Book>();
        for (var i = 1; i <= count; i++)
        {
            var book = await CreateBook($"{prefix} {i}");
            createdBooks.Add(book);
        }

        return createdBooks;
    }
}