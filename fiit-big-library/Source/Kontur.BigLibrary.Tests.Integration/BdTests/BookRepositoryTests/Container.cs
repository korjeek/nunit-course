using System;
using Kontur.BigLibrary.DataAccess;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Tests.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Kontur.BigLibrary.Tests.Integration.BdTests.BookRepositoryTests;

public class Container
{
    private IServiceCollection _collection;

    public Container()
    {
        _collection = new ServiceCollection();
        _collection.AddSingleton<IDbConnectionFactory>(x => new DbConnectionFactory(DbHelper.ConnectionString));
        _collection.AddSingleton<IBookRepository, BookRepository>();
    }

    public Container AddBookBuilder()
    {
        _collection.AddTransient<BookBuilder>();
        return this;
    }

    public Container AddReaderBuilder()
    {
        _collection.AddTransient<ReaderBuilder>();
        return this;
    }

    public IServiceProvider Build()
    {
        return _collection.BuildServiceProvider();
    }
}