using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FuseBeads.Application.DTOs;
using FuseBeads.Application.Services;
using FuseBeads.Domain.Entities;
using FuseBeads.Domain.Interfaces;
using FuseBeads.Resources.Strings;
using System.Collections.ObjectModel;

namespace FuseBeads.ViewModels;

#pragma warning disable MVVMTK0045
public partial class MainViewModel : ObservableObject
{
    private readonly IPatternService _patternService;
    private readonly IPrintRenderer _printRenderer;
    private readonly IPatternStorage _patternStorage;
    private readonly IProgressStorage _progressStorage;
    private readonly IPatternRenderer _patternRenderer;
    private readonly IFileSaver _fileSaver;
    private PatternResult? _lastPatternResult;
    private byte[]? _lastImageBytes;

    private readonly Stack<CellEdit> _undoStack = new();
    private readonly Stack<CellEdit> _redoStack = new();

    public MainViewModel(
        IPatternService patternService,
        IPrintRenderer printRenderer,
        IPatternStorage patternStorage,
        IProgressStorage progressStorage,
        IBeadColorPaletteFactory paletteFactory,
        IPatternRenderer patternRenderer,
        IFileSaver fileSaver)
    {
        _patternService = patternService;
        _printRenderer = printRenderer;
        _patternStorage = patternStorage;
        _progressStorage = progressStorage;
        _patternRenderer = patternRenderer;
        _fileSaver = fileSaver;

        foreach (var pt in paletteFactory.AvailablePalettes)
            PaletteTypes.Add(pt);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPattern))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowInstructionsCommand))]
    [NotifyCanExecuteChangedFor(nameof(SavePatternCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShareShoppingListCommand))]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private ImageSource? _patternImageSource;

    [ObservableProperty]
    private ImageSource? _originalImageSource;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = AppResources.StatusInitial;

    [ObservableProperty]
    private int _gridWidth = 29;

    [ObservableProperty]
    private int _gridHeight;

    [ObservableProperty]
    private int _totalBeads;

    [ObservableProperty]
    private int _totalColors;

    public bool HasPattern => PatternImageSource is not null;

    public bool HasImageBytes => _lastImageBytes is not null;

    public ObservableCollection<ColorInfoViewModel> ColorInfos { get; } = [];

    // Palette selection
    public ObservableCollection<PaletteType> PaletteTypes { get; } = [];

    [ObservableProperty]
    private PaletteType _selectedPaletteType = PaletteType.Standard;

    // Max color limit
    [ObservableProperty]
    private int _maxColors;

    // Image pre-processing
    [ObservableProperty]
    private float _brightness;

    [ObservableProperty]
    private float _contrast;

    [ObservableProperty]
    private float _saturation;

    // Board grid overlay
    [ObservableProperty]
    private bool _showBoardGrid;

    [ObservableProperty]
    private int _boardWidth = 29;

    [ObservableProperty]
    private int _boardHeight = 29;

    // Dithering
    [ObservableProperty]
    private bool _enableDithering;

    // Bead completion tracking
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetProgressCommand))]
    private int _checkedBeads;

    public int BeadSizePx { get; } = 20;

    // Controls panel
    [ObservableProperty]
    private bool _isControlsPanelExpanded;

    partial void OnIsControlsPanelExpandedChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(ControlsPanelToggleIcon));
    }

    public string ControlsPanelToggleIcon => IsControlsPanelExpanded ? "▲" : "▼";

    [RelayCommand]
    private void ToggleControlsPanel() => IsControlsPanelExpanded = !IsControlsPanelExpanded;

    // Undo/Redo
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool _canUndo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private bool _canRedo;

    [RelayCommand]
    private async Task PickImageAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppResources.PickImageTitle,
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
                return;

            await GeneratePatternFromFileAsync(result);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPrefix, ex.Message);
            IsBusy = false;
        }
    }

    private async Task GeneratePatternFromFileAsync(FileResult file)
    {
        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusProcessing;

            await using var fileStream = await file.OpenReadAsync();
            using var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            _lastImageBytes = memStream.ToArray();
            RegeneratePatternCommand.NotifyCanExecuteChanged();

            OriginalImageSource = ImageSource.FromStream(() => new MemoryStream(_lastImageBytes));

            using var stream = new MemoryStream(_lastImageBytes);
            var settings = new PatternSettings
            {
                Width = GridWidth,
                Height = 0,
                BeadSizePx = 20,
                PaletteType = SelectedPaletteType,
                MaxColors = MaxColors,
                Brightness = Brightness,
                Contrast = Contrast,
                Saturation = Saturation,
                ShowBoardGrid = ShowBoardGrid,
                BoardWidth = BoardWidth,
                BoardHeight = BoardHeight,
                EnableDithering = EnableDithering
            };

            var patternResult = await _patternService.GeneratePatternAsync(stream, settings);
            ApplyPatternResult(patternResult);

            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoState();

            await _progressStorage.ClearProgressAsync();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPrefix, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasImageBytes))]
    private async Task RegeneratePatternAsync()
    {
        if (_lastImageBytes is null) return;

        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusProcessing;

            using var stream = new MemoryStream(_lastImageBytes);
            var settings = new PatternSettings
            {
                Width = GridWidth,
                Height = 0,
                BeadSizePx = 20,
                PaletteType = SelectedPaletteType,
                MaxColors = MaxColors,
                Brightness = Brightness,
                Contrast = Contrast,
                Saturation = Saturation,
                ShowBoardGrid = ShowBoardGrid,
                BoardWidth = BoardWidth,
                BoardHeight = BoardHeight,
                EnableDithering = EnableDithering
            };

            var patternResult = await _patternService.GeneratePatternAsync(stream, settings);
            ApplyPatternResult(patternResult);

            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoState();

            await _progressStorage.ClearProgressAsync();
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPrefix, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPatternResult(PatternResult patternResult)
    {
        GridHeight = patternResult.Pattern.Rows;
        PatternImageSource = ImageSource.FromStream(() => new MemoryStream(patternResult.PatternImage));

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
        CheckedBeads = patternResult.Pattern.CheckedBeadsCount;
        StatusText = string.Format(AppResources.StatusPatternCreated, GridWidth, GridHeight, TotalBeads, TotalColors);
        _ = AutoSavePatternAsync(patternResult.Pattern);
    }

    [RelayCommand(CanExecute = nameof(HasPattern))]
    private async Task PrintAsync()
    {
        if (_lastPatternResult is null) return;

        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusPrinting;

            byte[] printImage = await Task.Run(() =>
                _printRenderer.RenderPrintPage(_lastPatternResult.Pattern));

            var filePath = Path.Combine(FileSystem.CacheDirectory, "Bugelperlen-Druck.png");
            await File.WriteAllBytesAsync(filePath, printImage);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.SharePrintTitle,
                File = new ShareFile(filePath)
            });

            StatusText = AppResources.StatusPrinted;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPrinting, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // PDF Export
    [RelayCommand(CanExecute = nameof(HasPattern))]
    private async Task ExportPdfAsync()
    {
        if (_lastPatternResult is null) return;

        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusPdfCreating;

            byte[] pdfBytes = await Task.Run(() =>
                _printRenderer.RenderPrintPageAsPdf(_lastPatternResult.Pattern));

            var filePath = Path.Combine(FileSystem.CacheDirectory, "Bugelperlen-Muster.pdf");
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = AppResources.SharePdfTitle,
                File = new ShareFile(filePath)
            });

            StatusText = AppResources.StatusPdfCreated;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPdf, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasPattern))]
    private async Task ShowInstructionsAsync()
    {
        if (_lastPatternResult is null) return;

        await Shell.Current.GoToAsync("InstructionPage", new Dictionary<string, object>
        {
            ["Pattern"] = _lastPatternResult.Pattern
        });
    }

    private string AutoSavePatternPath => Path.Combine(FileSystem.AppDataDirectory, "current_pattern.json");

    // Save pattern
    [RelayCommand(CanExecute = nameof(HasPattern))]
    private async Task SavePatternAsync()
    {
        if (_lastPatternResult is null) return;

        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusSaving;

            using var stream = new MemoryStream();
            await _patternStorage.SavePatternToStreamAsync(stream, _lastPatternResult.Pattern);
            stream.Position = 0;

            var result = await _fileSaver.SaveAsync("Bugelperlen-Muster.json", stream, CancellationToken.None);
            if (result.IsSuccessful)
                StatusText = AppResources.StatusSaved;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorSaving, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Load pattern
    [RelayCommand]
    private async Task LoadPatternAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppResources.LoadPatternTitle,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.macOS, new[] { "public.json" } },
                })
            });

            if (result is null) return;

            IsBusy = true;
            StatusText = AppResources.StatusLoading;
            await LoadPatternFromPathAsync(result.FullPath);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorLoading, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPatternFromPathAsync(string filePath)
    {
        var pattern = await _patternStorage.LoadPatternAsync(filePath);

        GridWidth = pattern.Columns;
        GridHeight = pattern.Rows;

        var summary = pattern.GetColorSummary();
        var colorInfos = summary.Select(kvp => new ColorInfo
        {
            ColorName = kvp.Key.Name,
            HexCode = kvp.Key.HexCode,
            Count = kvp.Value,
            Percentage = Math.Round(100.0 * kvp.Value / pattern.TotalBeads, 1)
        }).ToList();

        byte[] patternImage;
        if (ShowBoardGrid)
            patternImage = _patternRenderer.RenderPattern(pattern, BeadSizePx, BoardWidth, BoardHeight);
        else
            patternImage = _patternRenderer.RenderPattern(pattern, BeadSizePx);

        var patternResult = new PatternResult
        {
            Pattern = pattern,
            PatternImage = patternImage,
            ColorInfos = colorInfos
        };

        ApplyPatternResult(patternResult);
        OriginalImageSource = null;

        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();

        await LoadAndApplyProgressAsync();
        if (CheckedBeads > 0)
            RefreshPatternImage();

        StatusText = string.Format(AppResources.StatusLoaded, GridWidth, GridHeight, TotalBeads);
    }

    public async Task InitializeAsync()
    {
        if (!File.Exists(AutoSavePatternPath)) return;

        try
        {
            IsBusy = true;
            StatusText = AppResources.StatusLoading;
            await LoadPatternFromPathAsync(AutoSavePatternPath);
        }
        catch
        {
            File.Delete(AutoSavePatternPath);
            StatusText = AppResources.StatusInitial;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AutoSavePatternAsync(BeadPattern pattern)
    {
        try
        {
            await _patternStorage.SavePatternAsync(AutoSavePatternPath, pattern);
        }
        catch
        {
            // Silent – do not disrupt user workflow
        }
    }

    // Share shopping list
    [RelayCommand(CanExecute = nameof(HasPattern))]
    private async Task ShareShoppingListAsync()
    {
        if (_lastPatternResult is null) return;

        try
        {
            var shoppingList = _lastPatternResult.Pattern.ToShoppingList();
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Title = AppResources.ShareShoppingListTitle,
                Text = shoppingList
            });
        }
        catch (Exception ex)
        {
            StatusText = string.Format(AppResources.ErrorPrefix, ex.Message);
        }
    }

    // Undo
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0 || _lastPatternResult is null) return;

        var edit = _undoStack.Pop();
        _redoStack.Push(edit);

        _lastPatternResult.Pattern.SetCell(edit.Row, edit.Column, edit.OldColor);
        RefreshPatternImage();
        UpdateUndoRedoState();
    }

    // Redo
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0 || _lastPatternResult is null) return;

        var edit = _redoStack.Pop();
        _undoStack.Push(edit);

        _lastPatternResult.Pattern.SetCell(edit.Row, edit.Column, edit.NewColor);
        RefreshPatternImage();
        UpdateUndoRedoState();
    }

    public void EditCell(int row, int column, BeadColor newColor)
    {
        if (_lastPatternResult is null) return;

        var oldColor = _lastPatternResult.Pattern.Grid[row, column]?.Color;
        if (oldColor is null || oldColor == newColor) return;

        _undoStack.Push(new CellEdit(row, column, oldColor, newColor));
        _redoStack.Clear();

        _lastPatternResult.Pattern.SetCell(row, column, newColor);
        RefreshPatternImage();
        UpdateUndoRedoState();
    }

    private void RefreshPatternImage()
    {
        if (_lastPatternResult is null) return;

        byte[] image;
        if (ShowBoardGrid)
            image = _patternRenderer.RenderPattern(_lastPatternResult.Pattern, 20, BoardWidth, BoardHeight);
        else
            image = _patternRenderer.RenderPattern(_lastPatternResult.Pattern);

        PatternImageSource = ImageSource.FromStream(() => new MemoryStream(image));

        var summary = _lastPatternResult.Pattern.GetColorSummary();
        ColorInfos.Clear();
        foreach (var kvp in summary)
        {
            double pct = Math.Round(100.0 * kvp.Value / _lastPatternResult.Pattern.TotalBeads, 1);
            ColorInfos.Add(new ColorInfoViewModel
            {
                ColorName = kvp.Key.Name,
                HexCode = kvp.Key.HexCode,
                Count = kvp.Value,
                Percentage = pct,
                DisplayColor = Color.FromArgb(kvp.Key.HexCode)
            });
        }
        TotalColors = summary.Count;
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    public void ToggleBead(int row, int col)
    {
        if (_lastPatternResult is null) return;
        if (row < 0 || row >= _lastPatternResult.Pattern.Rows) return;
        if (col < 0 || col >= _lastPatternResult.Pattern.Columns) return;
        if (_lastPatternResult.Pattern.Grid[row, col] is null) return;

        _lastPatternResult.Pattern.ToggleChecked(row, col);
        CheckedBeads = _lastPatternResult.Pattern.CheckedBeadsCount;
        RefreshPatternImage();
        _ = SaveProgressAsync();
    }

    [RelayCommand(CanExecute = nameof(HasCheckedBeads))]
    private async Task ResetProgressAsync()
    {
        if (_lastPatternResult is null) return;

        _lastPatternResult.Pattern.CheckedCells.Clear();
        CheckedBeads = 0;
        RefreshPatternImage();
        await _progressStorage.ClearProgressAsync();
    }

    private bool HasCheckedBeads => CheckedBeads > 0;

    private async Task SaveProgressAsync()
    {
        if (_lastPatternResult is null) return;
        await _progressStorage.SaveProgressAsync(_lastPatternResult.Pattern.CheckedCells);
    }

    private async Task LoadAndApplyProgressAsync()
    {
        if (_lastPatternResult is null) return;

        var checkedCells = await _progressStorage.LoadProgressAsync();
        _lastPatternResult.Pattern.CheckedCells.Clear();
        foreach (var cell in checkedCells)
            _lastPatternResult.Pattern.CheckedCells.Add(cell);

        CheckedBeads = _lastPatternResult.Pattern.CheckedBeadsCount;
        if (CheckedBeads > 0)
            RefreshPatternImage();
    }

    private record CellEdit(int Row, int Column, BeadColor OldColor, BeadColor NewColor);
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
