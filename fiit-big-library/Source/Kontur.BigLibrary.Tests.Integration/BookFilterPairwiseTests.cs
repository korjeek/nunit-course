using System.Collections.Generic;
using System.Threading.Tasks;
using Kontur.BigLibrary.Service.Contracts;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration;

[TestFixture]
public class BookFilterPairwiseTests
{
    private static IEnumerable<TestCaseData> GetPairwiseFilterData()
    {
        yield return new TestCaseData(new BookFilter
        {
            Query = "C#",
            Order = BookOrder.ByRankAndLastAdding,
            IsBusy = true,
            RubricSynonym = "programming",
            Limit = 10,
            Offset = 0
        }).SetName("TC1_FullFilter_WithRankSorting");
        
        yield return new TestCaseData(new BookFilter
        {
            Query = null,
            Order = BookOrder.ByLastAdding,
            IsBusy = null,
            RubricSynonym = null,
            Limit = null,
            Offset = null
        }).SetName("TC2_EmptyFilter_DefaultBehavior");
        
        yield return new TestCaseData(new BookFilter
        {
            Query = "",
            Order = BookOrder.ByRankAndLastAdding,
            IsBusy = null,
            RubricSynonym = "programming",
            Limit = null,
            Offset = 10
        }).SetName("TC3_RankSorting_WithoutQuery_ShouldFallback");
        
        yield return new TestCaseData(new BookFilter
        {
            Query = "Clean Code",
            Order = BookOrder.ByLastAdding,
            IsBusy = false,
            RubricSynonym = null,
            Limit = 50,
            Offset = null
        }).SetName("TC4_QueryOnly_WithDefaultSorting");
        
        yield return new TestCaseData(null)
            .SetName("TC5_NullFilterObject_ShouldThrowOrHandle");
    }

    [TestCaseSource(nameof(GetPairwiseFilterData))]
    public async Task SelectBooksSummary_PairwiseTests(BookFilter filter)
    {
        await Task.CompletedTask;
    }
}
