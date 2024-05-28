using NATS.Client;
using System.Text;
using System.Text.Json;

namespace EventsLogger
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
    public class Program
    {
        public static void Main(string[] args)
        {
            ConnectionFactory cf = new ConnectionFactory();
            IConnection c = cf.CreateConnection();

            var rankSubscriber = c.SubscribeAsync("valuator.logs.events.rank", "events_logger", (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                TextData? info = JsonSerializer.Deserialize<TextData>(data);
                Console.WriteLine($"1. Rank\n2. RecordId = {info?.id}\n3. {info?.data}");
                
            });
            rankSubscriber.Start();

            var similaritySubcriber = c.SubscribeAsync("valuator.logs.events.similarity", "events_logger", (sender, args) =>
            {
                string data = Encoding.UTF8.GetString(args.Message.Data);
                TextData? info = JsonSerializer.Deserialize<TextData>(data);
                Console.WriteLine($"1. Similarity\n2. RecordId = {info?.id}\n3. {info?.data}");
                
            });
            similaritySubcriber.Start();

            Console.WriteLine("Press Enter to exit(EventsLogger)");
            Console.ReadLine();

        }
    }

}