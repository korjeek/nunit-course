using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Factories;
using Microsoft.Playwright;

namespace Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls.Base;

public class Input : ControlBase
{
    // public Input(ILocator locator): base(locator)
    public Input(ILocator locator, IControlFactory controlFactory, IPageFactory pageFactory) : base(locator, controlFactory, pageFactory)
    {
    }

    public async Task FillAsync(string text)
    {
        await Locator.ClearAsync();
        await Locator.FillAsync(text);
    }

    public async Task<bool> IsValid()
    {
        var classValue = await GetAttributeValueAsync("class");
        return classValue == "form-control ";
    }
}