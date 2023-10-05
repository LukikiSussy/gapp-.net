using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace gapp

{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
        }

        public static class Globals
        {
            public static class Constants
            {
                public const string Url = "https://gevo.edookit.net/user/login";
            }
        }

        private static async Task CreateCookiesJson()
        {
            using var playwright = await Playwright.CreateAsync();

            var browser = await playwright.Webkit.LaunchAsync(new() { Headless = false });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Globals.Constants.Url);
            await page.GetByText("Přihlásit přes").ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Google" }).ClickAsync();
            await page.WaitForURLAsync("https://gevo.edookit.net/", new() { Timeout = 900000 });

            var Cookies = await context.CookiesAsync();

            var CookiesJson = new Dictionary<string, Object>(){};
            CookiesJson.Add("cookies", Cookies);

            string mainDir = FileSystem.Current.AppDataDirectory;
            string fileName = "cookies.json";
            string filePath = System.IO.Path.Combine(mainDir, fileName);

            using FileStream outputStream = System.IO.File.OpenWrite(filePath);
            using StreamWriter streamWriter = new StreamWriter(outputStream);

            string json = $"{{\"cookies\": {JsonConvert.SerializeObject(Cookies, Formatting.Indented)}}}";

            await streamWriter.WriteAsync(json);


            await ScrapeTimetable(page);
        }


        private static async Task ScrapeTimetable(IPage page)
        {
            if (await page.GetByText("Přihlásit přes").IsVisibleAsync())
            {
                await page.GetByText("Přihlásit přes").ClickAsync();
                // musi byt kvuli edge case kde to redirectne rovnou
                if (await page.GetByRole(AriaRole.Button, new() { Name = "Google" }).IsVisibleAsync())
                {
                    await page.GetByRole(AriaRole.Button, new() { Name = "Google" }).ClickAsync();
                    // OS REMOVE
                    try
                    {
                        File.Delete("cookies.json");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);
                        Trace.WriteLine("cookies.json not found - cookies missing");
                    }
                }
            }

            // tohle taky nekdy to throwne error 500 (zustane to na random strance)
            if (page.Url != "https://gevo.edookit.net/user/login")
            {
                try
                {
                    File.Delete("cookies.json");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    Trace.WriteLine("cookies.json not found - page stuck on wrong url");
                }
            }


            foreach (var li in await page.Locator(".hoverLesson").AllInnerTextsAsync())
            {
                Trace.WriteLine(li);
            }

        }

        private static async Task getTimetableAsync()
        {
            using var playwright = await Playwright.CreateAsync();

            var browser = await playwright.Webkit.LaunchAsync(new() { Headless = false });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(Globals.Constants.Url);

            await ScrapeTimetable(page);
        }






        public static async Task Scraper()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Webkit.LaunchAsync(new() { Headless = false });

            var context = await browser.NewContextAsync();

            var page = await context.NewPageAsync();
            await page.GotoAsync("https://bing.com");
            await context.CloseAsync();


        }

        private void CounterBtn_Clicked(object sender, EventArgs e)
        {
            Task func = CreateCookiesJson();
        }
    }
}