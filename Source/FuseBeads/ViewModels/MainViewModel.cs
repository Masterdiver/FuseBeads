using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FuseBeads.Application.DTOs;
using FuseBeads.Application.Services;
using FuseBeads.Domain.Interfaces;
using System.Collections.ObjectModel;

namespace FuseBeads.ViewModels;

#pragma warning disable MVVMTK0045
public partial class MainViewModel : ObservableObject
{
    private readonly IPatternService _patternService;
    private readonly IPrintRenderer _printRenderer;
    private PatternResult? _lastPatternResult;

    public MainViewModel(IPatternService patternService, IPrintRenderer printRenderer)
    {
        _patternService = patternService;
        _printRenderer = printRenderer;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPattern))]
    private ImageSource? _patternImageSource;

    [ObservableProperty]
    private ImageSource? _originalImageSource;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Wähle ein Bild aus, um ein Bügelperlenmuster zu erstellen.";

    [ObservableProperty]
    private int _gridWidth = 29;

    [ObservableProperty]
    private int _gridHeight = 29;

    [ObservableProperty]
    private int _totalBeads;

    [ObservableProperty]
    private int _totalColors;

    public bool HasPattern => PatternImageSource is not null;

    public ObservableCollection<ColorInfoViewModel> ColorInfos { get; } = [];

    [RelayCommand]
    private async Task PickImageAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Bild auswählen",
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
                return;

            IsBusy = true;
            StatusText = "Bild wird verarbeitet...";

            // Show the original image
            var originalStream = await result.OpenReadAsync();
            OriginalImageSource = ImageSource.FromStream(() =>
            {
                var ms = new MemoryStream();
                originalStream.CopyTo(ms);
                ms.Position = 0;
                originalStream.Position = 0;
                return ms;
            });

            // Generate pattern
            await using var stream = await result.OpenReadAsync();
            var settings = new PatternSettings
            {
                Width = GridWidth,
                Height = GridHeight,
                BeadSizePx = 20
            };

            var patternResult = await _patternService.GeneratePatternAsync(stream, settings);

            // Update pattern image
            PatternImageSource = ImageSource.FromStream(() => new MemoryStream(patternResult.PatternImage));

            // Update color infos
            ColorInfos.Clear();
            foreach (var info in patternResult.ColorInfos)
            {
                ColorInfos.Add(new ColorInfoViewModel
                {
                    ColorName = info.ColorName,
                    HexCode = info.HexCode,
                    Count = info.Count,
                    Percentage = info.Percentage,
                    DisplayColor = Color.FromArgb(info.HexCode)
                });
            }

            TotalBeads = patternResult.TotalBeads;
            TotalColors = patternResult.ColorInfos.Count;
            _lastPatternResult = patternResult;
            StatusText = $"Muster erstellt: {GridWidth}×{GridHeight} = {TotalBeads} Perlen, {TotalColors} Farben";
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task PrintAsync()
    {
        if (_lastPatternResult is null)
            return;

        try
        {
            IsBusy = true;
            StatusText = "Druckseite wird erstellt...";

            byte[] printImage = await Task.Run(() =>
                _printRenderer.RenderPrintPage(_lastPatternResult.Pattern, 20));

            var filePath = Path.Combine(FileSystem.CacheDirectory, "Bugelperlen-Druck.png");
            await File.WriteAllBytesAsync(filePath, printImage);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Bügelperlen-Muster drucken",
                File = new ShareFile(filePath)
            });

            StatusText = "Druckseite erstellt.";
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler beim Drucken: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public partial class ColorInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string _colorName = string.Empty;

    [ObservableProperty]
    private string _hexCode = string.Empty;

    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    private Color _displayColor = Colors.Transparent;
}
#pragma warning restore MVVMTK0045
