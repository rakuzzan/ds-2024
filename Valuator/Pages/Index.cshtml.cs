using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Text.Json;
using NATS.Client;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ConnectionMultiplexer _redis;
    private readonly IConnection _natsConnection;
    private const string SUBJECT_SIMILARITY = "SimilarityCalculate_logger";
    private const string SUBJECT_RANK = "RankCalculate";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        _redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
        try
        {
            // Создание подключения к NATS
            Options options = ConnectionFactory.GetDefaultOptions();
            options.Url = "127.0.0.1:4222";
            _natsConnection = new ConnectionFactory().CreateConnection(options);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ошибка при подключении к NATS: {ex.Message}");
            throw;
        }
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);
        if (!string.IsNullOrEmpty(text))
        {

            string id = Guid.NewGuid().ToString();

            string textKey = "TEXT-" + id;
            IDatabase db = _redis.GetDatabase();
            db.StringSet(textKey, text);


            byte[] messageBytes = Encoding.UTF8.GetBytes(id);
            _natsConnection.Publish(SUBJECT_RANK, messageBytes);

            string similarityKey = "SIMILARITY-" + id;
            int res = (IsSimilitary(text, id) == true) ? 1 : 0;
            db.StringSet(similarityKey, res);

            Info similarityStruct = new Info();
            similarityStruct.Id = id;
            similarityStruct.Data = res;

            string json = JsonSerializer.Serialize(similarityStruct);
            messageBytes = Encoding.UTF8.GetBytes(json);

            _natsConnection.Publish(SUBJECT_SIMILARITY, messageBytes);
            _natsConnection.Drain();
            _natsConnection.Close();

            return Redirect($"summary?id={id}");
        }
        else
        {
            return Redirect($"index");
        }
       
    }

    public bool IsSimilitary(string text, string currentId)
    {
        foreach (var key in _redis.GetServer(_redis.GetEndPoints()[0]).Keys())
        {
            if (key.ToString().StartsWith("TEXT-") && !key.ToString().EndsWith(currentId) && !string.IsNullOrEmpty(text))
            {
                string storedText = _redis.GetDatabase().StringGet(key);
                if (text.Equals(storedText, StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Если найден дубликат, возвращаем true
                }
            }
        }

        return false;
    }


    public class Info
    {
        public string Id { get; set; }
        public double Data { get; set; }
    }
}