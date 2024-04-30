using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetTopologySuite.GeometriesGraph.Index;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
        _redis = redis;
    }

    double CalculateRank(string text)
    {
        if (text == null) return 0;

        int numOfLetters = 0;
        foreach (char ch in text)
        {
            if (Char.IsBetween(ch, 'a', 'z')
                || Char.IsBetween(ch, 'A', 'Z')
                || Char.IsBetween(ch, 'а', 'я')
                || Char.IsBetween(ch, 'А', 'Я'))
            {
                numOfLetters++;
            }
        }

        double rank = (double)(text.Length - numOfLetters) / text.Length;

        return rank;
    }

    double CalculateSimilarity(string text)
    {
        int similarity = 0;

        var keys = _redis.GetServer(_redis.GetEndPoints().First()).Keys();

        foreach (string key in keys)
        {
            if (key.Contains("TEXT-") && _db.StringGet(key) == text)
            {
                similarity = 1;
            }
        }

        return similarity;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);
        _db.StringSet(rankKey, rank.ToString());

        string similarityKey = "SIMILARITY-" + id;
        double similarity = CalculateSimilarity(text);
        _db.StringSet(similarityKey, similarity.ToString());

        string textKey = "TEXT-" + id;
        _db.StringSet(textKey, text);

        return Redirect($"summary?id={id}");
    }
}
