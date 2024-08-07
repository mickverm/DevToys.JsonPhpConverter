using DevToys.Api;
using DevToys.JsonPhpConverter.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevToys.JsonPhpConverter.Helpers;

internal static partial class PhpHelper
{
    public static async ValueTask<ResultInfo<string>> ConvertAsync(
        string input,
        Indentation indentation,
        Quote quote,
        bool trailingCommas,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await TaskSchedulerAwaiter.SwitchOffMainThreadAsync(cancellationToken);

        var conversionResult = ConvertFromJson(input, indentation, quote, trailingCommas, logger, cancellationToken);
        if (!conversionResult.HasSucceeded && string.IsNullOrWhiteSpace(conversionResult.Data))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(Resources.Resources.InvalidJson, false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return conversionResult;
    }

    private static ResultInfo<string> ConvertFromJson(
        string? input,
        Indentation indentation,
        Quote quote,
        bool trailingCommas,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new(string.Empty, true);
        }

        try
        {
            var json = JsonDocument.Parse(input, new()
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            if (json is null)
            {
                return new(string.Empty, false);
            }

            int? indent = indentation switch
            {
                Indentation.TwoSpaces => 2,
                Indentation.FourSpaces => 4,
                Indentation.Tabs => null,
                _ => throw new NotSupportedException(),
            };

            var config = new PhpConversionConfig
            {
                Indentation = indent,
                SingleQuote = quote is Quote.SingleQuote,
                TrailingCommas = trailingCommas
            };

            string phpCode = ConvertToPhp(json.RootElement, config);
            if (string.IsNullOrWhiteSpace(phpCode))
            {
                return new(string.Empty, false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new("<?php\n\n$json = " + phpCode + ";\n");
        }
        catch (JsonException ex)
        {
            return new(ex.Message, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yaml to Json Converter");
            return new(string.Empty, false);
        }
    }

    public class PhpConversionConfig
    {
        public int? Indentation { private get; set; }
        public bool TrailingCommas { private get; set; }
        public bool SingleQuote { private get; set; }

        public string GetIndent(int depth)
        {
            return Indentation is int indent
                ? new(' ', indent * depth)
                : new('\t', depth);
        }

        public string TrailingComma => TrailingCommas ? "," : "";

        public string Quote => SingleQuote ? "'" : "\"";
    }

    private static string ConvertToPhp(JsonElement element, PhpConversionConfig config, int depth = 0)
    {
        string indent = config.GetIndent(depth);
        string innerIndent = config.GetIndent(depth + 1);
        string comma = config.TrailingComma;
        string quote = config.Quote;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var items = new List<string>();
                    foreach (JsonProperty item in element.EnumerateObject())
                    {
                        items.Add($"{innerIndent}{quote}{item.Name}{quote} => {ConvertToPhp(item.Value, config, depth + 1)}");
                    }

                    if (items.Count == 0)
                    {
                        return "[]";
                    }

                    return $"[\n{string.Join($",\n", items)}{comma}\n{indent}]";
                }

            case JsonValueKind.Array:
                {
                    var items = new List<string>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        items.Add($"{innerIndent}{ConvertToPhp(item, config, depth + 1)}");
                    }

                    if (items.Count == 0)
                    {
                        return "[]";
                    }

                    return $"[\n{string.Join($",\n", items)}{comma}\n{indent}]";
                }

            case JsonValueKind.String:
                return quote + element.GetString() + quote;

            case JsonValueKind.Number:
                return element.GetRawText();

            case JsonValueKind.True:
                return "true";

            case JsonValueKind.False:
                return "false";

            case JsonValueKind.Null:
                return "null";

            default:
                throw new NotSupportedException($"The JsonValueKind '{element.ValueKind}' is not supported.");
        }
    }
}
