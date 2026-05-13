using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls.Base;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Factories;
using Microsoft.Playwright;

namespace Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;

public class LoginPage : PageBase
{
    // public LoginPage(IPage page) : base(page)
    // {
    // }
    public LoginPage(IPage page, IControlFactory controlFactory) : base(page, controlFactory)
    {
    }

    public override string Url => Urls.Login;
    public override string TitleText { get; }

    // public ILocator Email => Page.Locator("[type=email]");
    // public ILocator Password => Page.Locator("[id=password]");
    // public ILocator SignInButton => Page.Locator("[type=submit]");
    // public ILocator RememberMeCheckbox => Page.Locator("[id='rememberMe']");
    // public ILocator RegistrationLink => Page.Locator("a[href='/register']");
    
    public ValidationInput Email => ControlFactory.Create<ValidationInput>(Page.Locator("[data-tid='email']"));
    public ValidationInput Password => ControlFactory.Create<ValidationInput>(Page.Locator("[data-tid='password']"));
    public Button SignInButton => ControlFactory.Create<Button>(Page.Locator("[type=submit]"));
    public Checkbox RememberMeCheckbox => ControlFactory.Create<Checkbox>(Page.Locator("[id='rememberMe']"));
    public Link RegistrationLink => ControlFactory.Create<Link>(Page.Locator("a[href='/register']"));
    
    public async Task LoginAsync(string email, string password)
    {
        await Email.FillAsync(email);
        await Password.FillAsync(password);
        await SignInButton.ClickAsync();
    }
}