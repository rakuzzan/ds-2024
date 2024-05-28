﻿using NATS.Client;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data.Common;

namespace RankCalculator
{
    class TextData
    {
        public TextData(string id, double data)
        {
            this.id = id;
            this.data = data;
        }
        public string id { get; set; }
        public double data { get; set; }
    }

    class IdAndCountryOfText
    {
        public IdAndCountryOfText(string country, string textId)
        {
            this.textId = textId;
            this.country = country;
        }
        public string country { get; set; }
        public string textId { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ConnectionFactory cf = new ConnectionFactory();
            IConnection c = cf.CreateConnection();

            var s = c.SubscribeAsync("valuator.processing.rank", "rank_calculator", (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                IdAndCountryOfText? structData = JsonSerializer.Deserialize<IdAndCountryOfText>(data);

                string dbEnvironmentVariable = $"DB_{structData?.country}";
                string? dbConnection = Environment.GetEnvironmentVariable(dbEnvironmentVariable);

                if (dbConnection == null)
                {
                    return;
                }

                IDatabase savingDb = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(dbConnection)).GetDatabase();

                string textKey = "TEXT-" + structData?.textId;
                string? text = savingDb?.StringGet(textKey);

                string rankKey = "RANK-" + structData?.textId;

                double rank = CalculateRank(text);

                savingDb?.StringSet(rankKey, rank);
                Console.WriteLine($"LOOKUP: {structData?.textId}, {structData?.country}");

                if (structData == null)
                {
                    return;
                }
                TextData textData = new TextData(structData.textId, rank);
                string jsonData = JsonSerializer.Serialize(textData);

                byte[] jsonDataEncoded = Encoding.UTF8.GetBytes(jsonData);

                c.Publish("valuator.logs.events.rank", jsonDataEncoded);
            });

            s.Start();

            Console.WriteLine("Press Enter to exit(RankCalculator)");
            Console.ReadLine();
        }

        static double CalculateRank(string? text)
        {
            if (text == null) 
            {
                return 0.0;
            }
           
            int totalCharacters = text.Length;
            int nonAlphabeticCharacters = 0;

            foreach (char character in text)
            {
                if (!char.IsLetter(character))
                {
                    nonAlphabeticCharacters++;
                }
            }

            double contentRank = (double)nonAlphabeticCharacters / totalCharacters;

            return contentRank;
        }
    }
}