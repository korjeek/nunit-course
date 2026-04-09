using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4.Data;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration;

[Parallelizable(ParallelScope.All)]
public class GoogleSheetsIntegrationTests
{
    private GoogleSheetsIntegration _integration;
    private SheetsService _sheetsService;
    private DriveService _driveService;
    
    private const string StructureTestSpreadsheetId = "1krG2GsklEyNKlHATY2sPcTqMFqk87Nbma2sPLq7oaZ8";

    [SetUp]
    public void SetUp()
    {
        _integration = new GoogleSheetsIntegration();

        var credential = GoogleCredential.FromFile("Integration/secrets.json")
            .CreateScoped(
                SheetsService.Scope.Spreadsheets, 
                DriveService.Scope.DriveFile);
        
        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        };
        
        _sheetsService = new SheetsService(initializer);
        _driveService = new DriveService(initializer);
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
        var oldFiles = _driveService.Files.List().Execute().Files
            .Where(f => f.Name.StartsWith("TestSheet_")); 
        foreach(var file in oldFiles) 
            _driveService.Files.Delete(file.Id).Execute();
        
        var newSpreadsheet = new Spreadsheet 
        { 
            Properties = new SpreadsheetProperties
            {
                Title = $"TestSheet_{Guid.NewGuid():N}" 
            }
        };
        var createdSpreadsheet = _sheetsService.Spreadsheets.Create(newSpreadsheet).Execute();
        var spreadsheetId = createdSpreadsheet.SpreadsheetId;
        var spreadsheetName = createdSpreadsheet.Sheets[0].Properties.Title;
        
        var expectedData = new List<IList<object>>
        {
            new List<object> { "Название книги", "№", "Описание", "Цена, Р" },
            new List<object> { "Книга 1", "1", "Описание 1", "100" },
            new List<object> { "Книга 2", "2", "Описание 2", "200" }
        };
        
        var valueRange = new ValueRange { Values = expectedData };
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, spreadsheetName);
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        updateRequest.Execute();
        
        var actualData = _integration.ReadData(spreadsheetId, spreadsheetName);
        expectedData.Should().BeEquivalentTo(actualData);
    }
}