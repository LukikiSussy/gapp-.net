//TODO:
//u cookies.json delete nejde file vymazat kdyz je in use - vytvorit soubor s variable, ktera bude trackovat jestli je potreba file pri pristim spusteni znovu vytvorit
//cela getTimetable funkce
//logic na vytvareni cookies.json a pouzivani cookies.json, kdyz uz existuje




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
using Newtonsoft.Json.Linq;
using Microsoft.Maui.Controls.Shapes;
using Newtonsoft.Json.Serialization;

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

        private static async Task GetTimetable()
        {
            using var playwright = await Playwright.CreateAsync();

            var browser = await playwright.Webkit.LaunchAsync(new() { Headless = true });

            string fileName = "cookies.json";
            string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);

            var context = await browser.NewContextAsync(new() { StorageState = File.ReadAllText(filePath) });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Globals.Constants.Url);

            await ScrapeTimetable(page);
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

            string fileName = "cookies.json";
            string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);


            using FileStream outputStream = System.IO.File.OpenWrite(filePath);
            using StreamWriter streamWriter = new StreamWriter(outputStream);

            //tady to musi byt takhle fugly, jinak by ty veci v tom jsonu meli velky pismena na zacatku a nefungovalo by to
            string json = $"{{\"cookies\": {JsonConvert.SerializeObject(Cookies, new JsonSerializerSettings { Formatting = Formatting.Indented, ContractResolver = new CamelCasePropertyNamesContractResolver() })}}}";

            await streamWriter.WriteAsync(json);


            await ScrapeTimetable(page);
        }

        //Stackoverflow inspired code
        private static bool IsValidJson(string path)
        {
            try 
            {
                File.ReadAllText(path);
            }
            catch
            {
                return false;
            }

            string strInput = File.ReadAllText(path);


            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CounterBtn_Clicked(object sender, EventArgs e)
        {
            string fileName = "cookies.json";
            string filePath = System.IO.Path.Combine(Globals.Constants.mainDir, fileName);
            if (!IsValidJson(filePath))
            {
                Task func = CreateCookiesJson();
            }
            else
            {
                Task func = GetTimetable();
            }
        }
    }
}