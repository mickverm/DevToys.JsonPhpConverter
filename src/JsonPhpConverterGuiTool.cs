using DevToys.Api;
using Mickverm.DevToys.JsonPhpConverter.Helpers;
using Mickverm.DevToys.JsonPhpConverter.Models;
using Mickverm.DevToys.JsonPhpConverter.Resources;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;

namespace Mickverm.DevToys.JsonPhpConverter;

[Export(typeof(IGuiTool))]
[Name("JsonPhpConverter")]
[ToolDisplayInformation(
    IconFontName = "DevToys-Tools-Icons",
    IconGlyph = '\u0109',
    GroupName = PredefinedCommonToolGroupNames.Converters,
    ResourceManagerAssemblyIdentifier = nameof(ResourceAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.JsonPhpConverter.Resources.Resources",
    ShortDisplayTitleResourceName = nameof(Resources.Resources.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(Resources.Resources.LongDisplayTitle),
    DescriptionResourceName = nameof(Resources.Resources.Description),
    AccessibleNameResourceName = nameof(Resources.Resources.AccessibleName))]
[Order(Before = "JsonTableConverter")]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Json)]
internal sealed class JsonPhpConverterGuiTool : IGuiTool, IDisposable
{
    private static readonly SettingDefinition<Indentation> indentationMode
        = new(name: $"{nameof(Resources.Resources)}.{nameof(indentationMode)}", defaultValue: Indentation.FourSpaces);

    private static readonly SettingDefinition<Quote> quoteMode
        = new(name: $"{nameof(Resources.Resources)}.{nameof(quoteMode)}", defaultValue: Quote.SingleQuote);

    private static readonly SettingDefinition<bool> trailingCommas
        = new(name: $"{nameof(Resources.Resources)}.{nameof(trailingCommas)}", defaultValue: true);

    private enum GridColumn
    {
        Content
    }

    private enum GridRow
    {
        Header,
        Content,
        Footer
    }

    private readonly DisposableSemaphore _semaphore = new();
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IUIMultiLineTextInput _inputTextArea = MultiLineTextInput("json-to-php-input-text-area")
        .Language("json");
    private readonly IUIMultiLineTextInput _outputTextArea = MultiLineTextInput("json-to-php-output-text-area")
        .Language("php");

    private CancellationTokenSource? _cancellationTokenSource;

    [ImportingConstructor]
    public JsonPhpConverterGuiTool(ISettingsProvider settingsProvider)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;
    }

    internal Task? WorkTask { get; private set; }

    public UIToolView View
        => new(
            isScrollable: true,
            Grid()
                .ColumnLargeSpacing()
                .RowLargeSpacing()
                .Rows(
                    (GridRow.Header, Auto),
                    (GridRow.Content, new UIGridLength(1, UIGridUnitType.Fraction))
                )
                .Columns(
                    (GridColumn.Content, new UIGridLength(1, UIGridUnitType.Fraction))
                )
            .Cells(
                Cell(
                    GridRow.Header,
                    GridColumn.Content,
                    Stack().Vertical().WithChildren(
                        Label()
                        .Text(Resources.Resources.Configuration),
                        Setting("json-to-php-text-indentation-setting")
                        .Icon("FluentSystemIcons", '\uF6F8')
                        .Title(Resources.Resources.IndentationTitle)
                        .Description(Resources.Resources.Indentation)
                        .Handle(
                            _settingsProvider,
                            indentationMode,
                            OnIndentationModelChanged,
                            Item(Resources.Resources.TwoSpaces, Indentation.TwoSpaces),
                            Item(Resources.Resources.FourSpaces, Indentation.FourSpaces),
                            Item(Resources.Resources.Tabs, Indentation.Tabs)),
                        Setting("json-to-php-quote-setting")
                        .Icon("FluentSystemIcons", '\u0022')
                        .Title(Resources.Resources.QuotesTitle)
                        .Description(Resources.Resources.Quotes)
                        .Handle(
                            _settingsProvider,
                            quoteMode,
                            OnQuoteModelChanged,
                            Item(Resources.Resources.SingleQuote, Quote.SingleQuote),
                            Item(Resources.Resources.DoubleQuote, Quote.DoubleQuote)),
                        Setting()
                        .Icon("FluentSystemIcons", '\u002C')
                        .Title(Resources.Resources.TrailingCommasTitle)
                        .Description(Resources.Resources.TrailingCommas)
                        .Handle(
                            _settingsProvider,
                            trailingCommas,
                            OnTrailingCommasChanged)
                    )
                ),
                Cell(
                    GridRow.Content,
                    GridColumn.Content,
                    SplitGrid()
                        .Vertical()
                        .WithLeftPaneChild(
                            _inputTextArea
                                .Title(Resources.Resources.Input)
                                .OnTextChanged(StartConvert))
                        .WithRightPaneChild(
                            _outputTextArea
                                .Title(Resources.Resources.Output)
                                .ReadOnly()
                                .Extendable())
                )
            )
        );

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        if (dataTypeName == PredefinedCommonDataTypeNames.Json && parsedData is string json)
        {
            _inputTextArea.Text(json);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _semaphore.Dispose();
    }

    private void OnIndentationModelChanged(Indentation indentationMode)
    {
        StartConvert();
    }

    private void OnQuoteModelChanged(Quote quoteMode)
    {
        StartConvert();
    }

    private void OnTrailingCommasChanged(bool trailingCommas)
    {
        StartConvert();
    }

    private void StartConvert(string? text = null)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        WorkTask = ConvertAsync(
            text ?? _inputTextArea.Text,
            _settingsProvider.GetSetting(indentationMode),
            _settingsProvider.GetSetting(quoteMode),
            _settingsProvider.GetSetting(trailingCommas),
            _cancellationTokenSource.Token);
    }

    private async Task ConvertAsync(string input, Indentation indentationMode, Quote quoteMode, bool trailingCommas, CancellationToken cancellationToken)
    {
        using (await _semaphore.WaitAsync(cancellationToken))
        {
            await TaskSchedulerAwaiter.SwitchOffMainThreadAsync(cancellationToken);

            ResultInfo<string> conversionResult = await PhpHelper.ConvertAsync(
                input,
                indentationMode,
                quoteMode,
                trailingCommas,
                _logger,
                cancellationToken);
            _outputTextArea.Text(conversionResult.Data!);
        }
    }
}
