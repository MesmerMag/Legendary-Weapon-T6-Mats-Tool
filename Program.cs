using Newtonsoft.Json.Linq;
using System.Net;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GW2_Legendary_Mats
{
    internal class Program
    {
        private static string _apiKey = "";
        private static WebClient _httpClient;

        private static string _character;

        private static JArray _bankItems;
        private static JObject _charItems;
        private static int _totalStoneRequired, _totalDustRequired, _totalEcto;

        private static void RequestApiKey(bool forceQuestion = false)
        {
            var filename = Path.Combine(Directory.GetCurrentDirectory(), "_api_key.txt");

            void Ask()
            {
                Console.WriteLine("Enter your GW2 API key:");
                var key = Console.ReadLine();
                File.WriteAllText(filename, key);
                _apiKey = key;
            }

            if (forceQuestion)
            {
                Ask();
            }
            else
            {
                try
                {
                    _apiKey = File.ReadAllText(filename, Encoding.UTF8);
                }
                catch (FileNotFoundException)
                {
                    Ask();
                }
            }
        }

        private static void RequestCharacter()
        {
            string charactersJson = null;
            while (charactersJson == null)
            {
                var charactersUrl = $"https://api.guildwars2.com/v2/characters?access_token={_apiKey}";
                try
                {
                    charactersJson = _httpClient.DownloadString(charactersUrl);
                }
                catch (WebException exception)
                {
                    Console.Error.WriteLine(exception.Message);
                    RequestApiKey(true);
                }
            }

            var characters = JArray.Parse(charactersJson);

            if (characters.Count < 1)
            {
                Console.Error.WriteLine("Unable to fetch character information.");
                Environment.Exit(1);
            }

            for (var key = 0; key < characters.Count; key++)
            {
                Console.WriteLine($"{key}: {characters[key]}");
            }

            string numberInput = null;
            int number;
            while (numberInput == null ||
                   !int.TryParse(numberInput, out number) ||
                   number < 0 ||
                   number >= characters.Count)
            {
                Console.WriteLine("What character number?");
                numberInput = Console.ReadLine();
            }

            _character = (string) characters[number];
        }

        private static void Main(string[] args)
        {
            _httpClient = new WebClient();

            RequestApiKey();
            RequestCharacter();

            var bankUrl =
                $"https://api.guildwars2.com/v2/account/materials?access_token={_apiKey}";
            var characterUrl =
                $"https://api.guildwars2.com/v2/characters/{_character}/inventory?access_token={_apiKey}";

            _bankItems = JArray.Parse(_httpClient.DownloadString(bankUrl));
            _charItems = JObject.Parse(_httpClient.DownloadString(characterUrl));

            Console.Clear();
            Console.WriteLine("\r\n");
            DisplayInfo(" Vial of Potent Blood │ ", 24294, 24295, false);
            DisplayInfo(" Large Bone           │ ", 24341, 24358, false);
            DisplayInfo(" Large Claw           │ ", 24350, 24351, false);
            DisplayInfo(" Large Fang           │ ", 24356, 24357, false);
            DisplayInfo(" Large Scales         │ ", 24288, 24289, false);
            DisplayInfo(" Intricate Totem      │ ", 24299, 24300, false);
            DisplayInfo(" Potent Venom Sac     │ ", 24282, 24283, true);
            Console.WriteLine("\r\n");

            _totalDustRequired += 250;
            _totalStoneRequired -= ItemQty(20796);
            Console.WriteLine(_totalStoneRequired.ToString().PadLeft(5) + " Philosopher's Stone");

            var numDust = ItemQty(24277);
            if (numDust < _totalDustRequired)
            {
                _totalDustRequired -= numDust;
            }
            else
            {
                _totalDustRequired = 0;
            }

            _totalEcto = (int) (_totalDustRequired / 1.85) + 250;

            var numEcto = ItemQty(19721);
            if (numEcto < _totalEcto)
            {
                _totalEcto -= numEcto;
            }
            else
            {
                _totalEcto = 0;
            }

            Console.WriteLine(
                $"{_totalDustRequired.ToString().PadLeft(5)} Crystalline Dust or {ConvertToStacks(_totalDustRequired)}");

            Console.WriteLine(
                $"{_totalEcto.ToString().PadLeft(5)} Globs of Ectoplasm or {ConvertToStacks(_totalEcto)}\r\n");

            Console.WriteLine(" ** Note **" + "\r\n");
            Console.WriteLine(
                " Set aside 250 Crystalline Dust for the Gift of Magic and use the remainder for material conversion");
            Console.WriteLine(
                " Set aside 250 Globs of Ectoplasm for the Gift of Fortune and salvage the remainder for Crystalline Dust");
            Console.ReadLine();
        }

        private static void DisplayInfo(string name, int t5, int t6, bool endOfList)
        {
            var total = 0;
            var needed = 0;

            var tt5 = ItemQty(t5);
            var tt6 = ItemQty(t6);

            if (tt6 < 250)
            {
                total = (250 - tt6) / 5 * 50;
            }

            if (total > tt5)
            {
                needed = total - tt5;
            }

            var stacks = ConvertToStacks(needed);
            var stones = (total / 50) * 5;
            var dust = stones;

            var text = name + total.ToString().PadLeft(5) + " Total │ " + needed.ToString().PadLeft(5) + " Needed │ ";
            text += stacks.PadRight(13) + " │ " + stones.ToString().PadLeft(3) + " Philosopher's Stone │ " +
                    dust.ToString().PadLeft(3) + " Crystalline Dust";

            var lineBreak = "";
            if (endOfList == false)
            {
                lineBreak = new string('─', 22) + "┼" + new string('─', 13) + "┼" + new string('─', 14) + "┼" +
                            new string('─', 15) + "┼" +
                            new string('─', 25) + "┼" + new string('─', 21);
            }

            _totalStoneRequired += stones;
            _totalDustRequired += dust;

            Console.WriteLine(text);
            Console.WriteLine(lineBreak);
        }

        private static int ItemQty(int id)
        {
            var result = 0;

            foreach (var item in _bankItems)
            {
                if ((int) item["id"] == id)
                {
                    result += (int) item["count"];
                }
            }

            var material = from bag in _charItems["bags"]
                where bag.HasValues
                from item in bag["inventory"]
                where item.HasValues
                where (int) item["id"] == id
                select (int) item["count"];

            result += material.Sum();

            return result;
        }

        private static string ConvertToStacks(int amount)
        {
            var fullNumber = (double) amount / 250;
            var remainder = (fullNumber - Math.Truncate(fullNumber)) * 250;
            var stacks = Math.Truncate(fullNumber) + " Stacks " + remainder.ToString("###");

            return stacks;
        }
    }
}