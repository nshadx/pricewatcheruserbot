using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using pricewatcheruserbot.Browser;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonScrapper(
    ILogger<OzonScrapper> logger,
    BrowserService browserService,
    IOptions<BrowserConfiguration> configuration,
    OzonInput input
) : ScrapperBase(logger, browserService, configuration)
{
    public override string BaseUrl { get; } = new("https://ozon.ru");

    protected override async Task AuthorizeCore()
    {
        PageObject pageObject = new(Page);

        await TakeScreenshot("ozon_begin_login");

        await pageObject.OpenLoginForm();

        Logger.LogInformation("Login form opened");
        await TakeScreenshot("ozon_login_form_opened");

        Logger.LogInformation("Phone number requested");

        var phoneNumber = await input.GetPhoneNumber();
        await TakeScreenshot("ozon_page_loaded_input_1");
        await pageObject.EnterPhoneNumber(phoneNumber);

        Logger.LogInformation("Phone number entered");
        await TakeScreenshot("ozon_phone_number_entered");

        Logger.LogInformation("Submitting form...");

        await pageObject.LoginSubmit();

        Logger.LogInformation("Form submitted");
        await TakeScreenshot("ozon_form_submitted");

        // var isPhoneInvalid = await pageObject.IsPhoneNumberInvalid();
        // if (isPhoneInvalid)
        // {
        //     Logger.LogError("Invalid phone number entered");
        //     return;
        // }

        Logger.LogInformation("Select code via phone authentication way...");

        await pageObject.SelectAnotherLoginWay();

        Logger.LogInformation("Code via phone authentication way selected");
        await TakeScreenshot("ozon_authentication_via_phone");

        Logger.LogInformation("Phone verification code requested");

        var phoneVerificationCode = await input.GetPhoneVerificationCode();
        await TakeScreenshot("ozon_page_loaded_input_2");
        await pageObject.EnterPhoneVerificationCode(phoneVerificationCode);

        Logger.LogInformation("Phone verification code entered");
        await TakeScreenshot("ozon_phone_verification_code_entered");

        Logger.LogInformation("Check for email verification requirement...");

        var requiresEmail = await pageObject.IsEmailVerificationRequired();
        if (requiresEmail)
        {
            await pageObject.SelectEmailVerification();

            Logger.LogInformation("Email verification requested");

            var emailVerificationCode = await input.GetEmailVerificationCode();
            await TakeScreenshot("ozon_page_loaded_input_3");
            await pageObject.EnterEmailVerificationCode(emailVerificationCode);

            Logger.LogInformation("Email verification code entered");
            await TakeScreenshot("ozon_email_verification_code_entered");
        }

        Logger.LogInformation("Successful authorization");
        await TakeScreenshot("ozon_successful_authorization");
    }

    protected override async Task<bool> IsAuthorizedCore()
    {
        PageObject pageObject = new(Page);

        Logger.LogInformation("Closing modals...");
        
        await pageObject.CloseModals();
        
        Logger.LogInformation("Modals closed");

        Logger.LogInformation("Check for login requirement...");

        var requiresLogin = await pageObject.RequiresLogin();

        Logger.LogInformation("Login requirement check completed");
        await TakeScreenshot("ozon_login_requirement_check");

        return !requiresLogin;
    }

    protected override async Task<double> GetPriceCore()
    {
        PageObject pageObject = new(Page);
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);
        
        Logger.LogInformation("Price was received successfully"); 
        await TakeScreenshot("ozon_price_received");
            
        return priceValue;
    }

    private class PageObject(IPage page)
    {
        public Task CloseModals()
        {
            return Task.CompletedTask;
        }
        
        public async Task<bool> IsEmailVerificationRequired()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button[@type='submit']").First;

            return await locator.IsVisibleAsync();
        }
        
        public async Task SelectEmailVerification()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button[@type='submit']").First;

            await locator.ClickAsync();
        }
        
        public async Task EnterPhoneVerificationCode(string code)
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::input[@type='number']").First;

            await locator.FillAsync(code);
        }
        
        public async Task EnterEmailVerificationCode(string code)
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::input").First;

            await locator.FillAsync(code);
        }
        
        public async Task SelectAnotherLoginWay()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button/div[contains(text(), 'Войти другим способом')]/..").First;


            await locator.ClickAsync();
        }
        
        public async Task LoginSubmit()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button[@type='submit']").First;
            
            await locator.ClickAsync();
        }
        
        public async Task EnterPhoneNumber(string phoneNumber)
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::input[contains(@type, 'tel')]").First;
            
            await locator.FillAsync(phoneNumber);
        }

        // public async Task<bool> IsPhoneNumberInvalid()
        // {
        //     var locator = page
        //         .FrameLocator("#authFrame")
        //         .Locator("//p[contains(text(), 'Некорректный формат телефона')]").First;
        //
        //     return await locator.IsVisibleAsync();
        // }

        public async Task<bool> RequiresLogin()
        {
            var locator = page.Locator("//div[contains(@data-widget, 'profileMenuAnonymous')]").First;

            // i removed 'networkidle' awaiter before, it causes an issue for Ozon: we should await latest .js script that enables login button 
            await page.WaitForRequestFinishedAsync(new PageWaitForRequestFinishedOptions() { Predicate = request => request.Url.Contains("vendor-workbox") });

            return await locator.IsVisibleAsync() && await locator.IsEnabledAsync();
        }
        
        public async Task OpenLoginForm()
        {
            var loginButton = page
                .Locator("//div[contains(@data-widget, 'profileMenuAnonymous')]")
                .First;
            
            await loginButton.ClickAsync();
        }
        
        public async Task<string?> GetPrice()
        {
            var locator = page
                .Locator("//div[contains(@data-widget, 'webPrice')]/descendant::span").First
                .Or(
                    page.Locator("//div/child::span[text()='c Ozon Картой']/../div/span")
                ).First;
            
            var result = await locator.TextContentAsync();

            return result;
        }
    }
}