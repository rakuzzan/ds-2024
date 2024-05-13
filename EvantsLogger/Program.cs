using NATS.Client;
using System.Text;
using System.Text.Json;

public class Info
{
    public string Id { get; set; }
    public double Data { get; set; }
}

class EventsLogger
{
    private static readonly IConnection natsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");
    private const string SUBJECT_SIMILARITY = "SimilarityCalculate_logger";
    private const string SUBJECT_RANK = "RankCalculate_logger";

    static void Main(string[] args)
    {

        var similaritySubscriber = natsConnection.SubscribeAsync(SUBJECT_SIMILARITY, "events_logger", (sender, args) =>
        {
            var messageBytes = args.Message.Data;
            string data = Encoding.UTF8.GetString(messageBytes);
            Info? info = JsonSerializer.Deserialize<Info>(data);
            Console.WriteLine($"{SUBJECT_SIMILARITY}: \n ID: {info?.Id} \n Similarity: {info?.Data} ");
        });
        similaritySubscriber.Start();

        var rankSubscriber = natsConnection.SubscribeAsync(SUBJECT_RANK, "events_logger", (sender, args) =>
        {

            var messageBytes = args.Message.Data;
            string data = Encoding.UTF8.GetString(messageBytes);
            Info? info = JsonSerializer.Deserialize<Info>(data);

            Console.WriteLine($"{SUBJECT_RANK}: \n ID: {info?.Id} \n Rank: {info?.Data}");

        });
        rankSubscriber.Start();


        Console.WriteLine("Events Logger запущен. Ожидание сообщений...");
        Console.ReadLine();
    }
}