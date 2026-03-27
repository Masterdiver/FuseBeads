using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FuseBeads.Domain.Entities;
using FuseBeads.Resources.Strings;
using System.Collections.ObjectModel;

namespace FuseBeads.ViewModels;

#pragma warning disable MVVMTK0045
public partial class InstructionViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string _title = AppResources.InstructionTitle;

    public ObservableCollection<InstructionRowViewModel> Rows { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Pattern", out var obj) && obj is BeadPattern pattern)
        {
            BuildInstructions(pattern);
        }
    }

    private void BuildInstructions(BeadPattern pattern)
    {
        Rows.Clear();
        for (int row = 0; row < pattern.Rows; row++)
        {
            var cells = new List<BeadCellViewModel>();
            for (int col = 0; col < pattern.Columns; col++)
            {
                var cell = pattern.Grid[row, col];
                if (cell is not null)
                {
                    cells.Add(new BeadCellViewModel
                    {
                        DisplayColor = Color.FromRgb(cell.Color.R, cell.Color.G, cell.Color.B),
                        ColorName = cell.Color.Name
                    });
                }
            }
            Rows.Add(new InstructionRowViewModel
            {
                RowNumber = row + 1,
                Cells = new ObservableCollection<BeadCellViewModel>(cells)
            });
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public partial class InstructionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _rowNumber;

    [ObservableProperty]
    private ObservableCollection<BeadCellViewModel> _cells = [];
}

public partial class BeadCellViewModel : ObservableObject
{
    [ObservableProperty]
    private Color _displayColor = Colors.Transparent;

    [ObservableProperty]
    private string _colorName = string.Empty;
}
#pragma warning restore MVVMTK0045
