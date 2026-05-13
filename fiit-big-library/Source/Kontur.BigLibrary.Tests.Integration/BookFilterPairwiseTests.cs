using System.Threading.Tasks;
using Kontur.BigLibrary.Service.Contracts;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration;

[TestFixture]
public class BookFilterPairwiseTests
{
    [Test, Pairwise]
    public async Task SelectBooksSummary_Pairwise(
        [Values("c#", "", null)] string query,
        [Values(BookOrder.ByRankAndLastAdding, BookOrder.ByLastAdding)] BookOrder order,
        [Values(true, false, null)] bool? isBusy,
        [Values("programming", null)] string rubricSynonym,
        [Values("csharp", null)] string synonym,
        [Values(10, null)] int? limit,
        [Values(0, null)] int? offset)
    {
        var filter = new BookFilter
        {
            Query = query,
            Order = order,
            IsBusy = isBusy,
            RubricSynonym = rubricSynonym,
            Synonym = synonym,
            Limit = limit,
            Offset = offset
        };
        
        await Task.CompletedTask;
    }
}