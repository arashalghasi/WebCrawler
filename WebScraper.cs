
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

public class WebScraper : IWebScraper
{

    public WebScraper(ILogger<WebScraper> log, IConfiguration config)
    {
        Log = log;
        Config = config;
    }

    public void Run()
    {
        try
        {
            Log.LogTrace("Program has started.");

            Console.WriteLine(@"Please Insert the full path of the folder that you want to save the result: (Like : C:\Users\arash\Desktop\SimpleWebScraper\) ");

            string jsonOutpath = string.Empty;

            do
            {
                jsonOutpath = Console.ReadLine();
            } while (string.IsNullOrEmpty(jsonOutpath));

            Console.WriteLine("Please Insert completely the name of json file that you want to save the result: (like : Result.json)");
            string jsonFileName = string.Empty;
            do
            {
                jsonFileName = Console.ReadLine();
            } while (string.IsNullOrEmpty(jsonFileName));

            string url = "https://www.alimentinutrizione.it/tabelle-nutrizionali/ricerca-per-ordine-alfabetico";
            var web = new HtmlWeb();
            var htmlDoc = web.Load(url);
            Log.LogInformation("The Html document loaded Perfectly.");
            var HtmlInfo = htmlDoc.DocumentNode.SelectNodes("//table[@id = 'cercatabella']//li");
            List<FoodGeneralInfo> listOfFoods = new List<FoodGeneralInfo>();

            //we add the name and the url of foods to a list
            foreach (var item in HtmlInfo)
            {
                var foodUrl = "https://www.alimentinutrizione.it" + item.ChildNodes[1].ChildNodes[1].Attributes[0].Value;
                var FoodName = item.InnerText.Trim();
                listOfFoods.Add(new FoodGeneralInfo { ItalianName = FoodName, Url = foodUrl });

            }
            int count = 1;

            Parallel.ForEach<FoodGeneralInfo>(listOfFoods, food =>
            {
                string foodUrl = food.Url;
                var htmlFood = web.Load(foodUrl);
                #region the first table
                string GeneralTableXPath = "//*[@id=\"conttableft\"]/div[1]/table";
                var foodGeneraltable = htmlFood.DocumentNode.SelectNodes(GeneralTableXPath);
                foreach (HtmlNode node in foodGeneraltable)
                {
                    if (node.ChildNodes.Count() > 1)
                    {
                        for (int j = 3; j < foodGeneraltable[0].ChildNodes.Count() - 1; j++)
                        {
                            switch (node.ChildNodes[j].ChildNodes[0].InnerText)
                            {
                                case "Categoria": food.Category = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Codice Alimento": food.FoodCode = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Nome Scientifico": food.ScientificName = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "English Name": food.EnglishName = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Parte Edibile": food.EatablePartpercentage = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Porzione": food.Portion = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Informazioni": food.Information = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                                case "Numero Campioni": food.NumberOfSamples = node.ChildNodes[j].ChildNodes[1].InnerText.Trim(); break;
                            }
                        }
                    }
                }

                #endregion
                #region the second table
                string NutTableXPath = "//*[@id=\"t3-content\"]/div[2]/article/section/table/tbody/tr";
                var foodNuttable = htmlFood.DocumentNode.SelectNodes(NutTableXPath);
                food.Nutritions = new List<Nutrition>();
                string curentCategory = string.Empty;
                foreach (var item in foodNuttable)
                {
                    if (item.Attributes["class"].Value.Contains("title"))
                    {
                        curentCategory = item.InnerText.Trim();
                    }
                    if (item.Attributes["class"].Value.Contains("corpo"))
                    {
                        food.Nutritions.Add(new Nutrition
                        {
                            Category = curentCategory,
                            Description = item.ChildNodes[0].InnerText.Trim(),
                            ValueFor100g = item.ChildNodes[2].InnerText.Replace("\u0026nbsp;", "").Trim(),
                            Procedures = item.ChildNodes[7].InnerText.Trim(),
                            DataSource = item.ChildNodes[6].InnerText.Trim(),
                            Reference = item != null
                            && item.HasChildNodes
                            && item.ChildNodes[8] != null
                            && item.ChildNodes[8].HasChildNodes
                            && item.ChildNodes[8].ChildNodes[0].Attributes.Contains("data-content")
                            ? item.ChildNodes[8].ChildNodes[0].Attributes["data-content"].Value
                            : ""
                        });
                    }
                }
                #endregion
                #region Codice Langual Table
                food.LangualCodes = new List<Langual>();
                string langualXPath = "//*[@id=\"t3-content\"]/div[2]/article/section/div[2]/div[1]/div";
                var langualtable = htmlFood.DocumentNode.SelectNodes(langualXPath);
                string info = string.Empty;
                foreach (var element in langualtable)
                {

                    for (int i = 1; i < element.ChildNodes.Count; i++)
                        food.LangualCodes.Add(new Langual
                        {
                            Id = element.ChildNodes[i].InnerText.Replace('|', ' ').Trim(),
                            Info = element.ChildNodes[i].Attributes["data-content"].Value.Trim()
                        }
                        );
                }
                #endregion
                #region  Chart
                food.ChartData = new Chart();
                string html = htmlFood.DocumentNode.InnerHtml;
                int startingIndex = html.IndexOf("['Proteine', ");
                string chartData = html.Substring(startingIndex, 100);
                string regex = @"\d+";
                var matches = Regex.Matches(chartData, regex);
                food.ChartData = new Chart
                {
                    Protein = matches[0].Value.Trim(),
                    Fat = matches[1].Value.Trim(),
                    Carbohydrate = matches[2].Value.Trim(),
                    Fiber = matches[3].Value.Trim(),
                    Alcohol = matches[4].Value.Trim()
                };
                #endregion
                Log.LogInformation($"The Food Number -->{count}<-- loaded completely");
                count++;
            });
            var option = new JsonSerializerOptions { WriteIndented = true, AllowTrailingCommas = true };
            string jsonString = JsonSerializer.Serialize(listOfFoods, option);
            File.AppendAllText(jsonOutpath + jsonFileName, jsonString);
            Log.LogInformation($"\nthe information saved in a json file with the address {jsonOutpath}");

        }
        catch (Exception e)
        {
            Log.LogError(e.Message);
        }
    }

    private readonly ILogger<WebScraper> Log;
    private readonly IConfiguration Config;

}
