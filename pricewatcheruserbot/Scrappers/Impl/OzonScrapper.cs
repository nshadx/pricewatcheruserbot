using Microsoft.Playwright;
using pricewatcheruserbot.Browser;
using pricewatcheruserbot.Configuration;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonScrapper(
    IUserInputProvider userInputProvider,
    ILogger<OzonScrapper> logger,
    BrowserService browserService
) : ScrapperBase(logger, browserService)
{
    public override Uri Host { get; } = new("https://ozon.ru");
    
    protected override async Task AuthorizeCore(IPage page)
    {
        PageObject pageObject = new(page);

            Logger.LogInformation("Check for login requirement...");

            var requiresLogin = await pageObject.RequiresLogin();
            
            Logger.LogInformation("Login requirement check completed");
            await page.Debug_TakeScreenshot("ozon_login_requirement_check");
            
            if (requiresLogin)
            {
                Logger.LogInformation("Begin login...");
                await page.Debug_TakeScreenshot("ozon_begin_login");
                
                await pageObject.OpenLoginForm();

                Logger.LogInformation("Login form opened");
                await page.Debug_TakeScreenshot("ozon_login_form_opened");
                
                Logger.LogInformation("Phone number requested");

                var phoneNumber = await userInputProvider.Ozon_GetPhoneNumber();
                await pageObject.EnterPhoneNumber(phoneNumber);
                
                Logger.LogInformation("Phone number entered");
                await page.Debug_TakeScreenshot("ozon_phone_number_entered");
                
                Logger.LogInformation("Submitting form...");
                
                await pageObject.LoginSubmit();
                
                Logger.LogInformation("Form submitted");
                await  page.Debug_TakeScreenshot("ozon_form_submitted");
                
                var isPhoneInvalid = await pageObject.IsPhoneNumberInvalid();
                if (isPhoneInvalid)
                {
                    Logger.LogError("Invalid phone number entered");
                    return;
                }
                
                Logger.LogInformation("Select code via phone authentication way...");
                
                await pageObject.SelectAnotherLoginWay();
                
                Logger.LogInformation("Code via phone authentication way selected");
                await page.Debug_TakeScreenshot("ozon_authentication_via_phone");

                Logger.LogInformation("Phone verification code requested");
                
                var code = await userInputProvider.Ozon_GetPhoneVerificationCode();
                await pageObject.EnterCode(code);
                
                Logger.LogInformation("Phone verification code entered");
                await page.Debug_TakeScreenshot("ozon_phone_verification_code_entered");

                Logger.LogInformation("Check for email verification requirement...");
                
                var requiresEmail = await pageObject.IsEmailVerificationRequired();
                if (requiresEmail)
                {
                    await pageObject.SelectEmailVerification();
                    
                    Logger.LogInformation("Email verification requested");
                    
                    var emailCode = await userInputProvider.Ozon_GetEmailVerificationCode();
                    await pageObject.EnterCode(emailCode);
                    
                    Logger.LogInformation("Email verification code entered");
                    await page.Debug_TakeScreenshot("ozon_email_verification_code_entered");
                }

                await BrowserService.SaveState();
                
                Logger.LogInformation("Successful authorization");
                await page.Debug_TakeScreenshot("ozon_successful_authorization");
            }
    }

    protected override async Task<double> GetPriceCore(IPage page)
    {
        PageObject pageObject = new(page);
            
        Logger.LogInformation("Begin price selecting...");
            
        var priceString = await pageObject.GetPrice();
        var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

        await BrowserService.SaveState();
            
        Logger.LogInformation("Price was received successfully"); 
        await page.Debug_TakeScreenshot("ozon_price_received");
            
        return priceValue;
    }

    private class PageObject(IPage page)
    {
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
        
        public async Task EnterCode(string code)
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::input[@type='number']").First;

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

        public async Task<bool> IsPhoneNumberInvalid()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//p[contains(text(), 'Некорректный формат телефона')]").First;

            return await locator.IsVisibleAsync();
        }

        public async Task<bool> RequiresLogin()
        {
            var locator = page.Locator("//div[contains(@data-widget, 'profileMenuAnonymous')]").First;

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