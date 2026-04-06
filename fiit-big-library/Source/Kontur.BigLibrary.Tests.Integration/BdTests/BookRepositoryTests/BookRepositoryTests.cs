using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Tests.Core.Helpers;
using Kontur.BigLibrary.Tests.Core.Helpers.StringGenerator;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BdTests.BookRepositoryTests;

[Parallelizable(ParallelScope.All)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class BookRepositoryTests
{
    private readonly IServiceProvider _container;
    private readonly IBookRepository _bookRepository;

    private readonly string _longString = StringGenerator.GetRandomString(10_000);
    private readonly ConcurrentBag<int> _bookIds = [];
    private readonly ConcurrentBag<Reader> _readers = [];
    
    [TearDown]
    public async Task Teardown()
    {
        foreach (var id in _bookIds)
        {
            await _bookRepository.DeleteBookAsync(id, CancellationToken.None);
            await _bookRepository.DeleteBookIndexAsync(id, CancellationToken.None);
        }
        foreach (var reader in _readers)
        {
            reader.IsDeleted = true;
            await _bookRepository.SaveReaderAsync(reader, CancellationToken.None);
        }
    }

    public BookRepositoryTests()
    {
        _container = new Container()
            .AddBookBuilder()
            .AddReaderBuilder()
            .Build();
        
        _bookRepository = _container.GetRequiredService<IBookRepository>();
    }

    [Test]
    public async Task GetBook_Exists_ReturnBook()
    {
        var expectedBook = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        await SaveBookForTest(expectedBook);

        var actualBook = await _bookRepository.GetBookAsync(expectedBook.Id!.Value, CancellationToken.None);

        actualBook.Should().BeEquivalentTo(expectedBook);
    }

    [Test]
    public async Task GetBook_WithLongDescription_ReturnBook()
    {
        var bookWithLongDescription = _container
            .GetRequiredService<BookBuilder>()
            .WithDescription(_longString)
            .Build();
        await SaveBookForTest(bookWithLongDescription);
        
        var actualBook = await _bookRepository.GetBookAsync(bookWithLongDescription.Id!.Value, CancellationToken.None);
        
        actualBook.Should().BeEquivalentTo(bookWithLongDescription);
    }

    [Test]
    public async Task GetBook_NotExists_ReturnNull()
    {
        var book = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        var actualBook = await _bookRepository.GetBookAsync(book.Id!.Value, CancellationToken.None);
        actualBook.Should().BeNull();
    }
    
    [Test]
    public async Task SelectBooks_ByName_ReturnOnlyNamedBook()
    {
        var uniqueBookName = StringGenerator.GetRandomString();
        var firstBook = _container
            .GetRequiredService<BookBuilder>()
            .WithName(uniqueBookName)
            .Build();
        var secondBook = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        await SaveBookForTest(firstBook);
        await SaveBookForTest(secondBook);

        var bookFilter = new BookFilter { Query = uniqueBookName};
        var actualBooks = await _bookRepository.SelectBooksAsync(bookFilter, CancellationToken.None);

        using (new AssertionScope())
        {
            actualBooks.Should().ContainSingle();
            firstBook.Name.Should().Be(actualBooks.Single().Name);
        }
    }
    
    [Test]
    public async Task SelectBooks_WithDeletedBooks_ReturnNull()
    {
        var bookToDelete = _container
            .GetRequiredService<BookBuilder>()
            .Delete()
            .Build();
        await SaveBookForTest(bookToDelete);

        var bookFilter = new BookFilter { Query = $"book {bookToDelete.Id}" };
        var actualBooks = await _bookRepository.SelectBooksAsync(bookFilter, CancellationToken.None);

        actualBooks.Should().BeEmpty();
    }

    [Test]
    public async Task SelectBooks_WithOrderAndOffset_ReturnBooks()
    {
        var prefix = StringGenerator.GetRandomString();
        var baseId = IntGenerator.Get();
        var firstBook = _container
            .GetRequiredService<BookBuilder>()
            .WithName($"{prefix} 1")
            .WithId(baseId)
            .Build();
        var secondBook = _container
            .GetRequiredService<BookBuilder>()
            .WithName($"{prefix} 2")
            .WithId(baseId + 1)
            .Build();
        var thirdBook = _container
            .GetRequiredService<BookBuilder>()
            .WithName($"{prefix} 3")
            .WithId(baseId + 2)
            .Build();   
        await SaveBookForTest(firstBook);
        await SaveBookForTest(secondBook);
        await SaveBookForTest(thirdBook);

        var bookFilter = new BookFilter
        {
            Query = prefix,
            Order = BookOrder.ByLastAdding,
            Offset = 1,
            Limit = 2
        };
        var actualBooks = await _bookRepository.SelectBooksAsync(bookFilter, CancellationToken.None);

        actualBooks.Select(x => x.Id).Should().Equal(secondBook.Id, firstBook.Id);
    }

    [Test]
    public async Task GetBookSummaryBySynonym_WithSelectBooksSummary_ShouldEquals()
    {
        var book = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        await SaveBookForTest(book);
        var synonym = $"book {book.Id}";
        
        var filter = new BookFilter { Synonym = synonym};
        var booksSummary = await _bookRepository.SelectBooksSummaryAsync(filter, CancellationToken.None);
        var expectedSummary = await _bookRepository.GetBookSummaryBySynonymAsync(synonym, CancellationToken.None);
        
        using (new AssertionScope())
        {
            booksSummary.Should().ContainSingle();
            expectedSummary.Should().BeEquivalentTo(booksSummary.Single());
        }
    }

    [Test]
    public async Task GetBookSummaryBySynonym_WithBusyBook_ReturnBook()
    {
        var freeBook = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        var busyBook = _container
            .GetRequiredService<BookBuilder>()
            .Build();
        await SaveBookForTest(freeBook);
        await SaveBookForTest(busyBook);
        
        var reader = _container
            .GetRequiredService<ReaderBuilder>()
            .WithBook(busyBook.Id!.Value)
            .Build();
        await SaveReaderForTest(reader);
        
        var bookSummary = await _bookRepository.GetBookSummaryBySynonymAsync($"book {busyBook.Id}", CancellationToken.None);
        
        bookSummary.IsBusy.Should().BeTrue();
    }

    private async Task SaveBookForTest(Book book)
    {
        await _bookRepository.SaveBookAsync(book, CancellationToken.None);
        await _bookRepository.SaveBookIndexAsync(book.Id!.Value, book.GetTextForFts(),
            $"book {book.Id!.Value}", CancellationToken.None);
        _bookIds.Add(book.Id.Value);
    }

    private async Task SaveReaderForTest(Reader reader)
    {
        await _bookRepository.SaveReaderAsync(reader, CancellationToken.None);
        _readers.Add(reader);
    }
}