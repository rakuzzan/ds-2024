using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;

    public SummaryModel(ILogger<SummaryModel> logger)
    {
        _logger = logger;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id, string country)
    {
        _logger.LogDebug(id);
        //TODO: проинициализировать свойства Rank и Similarity значениями из БД
        string dbEnvironmentVariable = $"DB_{country}";
        string? dbConnection = Environment.GetEnvironmentVariable(dbEnvironmentVariable);
        if (dbConnection == null) 
        {
            return;
        }
        IDatabase db = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(dbConnection)).GetDatabase();

        string? rankString = db.StringGet($"RANK-{id}");
        string? similarityString = db.StringGet($"SIMILARITY-{id}");

        if (similarityString == null || rankString == null) 
        {
            return;
        }

        Rank = double.Parse(rankString, System.Globalization.CultureInfo.InvariantCulture);
        Similarity = double.Parse(similarityString, System.Globalization.CultureInfo.InvariantCulture);

    }
}