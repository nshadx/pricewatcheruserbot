using Microsoft.Playwright;

namespace pricewatcheruserbot.Scrappers.Impl;

public class OzonScrapper(
    ILogger<OzonScrapper> logger,
    BrowserService browserService,
    [FromKeyedServices("ozon")] Func<string, string> configProvider
) : IScrapper
{
    public async Task Authorize()
    {
        var browser = await browserService.GetBrowserContext();
        var page = await browser.NewPageAsync();
        
        logger.LogInformation("Page init...");
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync("https://ozon.ru");
        logger.LogInformation("Page loaded");
        
        try
        {
            PageObject pageObject = new(page);

            logger.LogInformation("Check for login requirement...");
            
            var requiresLogin = await pageObject.RequiresLogin();
            if (requiresLogin)
            {
                logger.LogInformation("Begin login...");
                
                await pageObject.ClickLogin();

                logger.LogInformation("Phone number requested");
                
                var phoneNumber = configProvider("phone_number");
                await pageObject.EnterPhoneNumber(phoneNumber);
                
                logger.LogInformation("Select code via phone authentication way...");
                await pageObject.LoginSubmit();
                await pageObject.SelectAnotherLoginWay();

                logger.LogInformation("Code requested");
                
                var code = configProvider("code");
                await pageObject.EnterCode(code);

                logger.LogInformation("Check for email verification requirement...");
                
                var requiresEmail = await pageObject.SendEmailVerification();
                if (requiresEmail)
                {
                    logger.LogInformation("Email verification requested");
                    
                    var emailCode = configProvider("email_code");
                    await pageObject.EnterCode(emailCode);
                }

                if (!await pageObject.RequiresLogin())
                {
                    logger.LogInformation("Successful authorization");
                    
                    await browser.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = Environment.GetEnvironmentVariable("Session_Storage") });
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
        
        try
        {
            PageObject pageObject = new(page);
            
            logger.LogInformation("Begin price selecting...");
            
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

            logger.LogInformation("Price was received successfully");
            
            return priceValue;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private class PageObject(IPage page)
    {
        public async Task<bool> SendEmailVerification()
        {
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]/descendant::button[@type='submit']").First;

            try
            {
                await locator.ClickAsync();
                return true;
            }
            catch { return false; }
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
            
            await locator.PressSequentiallyAsync(phoneNumber);
        }

        public async Task<bool> RequiresLogin()
        {
            var locator = page.Locator("//div[contains(@data-widget, 'profileMenuAnonymous')]").First;

            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions() { State = WaitForSelectorState.Visible });
                return true;
            } catch { return false; }
        }
        
        public async Task ClickLogin()
        {
            var loginButton = page
                .Locator("//div[contains(@data-widget, 'profileMenuAnonymous')]")
                .First;
            var locator = page
                .FrameLocator("#authFrame")
                .Locator("//div[contains(@data-widget, 'loginOrRegistration')]").First;
            
            while (!await locator.IsVisibleAsync())
            {
                try
                {
                    await loginButton.ClickAsync();
                }
                catch { break; }
            }
        }
        
        public async Task<string> GetPrice()
        {
            var locator = page
                .Locator("//div[contains(@data-widget, 'webPrice')]/descendant::span").First
                .Or(
                    page.Locator("//div/child::span[text()='c Ozon Картой']/../div/span")
                ).First;
            
            var result = await locator.TextContentAsync() ?? throw new InvalidOperationException();

            return result;
        }
    }
}