using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class YandexMarketScrapper(
    YandexInput input,
    ILogger<YandexMarketScrapper> logger,
    BrowserService browserService,
    IOptions<BrowserConfiguration> configuration
) : ScrapperBase(logger, browserService, configuration)
{
    public override Uri BaseUrl { get; } = new("https://market.yandex.ru");
    
    protected override async Task AuthorizeCore()
    {
        PageObject pageObject = new(Page);
        
        Logger.LogInformation("Closing modals...");
        
        await pageObject.CloseModals();
        
        Logger.LogInformation("Modals closed");

        Logger.LogInformation("Check for login requirement...");

        var requiresLogin = await pageObject.RequiresLogin();

        Logger.LogInformation("Login requirement check completed");
        await TakeScreenshot("yandex_market_login_requirement_check");

        if (requiresLogin)
        {
            await TakeScreenshot("yandex_market_begin_login");

            await pageObject.OpenLoginForm();

            Logger.LogInformation("Login form opened");
            await TakeScreenshot("yandex_market_login_form_opened");

            Logger.LogInformation("Phone number requested");

            var phoneNumber = await input.GetPhoneNumber();
            await TakeScreenshot("yandex_market_page_loaded_input_1");
            await pageObject.EnterPhoneNumber(phoneNumber);

            Logger.LogInformation("Phone number entered");
            await TakeScreenshot("yandex_market_phone_number_entered");

            Logger.LogInformation("Submitting form...");

            await pageObject.LoginSubmit();

            Logger.LogInformation("Form submitted");
            await TakeScreenshot("yandex_market_form_submitted");
            
            Logger.LogInformation("Phone verification code requested");

            var phoneVerificationCode = await input.GetPhoneVerificationCode();
            await TakeScreenshot("yandex_market_page_loaded_input_2");
            await pageObject.EnterPhoneVerificationCode(phoneVerificationCode);

            Logger.LogInformation("Phone verification code entered");
            await TakeScreenshot("yandex_market_phone_verification_code_entered");
            
            var suggestedAccounts = await pageObject.GetSuggestedAccounts();
            await TakeScreenshot("yandex_market_page_loaded_input_3");
            var account = await input.GetAccount(suggestedAccounts);

            await pageObject.SelectAccount(account);
        }
    }

    protected override async Task<double> GetPriceCore()
    {
        PageObject pageObject = new(Page);
        
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);
        
        Logger.LogInformation("Price was received successfully"); 
        await TakeScreenshot("yandex_market_price_received");

        return priceValue;
    }

    private class PageObject(IPage page)
    {
        public async Task CloseModals()
        {
            var locator = page
                .Locator("//div[contains(@data-baobab-name, 'loginPopup')]").First;
        
            if (await locator.IsVisibleAsync())
            {
                var boundingBox = await locator.BoundingBoxAsync();
        
                if (boundingBox is not null)
                {
                    const int offset = 100;
                    var leftX = boundingBox.X - boundingBox.Width / 2 - offset;
                    var leftY = boundingBox.Y;
                
                    await page.Mouse.ClickAsync(leftX, leftY);
                }
            }
        }
        
        public async Task SelectAccount(string account)
        {
            var locator = page.Locator($"//div[contains(@class, 'Suggest-account-list')]/descendant::div[contains(., '{account}')]/div[contains(@data-react-aria-pressable, 'true')]");

            await locator.ClickAsync();
        }
        
        public async Task<IReadOnlyCollection<string>> GetSuggestedAccounts()
        {
            var locator = page.Locator("//div[contains(@class, 'Suggest-account-list')]/descendant::div[contains(@class, 'UserLogin-info')]");
            
            return await locator.AllInnerTextsAsync();
        }
        
        public async Task EnterPhoneVerificationCode(string code)
        {
            var locator = page
                .Locator("//div[contains(@class, 'body-auth')]/descendant::input[contains(@inputmode, 'numeric')]");
            var inputs = await locator.AllAsync();

            for (var i = 0; i < inputs.Count && i < code.Length; i++)
            {
                var input = inputs[i];
                var codeSymbol = code[i].ToString();
                
                await input.FillAsync(codeSymbol);
            }
        }
        
        public async Task LoginSubmit()
        {
            var locator = page
                .Locator("//form/descendant::button/descendant::span[contains(text(), 'Log in')]/..")
                .Or(page.Locator("//form/descendant::button/descendant::span[contains(text(), 'Войти')]/..")).First;
            
            await locator.ClickAsync();
        }
        
        public async Task EnterPhoneNumber(string phoneNumber)
        {
            var locator = page
                .Locator("//form/descendant::input[contains(@type, 'tel')]").First;
            
            await locator.FillAsync(phoneNumber);
        }
        
        public async Task OpenLoginForm()
        {
            var locator = page
                .Locator("//div[contains(@id, 'USER_MENU_ANCHOR')]/descendant::a[contains(text(), 'Войти')]");

            await locator.ClickAsync();
        }
        
        public async Task<bool> RequiresLogin()
        {
            var locator = page
                .Locator("//div[contains(@id, 'USER_MENU_ANCHOR')]/descendant::a[contains(text(), 'Войти')]");
            
            return await locator.IsVisibleAsync() && await locator.IsEnabledAsync();
        }
        
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//span[contains(@data-auto, 'snippet-price-current')]/descendant::span[1]").First;

            var result = await locator.TextContentAsync();

            return result;
        }
    }
}