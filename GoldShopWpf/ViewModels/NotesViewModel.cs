using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using GoldShopWpf.Services;
using GoldShopWpf.Views;

namespace GoldShopWpf.ViewModels;

public class NotesViewModel : ViewModelBase
{
    private string _searchText = string.Empty;
    private NotesGridItem? _selectedNote;

    public ObservableCollection<NotesGridItem> Notes { get; } = new();
    public ICollectionView NotesView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                NotesView.Refresh();
                RefreshSelectionState();
            }
        }
    }

    public NotesGridItem? SelectedNote
    {
        get => _selectedNote;
        set
        {
            if (SetProperty(ref _selectedNote, value))
            {
                RefreshSelectionState();
            }
        }
    }

    public bool? AreAllVisibleSelected
    {
        get
        {
            var visible = GetVisibleNotes().ToList();
            if (visible.Count == 0)
            {
                return false;
            }

            var selectedCount = visible.Count(note => note.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == visible.Count ? true : null;
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            foreach (var note in GetVisibleNotes())
            {
                note.IsSelected = value.Value;
            }

            RefreshSelectionState();
        }
    }

    public int CheckedCount => Notes.Count(note => note.IsSelected);

    public RelayCommand AddCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public NotesViewModel()
    {
        NotesView = CollectionViewSource.GetDefaultView(Notes);
        NotesView.Filter = FilterNote;
        NotesView.SortDescriptions.Add(new SortDescription(nameof(NotesGridItem.CreatedAt), ListSortDirection.Descending));
        Notes.CollectionChanged += OnNotesCollectionChanged;

        AddCommand = new RelayCommand(_ => AddNote());
        EditCommand = new RelayCommand(_ => EditNote(), _ => GetEditableNote() != null);
        DeleteCommand = new RelayCommand(_ => DeleteNotes(), _ => GetDeleteTargets().Count > 0);
        RefreshCommand = new RelayCommand(_ => Load());

        Load();
    }

    public void Load()
    {
        Notes.Clear();
        SelectedNote = null;

        foreach (var note in AppServices.ClientNoteService.GetNotes())
        {
            AddNoteRow(note.Id, note.ClientName, note.Content, note.CreatedAt);
        }

        NotesView.Refresh();
        RefreshSelectionState();
    }

    private void AddNote()
    {
        var window = new NoteWindow
        {
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        AppServices.ClientNoteService.AddNote(window.ClientName, window.NoteContent);
        Load();
        MessageBox.Show(UiText.L("MsgNoteSaved"), UiText.L("TitleNotes"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EditNote()
    {
        var editable = GetEditableNote();
        if (editable == null)
        {
            return;
        }

        var window = new NoteWindow(editable.ToClientNote())
        {
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        AppServices.ClientNoteService.UpdateNote(editable.Id, window.ClientName, window.NoteContent, editable.CreatedAt);
        Load();
        MessageBox.Show(UiText.L("MsgNoteUpdated"), UiText.L("TitleNotes"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteNotes()
    {
        var deleteTargets = GetDeleteTargets();
        if (deleteTargets.Count == 0)
        {
            return;
        }

        var message = deleteTargets.Count == 1
            ? UiText.Format("MsgDeleteNoteConfirm", deleteTargets[0].ClientName)
            : UiText.Format("MsgDeleteNotesConfirm", deleteTargets.Count);

        var result = MessageBox.Show(
            message,
            UiText.L("TitleConfirmDelete"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var note in deleteTargets)
        {
            AppServices.ClientNoteService.DeleteNote(note.Id);
        }

        Load();
        MessageBox.Show(
            string.Format(UiText.L("MsgNotesDeleted"), deleteTargets.Count),
            UiText.L("TitleNotes"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void AddNoteRow(int id, string clientName, string content, DateTime createdAt)
    {
        var row = new NotesGridItem
        {
            Id = id,
            ClientName = clientName,
            Content = content,
            CreatedAt = createdAt
        };

        row.PropertyChanged += OnNoteRowPropertyChanged;
        Notes.Add(row);
    }

    private void OnNotesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<NotesGridItem>())
            {
                item.PropertyChanged -= OnNoteRowPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<NotesGridItem>())
            {
                item.PropertyChanged += OnNoteRowPropertyChanged;
            }
        }
    }

    private void OnNoteRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotesGridItem.IsSelected))
        {
            RefreshSelectionState();
        }
    }

    private bool FilterNote(object obj)
    {
        if (obj is not NotesGridItem note)
        {
            return false;
        }

        var query = SearchText.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return note.ClientName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
               note.Content.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private IEnumerable<NotesGridItem> GetVisibleNotes()
    {
        return NotesView.Cast<NotesGridItem>();
    }

    private List<NotesGridItem> GetCheckedNotes()
    {
        return Notes.Where(note => note.IsSelected).ToList();
    }

    private NotesGridItem? GetEditableNote()
    {
        var checkedNotes = GetCheckedNotes();
        if (checkedNotes.Count == 1)
        {
            return checkedNotes[0];
        }

        if (checkedNotes.Count > 1)
        {
            return null;
        }

        return SelectedNote;
    }

    private List<NotesGridItem> GetDeleteTargets()
    {
        var checkedNotes = GetCheckedNotes();
        if (checkedNotes.Count > 0)
        {
            return checkedNotes;
        }

        return SelectedNote == null ? [] : [SelectedNote];
    }

    private void RefreshSelectionState()
    {
        OnPropertyChanged(nameof(AreAllVisibleSelected));
        OnPropertyChanged(nameof(CheckedCount));
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
    }
}
