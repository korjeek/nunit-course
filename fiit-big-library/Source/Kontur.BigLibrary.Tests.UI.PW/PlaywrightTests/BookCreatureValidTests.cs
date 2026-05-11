using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[NonParallelizable]
[WithAuth("test@mail.com", "Test123456!")]
public class BookCreatureValidTests: TestBase
{
    private AddBookModal _bookModal;
    private string? _tempFilePath;
    
    [SetUp]
    public async Task SetUp()
    {
        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        _bookModal = await mainPage.AddBookButton.ClickAndOpenModalAsync<AddBookModal>();
    }
    
    [TearDown]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath)) 
            File.Delete(_tempFilePath);
    }
    
    [TestCase("", false)]
    [TestCase("a", true)]
    [TestCase("abcdefghijklmnopqrst", true)]
    [TestCase("abcdefghijklmnopqrstu", false)]
    [TestCase("2147483647", true)]
    [TestCase("2147483648", false)]
    [TestCase("-9999999999999999999", true)]
    [TestCase("@#$%^&*()_+-=", true)]
    public async Task NameAuthorDescFields_ValidationTests(string name, bool isValid)
    {
        await _bookModal.NameInput.FillAsync(name);
        await _bookModal.AddBookSubmit.ClickAsync();
        var nameResult = await _bookModal.NameInput.IsValid();
        
        await _bookModal.AuthorInput.FillAsync(name);
        await _bookModal.AddBookSubmit.ClickAsync();
        var authorResult = await _bookModal.AuthorInput.IsValid();
        
        await _bookModal.DescriptionInput.FillAsync(name);
        await _bookModal.AddBookSubmit.ClickAsync();
        var descriptionResult = await _bookModal.DescriptionInput.IsValid();
        
        Assert.Multiple(() =>
        {
            Assert.That(nameResult, Is.EqualTo(isValid));
            Assert.That(authorResult, Is.EqualTo(isValid));
            Assert.That(descriptionResult, Is.EqualTo(isValid));
        });
    }

    [TestCase(153600, ".jpg", true)]
    [TestCase(153601, ".jpg", false)]
    [TestCase(1000, ".png", false)]
    public async Task ImageUpload_ValidationTests(int sizeInBytes, string format, bool isValid)
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{format}");
        await File.WriteAllBytesAsync(_tempFilePath, new byte[sizeInBytes]);

        await _bookModal.UploadImage.SetInputFilesAsync(_tempFilePath);
        var result = await _bookModal.UploadImage.IsValid();
        await _bookModal.AddBookSubmit.ClickAsync();
        
        Assert.That(isValid, Is.EqualTo(result));
    }

    [TestCase("", false)]
    [TestCase("0", true)]
    [TestCase("0,0", false)]
    [TestCase("0.0", false)]
    [TestCase("-1", false)]
    [TestCase("2147483647", true)]
    [TestCase("2147483648", false)]
    [TestCase("abc", false)]
    [TestCase("@#$%^&*()_+-=", false)]
    public async Task PriceFields_ValidationTests(string price, bool isValid)
    {
        await _bookModal.PriceInput.FillAsync(price);
        await _bookModal.AddBookSubmit.ClickAsync();
        var result = await _bookModal.PriceInput.IsValid();
        
        Assert.That(isValid, Is.EqualTo(result));
    }
}