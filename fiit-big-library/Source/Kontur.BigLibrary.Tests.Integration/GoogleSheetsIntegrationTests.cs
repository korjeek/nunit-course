using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration;

[Parallelizable(ParallelScope.All)]
public class GoogleSheetsIntegrationTests
{
    private GoogleSheetsIntegration _integration;
    private SheetsService _sheetsService;
    
    private const string StructureTestSpreadsheetId = "1krG2GsklEyNKlHATY2sPcTqMFqk87Nbma2sPLq7oaZ8";

    [SetUp]
    public void SetUp()
    {
        _integration = new GoogleSheetsIntegration();

        var credential = GoogleCredential.FromFile("Integration/secrets.json")
            .CreateScoped(SheetsService.Scope.Spreadsheets);
        
        _sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });
    }

    [Test]
    public void TestTableHeaderStructure()
    {
        var expectedHeaders = new List<string> { "Название книги", "№", "Описание", "Цена, Р" };
        
        var headersData = _integration.ReadData(StructureTestSpreadsheetId, "A1:D1");

        headersData.Should().NotBeNull();
        headersData.Count.Should().Be(1);
        
        var actualHeaders = headersData[0].Select(cell => cell?.ToString()).ToList();
        actualHeaders.Should().Equal(expectedHeaders);
    }

    [Test]
    public void TestReadEntirePage_ReturnsAllData()
    {
        var newSpreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties
            {
                Title = $"TestSheet_{Guid.NewGuid():N}"
            }
        };
        var createRequest = _sheetsService.Spreadsheets.Create(newSpreadsheet);
        var createdSpreadsheet = createRequest.Execute();
        var spreadsheetId = createdSpreadsheet.SpreadsheetId;
        var sheetId = createdSpreadsheet.Sheets[0].Properties.SheetId;

        try
        {
            var expectedData = new List<IList<object>>
            {
                new List<object> { "Название книги", "№", "Описание", "Цена, Р" },
                new List<object> { "Книга 1", "1", "Описание 1", "100" },
                new List<object> { "Книга 2", "2", "Описание 2", "200" }
            };
            
            var valueRange = new ValueRange { Values = expectedData };
            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, "A1:D3");
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            updateRequest.Execute();
            
            var actualData = _integration.ReadData(spreadsheetId, "A1:D3");
            expectedData.Should().BeEquivalentTo(actualData);
        }
        finally
        {
            ClearSheet(spreadsheetId, sheetId!.Value);
        }
    }
    
    private void ClearSheet(string spreadsheetId, int sheetId)
    {
        var getRequest = _sheetsService.Spreadsheets.Get(spreadsheetId);
        var spreadsheet = getRequest.Execute();
        var sheet = spreadsheet.Sheets.First(s => s.Properties.SheetId == sheetId);
        var rowCount = sheet.Properties.GridProperties.RowCount.GetValueOrDefault();
        var columnCount = sheet.Properties.GridProperties.ColumnCount.GetValueOrDefault();

        if (rowCount <= 0 || columnCount <= 0) 
            return;
        
        var range = $"{sheet.Properties.Title}!A1:{GetColumnLetter(columnCount)}{rowCount}";
        
        var clearRequest = _sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, range);
        clearRequest.Execute();
    }
    
    private string GetColumnLetter(int columnNumber)
    {
        var columnLetter = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            columnLetter = (char)('A' + columnNumber % 26) + columnLetter;
            columnNumber /= 26;
        }
        
        return columnLetter;
    }
}