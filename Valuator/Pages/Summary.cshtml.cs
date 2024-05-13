﻿using System;
using System.Collections.Generic;
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
    private readonly ConnectionMultiplexer _redis;//создатть отдельный класс для работы с ним

    public SummaryModel(ILogger<SummaryModel> logger)
    {
        _logger = logger;
        _redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        IDatabase db = _redis.GetDatabase();

        string rankKey = "RANK-" + id;
        if (db.KeyExists(rankKey))
        {
            string rankString = db.StringGet(rankKey);
            _logger.LogDebug($"Raw Rank String: {rankString}");
            try
            {
                Rank = Convert.ToDouble(rankString, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                // Обработка случая, когда значение не может быть преобразовано в double
                _logger.LogError($"Ошибка преобразования значения {rankKey} в тип double: {ex.Message}");
            }
        }

        string similarityKey = "SIMILARITY-" + id;
        if (db.KeyExists(similarityKey))
        {
            int similarity;
            if (int.TryParse(db.StringGet(similarityKey), out similarity))
            {
                Similarity = similarity;
            }
            else
            {
                // Обработка случая, когда значение не может быть преобразовано в int
                _logger.LogError($"Ошибка преобразования значения {similarityKey} в тип int");
            }
        }
    }
}