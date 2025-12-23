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

        page.SetDefaultTimeout(5000);
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync("https://ozon.ru");

        try
        {
            PageObject pageObject = new(page);

            var requiresLogin = await pageObject.RequiresLogin();
            if (requiresLogin)
            {
                await pageObject.ClickLogin();

                var phoneNumber = configProvider("phone_number");
                await pageObject.EnterPhoneNumber(phoneNumber);
                await pageObject.LoginSubmit();
                await pageObject.SelectAnotherLoginWay();

                var code = configProvider("code");
                await pageObject.EnterCode(code);

                var requiresEmail = await pageObject.SendEmailVerification();
                if (requiresEmail)
                {
                    var emailCode = configProvider("email_code");
                    await pageObject.EnterCode(emailCode);
                }

                if (!await pageObject.RequiresLogin())
                {
                    await browser.StorageStateAsync(new BrowserContextStorageStateOptions() { Path = Environment.GetEnvironmentVariable("Session_Storage") });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to authorize");
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
        
        page.SetDefaultTimeout(5000);
        await page.AddInitScriptAsync("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
        await page.GotoAsync(url.ToString());
        
        try
        {
            PageObject pageObject = new(page);
            
            var priceString = await pageObject.GetPrice();
            var priceValue = ScrapperUtils.GetPriceValueWithoutCurrency(priceString);

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