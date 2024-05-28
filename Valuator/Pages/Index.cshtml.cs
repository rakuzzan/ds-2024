using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using static System.Net.Mime.MediaTypeNames;
using NATS.Client;
using System.Text;
using System.Text.Json;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redisConnection;
    private readonly IDatabase _db;
    
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

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redisConnection)
    {
        _logger = logger;
        _redisConnection = redisConnection;
        _db = _redisConnection.GetDatabase();   
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text, string country)
    {
        _logger.LogDebug(text);
        _logger.LogDebug(country);

        string id = Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(text))
        {
            return Redirect($"index");
        }

        string dbEnvironmentVariable = $"DB_{country}";

        _db.StringSet(id, country);

        string? dbConnection = Environment.GetEnvironmentVariable(dbEnvironmentVariable);

        if (dbConnection != null) 
        {
            IDatabase savingDb = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(dbConnection)).GetDatabase();

            string similarityKey = "SIMILARITY-" + id;
            double similarity = CalculateSimilarity(text, dbConnection);
            savingDb?.StringSet(similarityKey, similarity);
            Console.WriteLine($"LOOKUP: {id}, {country}");

            string textKey = "TEXT-" + id;
            savingDb?.StringSet(textKey, text);
            Console.WriteLine($"LOOKUP: {id}, {country}");
            
            CancellationTokenSource cts = new CancellationTokenSource();

            ConnectionFactory cf = new ConnectionFactory();

            using (IConnection c = cf.CreateConnection())
            {
                IdAndCountryOfText structData = new IdAndCountryOfText(country, id);

                string infoJson = JsonSerializer.Serialize(structData);

                byte[] data = Encoding.UTF8.GetBytes(infoJson);

                c.Publish("valuator.processing.rank", data);

                TextData textData = new TextData(id, similarity);

                infoJson = JsonSerializer.Serialize(textData);

                data = Encoding.UTF8.GetBytes(infoJson);

                c.Publish("valuator.logs.events.similarity", data);

                c.Drain();

                c.Close();
            }

            cts.Cancel();

            return Redirect($"summary?id={id}&country={country}");
        }
        return Redirect($"index");
    }

    private double CalculateSimilarity(string text, string? dbConnection)
    {
        if (dbConnection == null)
        {
            return 0.0;
        }

        ConfigurationOptions redisConfiguration = ConfigurationOptions.Parse(dbConnection);
        ConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConfiguration);
        IDatabase savingDb = redisConnection.GetDatabase();

        var allKeys = redisConnection.GetServer(dbConnection).Keys();
        double similarity = 0.0;
        foreach (var key in allKeys)
        {
            if (key.ToString().Substring(0, 4) != "TEXT")
            {
                continue;
            }
            string? dbText = savingDb?.StringGet(key);
            if (dbText == text)
            {
                similarity = 1.0;
            }
        }
        return similarity;
    }
}