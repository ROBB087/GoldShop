using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using GoldShopWpf.Services;
using GoldShopWpf.Views;

namespace GoldShopWpf.ViewModels;

public class NotesViewModel : ViewModelBase
{
    private const int PageSize = 50;

    private string _searchText = string.Empty;
    private NotesGridItem? _selectedNote;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private int _totalRecords;

    public ObservableCollection<NotesGridItem> Notes { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                CurrentPage = 1;
                Load();
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

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(PageSummary));
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PageSummary));
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalRecords
    {
        get => _totalRecords;
        private set
        {
            if (SetProperty(ref _totalRecords, value))
            {
                OnPropertyChanged(nameof(PageSummary));
            }
        }
    }

    public string PageSummary => UiText.Format("LblPageSummary", CurrentPage, TotalPages, TotalRecords);
    public string RowsCountLabel => UiText.Format("LblRows", Notes.Count);
    public int EffectiveSelectedCount => CheckedCount > 0 ? CheckedCount : SelectedNote == null ? 0 : 1;
    public string SelectedCountLabel => UiText.Format("LblSelectedCount", EffectiveSelectedCount);
    public bool HasSelection => CheckedCount > 0 || SelectedNote != null;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public bool? AreAllVisibleSelected
    {
        get
        {
            if (Notes.Count == 0)
            {
                return false;
            }

            var selectedCount = Notes.Count(note => note.IsSelected);
            if (selectedCount == 0)
            {
                return false;
            }

            return selectedCount == Notes.Count ? true : null;
        }
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            foreach (var note in Notes)
            {
                note.IsSelected = value.Value;
            }

            RefreshSelectionState();
        }
    }

    public int CheckedCount => Notes.Count(note => note.IsSelected);

    public AsyncRelayCommand AddCommand { get; }
    public AsyncRelayCommand EditCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand PreviousPageCommand { get; }
    public AsyncRelayCommand NextPageCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }

    public NotesViewModel()
    {
        Notes.CollectionChanged += OnNotesCollectionChanged;

        AddCommand = TrackCommand(new AsyncRelayCommand(_ => AddNoteAsync(), _ => !IsBusy));
        EditCommand = TrackCommand(new AsyncRelayCommand(_ => EditNoteAsync(), _ => !IsBusy && GetEditableNote() != null));
        DeleteCommand = TrackCommand(new AsyncRelayCommand(_ => DeleteNotesAsync(), _ => !IsBusy && GetDeleteTargets().Count > 0));
        RefreshCommand = TrackCommand(new AsyncRelayCommand(_ => LoadAsync(), _ => !IsBusy));
        PreviousPageCommand = TrackCommand(new AsyncRelayCommand(_ => ChangePageAsync(-1), _ => !IsBusy && HasPreviousPage));
        NextPageCommand = TrackCommand(new AsyncRelayCommand(_ => ChangePageAsync(1), _ => !IsBusy && HasNextPage));
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection(), _ => HasSelection);

        Load();
    }

    public void Load()
    {
        ObserveBackgroundTask(LoadAsync(), "NotesViewModel.Load");
    }

    public async Task LoadAsync()
    {
        await RunBusyAsync("Loading notes...", async () =>
        {
            var requestedPage = CurrentPage;
            var page = await Task.Run(() => AppServices.ClientNoteService.GetNotesPage(SearchText, requestedPage, PageSize));

            var effectiveTotalPages = Math.Max(page.TotalPages, 1);
            if (requestedPage > effectiveTotalPages)
            {
                CurrentPage = effectiveTotalPages;
                page = await Task.Run(() => AppServices.ClientNoteService.GetNotesPage(SearchText, CurrentPage, PageSize));
            }

            TotalRecords = page.TotalCount;
            TotalPages = Math.Max(page.TotalPages, 1);

            Notes.Clear();
            SelectedNote = null;

            foreach (var note in page.Items)
            {
                AddNoteRow(note.Id, note.ClientName, note.Content, note.CreatedAt);
            }

            OnPropertyChanged(nameof(RowsCountLabel));
            RefreshSelectionState();
        }, UiText.L("MsgGenericError"));
    }

    private async Task AddNoteAsync()
    {
        var window = new NoteWindow
        {
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() != true)
        {
            return;
        }

        var clientName = window.ClientName;
        var noteContent = window.NoteContent;

        await RunBusyAsync("Saving note...", async () =>
        {
            await Task.Run(() => AppServices.ClientNoteService.AddNote(clientName, noteContent));
        }, string.Empty, rethrow: true);

        CurrentPage = 1;
        await LoadAsync();
        MessageBox.Show(UiText.L("MsgNoteSaved"), UiText.L("TitleNotes"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task EditNoteAsync()
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

        var clientName = window.ClientName;
        var noteContent = window.NoteContent;

        await RunBusyAsync("Saving note...", async () =>
        {
            await Task.Run(() => AppServices.ClientNoteService.UpdateNote(editable.Id, clientName, noteContent, editable.CreatedAt));
        }, string.Empty, rethrow: true);

        await LoadAsync();
        MessageBox.Show(UiText.L("MsgNoteUpdated"), UiText.L("TitleNotes"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task DeleteNotesAsync()
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

        await RunBusyAsync("Deleting notes...", async () =>
        {
            await Task.Run(() =>
            {
                foreach (var note in deleteTargets)
                {
                    AppServices.ClientNoteService.DeleteNote(note.Id);
                }
            });
        }, string.Empty, rethrow: true);

        await LoadAsync();
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

    private Task ChangePageAsync(int delta)
    {
        CurrentPage = Math.Clamp(CurrentPage + delta, 1, TotalPages);
        return LoadAsync();
    }

    public void SetVisibleSelection(bool isSelected)
    {
        foreach (var note in Notes)
        {
            note.IsSelected = isSelected;
        }

        RefreshSelectionState();
    }

    private void ClearSelection()
    {
        foreach (var note in Notes)
        {
            note.IsSelected = false;
        }

        SelectedNote = null;
        RefreshSelectionState();
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
        OnPropertyChanged(nameof(EffectiveSelectedCount));
        OnPropertyChanged(nameof(SelectedCountLabel));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(RowsCountLabel));
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
    }
}
