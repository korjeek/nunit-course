using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls.Base;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Factories;
using Microsoft.Playwright;

namespace Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls;

public class AddBookModal : ModalBase<AddBookModal>, IModal
{
    // public AddBookModal(ILocator locator) : base(locator)
    public AddBookModal(IPage page, IControlFactory controlFactory, IPageFactory pageFactory) : base(page, controlFactory, pageFactory)
    {
    }

    public static string Selector => "[id='create-book-lightbox']";
    
    public UploadButton UploadImage => ControlFactory.Create<UploadButton>(Locator.Locator("[id='bookImageFile']"));
    
    public DropdownSelect RubricDropdown => ControlFactory.Create<DropdownSelect>(Locator.Locator("[id='bookRubricId']"));
    
    public Input NameInput => ControlFactory.Create<Input>(Locator.Locator("[id='bookName']"));
    
    public Input AuthorInput => ControlFactory.Create<Input>(Locator.Locator("[id='bookAuthor']"));
    
    public Input DescriptionInput => ControlFactory.Create<Input>(Locator.Locator("[id='bookDescription']"));
    
    public Button AddBookSubmit => ControlFactory.Create<Button>(Locator.Locator("[id='add-book-button']"));
    public Input PriceInput => ControlFactory.Create<Input>(Locator.Locator("[id='bookPrice']"));
    public Label NameInvalidFeedback => ControlFactory.Create<Label>(Locator.Locator("[id='bookName-invalid-feedback']"));
    public Label AuthorInvalidFeedback => ControlFactory.Create<Label>(Locator.Locator("[id='bookAuthor-invalid-feedback']"));
    public Label DescriptionInvalidFeedback => ControlFactory.Create<Label>(Locator.Locator("[id='bookDescription-invalid-feedback']"));
    public Label ImageInvalidFeedback => ControlFactory.Create<Label>(Locator.Locator("[id='bookImageFile-invalid-feedback']"));
    public Label PriceInvalidFeedback => ControlFactory.Create<Label>(Locator.Locator("[id='bookPrice-invalid-feedback']"));
    
    public async Task FillAndSubmitAsync(Book book, string rubric, string imagePath)
    {
        await NameInput.FillAsync(book.Name);
        await AuthorInput.FillAsync(book.Author);
        await DescriptionInput.FillAsync(book.Description);
        await RubricDropdown.SelectByText(rubric);
        await UploadImage.SetInputFilesAsync(imagePath);
        await AddBookSubmit.ClickAsync();
    }

}