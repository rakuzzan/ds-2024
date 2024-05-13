﻿using NATS.Client;
using NATS.Client.Internals.SimpleJSON;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;


class RankCalculator
{
    private static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    private static readonly IConnection natsConnection = new ConnectionFactory().CreateConnection("127.0.0.1:4222");

    private const string SUBJECT = "RankCalculate";

    static void Main(string[] args)
    {
        var rankCalculated = natsConnection.SubscribeAsync(SUBJECT, (sender, args) =>
        {
            var messageBytes = args.Message.Data;
            string id = Encoding.UTF8.GetString(messageBytes);

            string text = redis.GetDatabase().StringGet("TEXT-" + id);
            if (text != null)
            { 
                double rank = CalculateRank(text);

                SaveRankToRedis(id, rank);

                Info rankStruct = new Info();
                rankStruct.Id = id;
                rankStruct.Data = rank;

                string json= JsonSerializer.Serialize(rankStruct);
                messageBytes = Encoding.UTF8.GetBytes(json);
                natsConnection.Publish(SUBJECT + "_logger", messageBytes);
            }
        });
        rankCalculated.Start();
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
        Console.WriteLine("Сохранена запись: {0} {1}", rankKey, rank);
    }

    
}
public class Info
{
    public string Id { get; set; }
    public double Data { get; set; }
}