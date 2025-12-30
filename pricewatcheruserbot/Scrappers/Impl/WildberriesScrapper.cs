using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class WildberriesScrapper(
    WildberriesInput input,
    ILogger<WildberriesScrapper> logger,
    BrowserService browserService,
    IOptions<BrowserConfiguration> configuration
) : ScrapperBase(logger, browserService, configuration)
{
    public override Uri BaseUrl { get; } = new("https://wildberries.ru");
    
    protected override async Task AuthorizeCore()
    {
        PageObject pageObject = new(Page);

        Logger.LogInformation("Closing modals...");
        
        await pageObject.CloseModals();
        
        Logger.LogInformation("Modals closed");
        
        Logger.LogInformation("Check for login requirement...");

        var requiresLogin = await pageObject.RequiresLogin();

        Logger.LogInformation("Login requirement check completed");
        await TakeScreenshot("wildberries_login_requirement_check");

        if (requiresLogin)
        {
            await TakeScreenshot("wildberries_begin_login");

            await pageObject.OpenLoginForm();

            Logger.LogInformation("Login form opened");
            await TakeScreenshot("wildberries_login_form_opened");

            Logger.LogInformation("Phone number requested");

            var phoneNumber = await input.GetPhoneNumber();
            await pageObject.EnterPhoneNumber(phoneNumber);

            Logger.LogInformation("Phone number entered");
            await TakeScreenshot("wildberries_phone_number_entered");

            Logger.LogInformation("Submitting form...");

            await pageObject.LoginSubmit();

            Logger.LogInformation("Form submitted");
            await TakeScreenshot("wildberries_form_submitted");
            
            Logger.LogInformation("Phone verification code requested");

            var phoneVerificationCode = await input.GetPhoneVerificationCode();
            await pageObject.EnterPhoneVerificationCode(phoneVerificationCode);

            Logger.LogInformation("Phone verification code entered");
            await TakeScreenshot("wildberries_phone_verification_code_entered");
        }
    }

    protected override async Task<double> GetPriceCore()
    {
        PageObject pageObject = new(Page);
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

        Logger.LogInformation("Price was received successfully"); 
        await TakeScreenshot("wildberries_price_received");
        
        return priceValue;
    }

    private class PageObject(IPage page)
    {
        public async Task CloseModals()
        {
            var locator = page
                .Locator("//div[contains(@class, 'popupContent')]").First;

            var boundingBox = await locator.BoundingBoxAsync();
            
            if (boundingBox is not null)
            {
                const int offset = 100;
                var leftX = boundingBox.X - boundingBox.Width / 2 - offset;
                var leftY = boundingBox.Y;
            
                await page.Mouse.ClickAsync(leftX, leftY);
            }
        }
        
        public async Task EnterPhoneVerificationCode(string code)
        {
            var locator = page
                .Locator("//form[contains(@id, 'spaAuthForm')]/descendant::input[contains(@inputmode, 'numeric')]").First;

            await locator.FillAsync(code);
        }
        
        public async Task LoginSubmit()
        {
            var locator = page
                .Locator("//form[contains(@id, 'spaAuthForm')]/descendant::button[contains(@id, 'requestCode')]").First;
            
            await locator.ClickAsync();
        }
        
        public async Task EnterPhoneNumber(string phoneNumber)
        {
            var locator = page
                .Locator("//form[contains(@id, 'spaAuthForm')]/descendant::input[contains(@inputmode, 'tel')]").First;
            
            await locator.FillAsync(phoneNumber);
        }
        
        public async Task OpenLoginForm()
        {
            var locator = page
                .Locator("//a[contains(@data-wba-header-name, 'Login')]/descendant::p[contains(text(), 'Войти')]/..");

            await locator.ClickAsync();
        }
        
        public async Task<bool> RequiresLogin()
        {
            var locator = page
                .Locator("//a[contains(@data-wba-header-name, 'Login')]/descendant::p[contains(text(), 'Войти')]");

            await locator.WaitForAsync();

            return await locator.IsVisibleAsync() && await locator.IsEnabledAsync();
        }
        
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@class, 'priceBlockPrice')]").First;

            var result = await locator.TextContentAsync();

            return result;
        }
    }
}