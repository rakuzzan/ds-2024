using NATS.Client;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;

public class MessageModel
{
    public string Text { get; set; }
    public string Id { get; set; }
}

class RankCalculator
{
    private static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    private static readonly IConnection natsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");

    static void Main(string[] args)
    {
        // Подписка на получение сообщений о тексте
        natsConnection.SubscribeAsync("text.processing", (sender, args) =>
        {
            var messageBytes = args.Message.Data;

            var messageObject = JsonConvert.DeserializeObject<MessageModel>(Encoding.UTF8.GetString(messageBytes));

            string text = messageObject.Text;
            string id = messageObject.Id;

            double rank = CalculateRank(text);

            // Сохранение ранга в базе данных
            SaveRankToRedis(id, rank);
        });

        // Ожидание сообщений
        Console.WriteLine("RankCalculator запущен. Ожидание сообщений...");
        Console.ReadLine();
    }

    static double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int nonAlphabeticCount = text.Count(c => !IsAlphabetic(c));
        int totalCharacters = text.Length;

        return ((double)nonAlphabeticCount / totalCharacters);
    }

    static bool IsAlphabetic(char c)
    {
        return char.IsLetter(c) || c == ' ';
    }

    static void SaveRankToRedis(string id, double rank)
    {
        IDatabase db = redis.GetDatabase();
        // Здесь код для сохранения ранга в Redis
        string rankKey = "RANK-" + id;
        db.StringSet(rankKey, rank);
    }
}