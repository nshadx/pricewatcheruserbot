using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonScrapper(
    ILogger<OzonScrapper> logger,
    BrowserService browserService,
    [FromKeyedServices("ozon")] Func<string, Task<string>> configProvider
) : IScrapper
{
    public async Task Authorize()
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        logger.LogInformation("Page init...");
        
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync("https://ozon.ru", new PageGotoOptions() { WaitUntil = WaitUntilState.NetworkIdle });
        
        logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("ozon_authorization_page_loaded");
        
        try
        {
            PageObject pageObject = new(page);

            logger.LogInformation("Check for login requirement...");

            var requiresLogin = await pageObject.RequiresLogin();
            if (requiresLogin)
            {
                logger.LogInformation("Begin login...");
                await page.Debug_TakeScreenshot("ozon_begin_login");
                
                await pageObject.OpenLoginForm();

                logger.LogInformation("Phone number requested");
                
                var phoneNumber = await configProvider("phone_number");
                await pageObject.EnterPhoneNumber(phoneNumber);
                
                logger.LogInformation("Phone number entered");
                await page.Debug_TakeScreenshot("ozon_phone_number_entered");
                
                logger.LogInformation("Submitting form...");
                
                await pageObject.LoginSubmit();
                
                logger.LogInformation("Form submitted");
                
                var isPhoneInvalid = await pageObject.IsPhoneNumberInvalid();
                if (isPhoneInvalid)
                {
                    logger.LogError("Invalid phone number entered");
                    return;
                }
                
                logger.LogInformation("Select code via phone authentication way...");
                
                await pageObject.SelectAnotherLoginWay();
                
                logger.LogInformation("Code via phone authentication way selected");
                await page.Debug_TakeScreenshot("ozon_another_way_of_authentication");

                logger.LogInformation("Code requested");
                
                var code = await configProvider("phone_verification_code");
                await pageObject.EnterCode(code);
                
                logger.LogInformation("Code entered");
                await page.Debug_TakeScreenshot("ozon_code_entered");

                logger.LogInformation("Check for email verification requirement...");
                
                var requiresEmail = await pageObject.IsEmailVerificationRequired();
                if (requiresEmail)
                {
                    await pageObject.SelectEmailVerification();
                    
                    logger.LogInformation("Email verification requested");
                    
                    var emailCode = await configProvider("email_verification_code");
                    await pageObject.EnterCode(emailCode);
                    
                    logger.LogInformation("Email verification code entered");
                    await page.Debug_TakeScreenshot("ozon_email_verification_code_entered");
                }

                if (!await pageObject.RequiresLogin())
                {
                    logger.LogInformation("Successful authorization");
                    await page.Debug_TakeScreenshot("ozon_successful_authorization");
                    
                    await browser.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = Environment.GetEnvironmentVariable("Session_Storage") });
                    
                    logger.LogInformation("Session saved");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authorize");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<double> GetPrice(Uri url)
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        logger.LogInformation("Page init...");
        
        page.SetDefaultTimeout(15000);
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync(url.ToString());
        
        logger.LogInformation("Page loaded");
        await page.Debug_TakeScreenshot("ozon_price_page_loaded");
        
        try
        {
            PageObject pageObject = new(page);
            
            logger.LogInformation("Begin price selecting...");
            
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

            logger.LogInformation("Price was received successfully"); 
            await page.Debug_TakeScreenshot("ozon_price_received");
            
            return priceValue;
        }
        finally
        {
            await page.CloseAsync();
        }
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
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button/div[contains(text(), 'Войти другим способом')]").First;

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

            return await locator.IsVisibleAsync() && !await locator.IsDisabledAsync();
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