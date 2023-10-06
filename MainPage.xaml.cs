using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework.Internal;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using System.Text.Json;

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
#pragma warning disable CA1416 // Validate platform compatibility
                public static string mainDir = FileSystem.Current.AppDataDirectory;
#pragma warning restore CA1416 // Validate platform compatibility

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

            string fileName = "cookies.json";
            string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);


            using FileStream outputStream = System.IO.File.OpenWrite(filePath);
            using StreamWriter streamWriter = new StreamWriter(outputStream);

            string json = $"{{\"cookies\": {JsonConvert.SerializeObject(Cookies, Formatting.Indented)}}}";

            await streamWriter.WriteAsync(json);


            await ScrapeTimetable(page);
        }

        [Obsolete]
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
                        string fileName = "cookies.json";
                        string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex);
                        Trace.WriteLine("cookies.json not found - cookies missing");
                    }
                }
            }

            // tohle taky nekdy to throwne error 500 (zustane to na random strance)
            if (page.Url != "https://gevo.edookit.net/user/login" && page.Url != "https://gevo.edookit.net/")
            {
                Trace.WriteLine(page.Url);
                try
                {
                    string fileName = "cookies.json";
                    string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    Trace.WriteLine("cookies.json not found - page stuck on wrong url");
                }
            }

            List<dynamic> data = new List<dynamic>();

            int i = 0;
            foreach (var li in await page.Locator(".hoverLesson").AllInnerTextsAsync())
            {
                string[] class_ = Regex.Replace(Regex.Replace(li, @"\u00A0", " "), @"\s\s+", "\n").Split("\n");

                var scheduleJson = new {
                    Subject = class_[1],
                    Teacher = class_[2],
                    Room = class_[3],
                    Day = class_[4],
                    Time = class_[5]
                };

                data.Add(scheduleJson);

                i++;
            }


            string timetableName = "timetable.json";
            string timetablePath = System.IO.Path.Combine(Globals.Constants.mainDir, timetableName);

            Trace.WriteLine(timetablePath);

            using FileStream outputStream = System.IO.File.OpenWrite(timetablePath);
            using StreamWriter streamWriter = new StreamWriter(outputStream);

            var opt = new JsonSerializerOptions() { WriteIndented = true };
            await streamWriter.WriteAsync(System.Text.Json.JsonSerializer.Serialize<List<dynamic>>(data, opt));

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