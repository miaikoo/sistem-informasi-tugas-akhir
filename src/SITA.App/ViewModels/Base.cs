using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SITA.App.Services;
using SITA.Core.Models;
using SITA.Core.Services;

namespace SITA.App.ViewModels;

/// <summary>An item for a ComboBox: the stored value plus the label shown to the user.</summary>
public record Option<T>(T Value, string Label)
{
    public static List<Option<TEnum>> FromEnum<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(v => new Option<TEnum>(v, EnumLabels.Of(v))).ToList();

    /// <summary>Enum options preceded by a "Semua ..." entry with a null value, for filters.</summary>
    public static List<Option<TEnum?>> FilterFromEnum<TEnum>(string allLabel) where TEnum : struct, Enum =>
        [new Option<TEnum?>(null, allLabel), .. Enum.GetValues<TEnum>().Select(v => new Option<TEnum?>(v, EnumLabels.Of(v)))];
}

public abstract class PageViewModel : ObservableObject
{
    public abstract string Title { get; }
    public abstract string Subtitle { get; }
    public virtual void Load() { }
}

/// <summary>
/// Shared behaviour for list + slide-in form pages: debounced search, refresh, add/edit/delete
/// with confirmation, and showing validation errors inside the form.
/// </summary>
public abstract partial class CrudPageViewModel<T> : PageViewModel where T : class
{
    protected IDialogService Dialog { get; }
    protected NotificationService Notifications { get; }
    protected UserSession Session { get; }

    private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(300) };

    public ObservableCollection<T> Items { get; } = [];

    [ObservableProperty] private T? _selectedItem;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private string _formTitle = "";
    [ObservableProperty] private string? _formError;
    [ObservableProperty] private T? _editor;
    [ObservableProperty] private int _itemCount;

    protected CrudPageViewModel(IDialogService dialog, NotificationService notifications, UserSession session)
    {
        Dialog = dialog;
        Notifications = notifications;
        Session = session;
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            Refresh();
        };
    }

    protected abstract string EntityName { get; }
    protected abstract IEnumerable<T> Query();
    protected abstract T CreateNew();
    protected abstract T CloneForEdit(T item);
    protected abstract OperationResult SaveItem(T item);
    protected abstract OperationResult DeleteItem(T item);
    protected abstract string Describe(T item);

    /// <summary>Hook to (re)load lookup lists the form needs, e.g. combo box sources.</summary>
    protected virtual void OnFormOpening(T editor, bool isNew) { }

    public virtual bool CanCreate => Session.IsAdmin;
    public virtual bool CanModify => Session.IsAdmin;
    public virtual bool CanDelete => CanModify;

    public bool IsEmpty => ItemCount == 0;
    partial void OnItemCountChanged(int value) => OnPropertyChanged(nameof(IsEmpty));

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    /// <summary>Lets subclasses re-run the query when a filter changes.</summary>
    protected void FilterChanged() => Refresh();

    public override void Load()
    {
        try
        {
            LoadLookups();
        }
        catch (Exception ex)
        {
            Dialog.Error(DbErrorTranslator.Translate(ex));
        }
        Refresh();
    }

    /// <summary>Loads filter/lookup lists once when the page opens.</summary>
    protected virtual void LoadLookups() { }

    [RelayCommand]
    protected void Refresh()
    {
        try
        {
            Items.Clear();
            foreach (var item in Query()) Items.Add(item);
            ItemCount = Items.Count;
        }
        catch (Exception ex)
        {
            Dialog.Error(DbErrorTranslator.Translate(ex));
        }
    }

    [RelayCommand]
    private void Add()
    {
        if (!CanCreate) return;
        OpenForm(CreateNew(), isNew: true);
    }

    [RelayCommand]
    private void Edit(T? item)
    {
        item ??= SelectedItem;
        if (item is null || !CanModify) return;
        OpenForm(CloneForEdit(item), isNew: false);
    }

    private void OpenForm(T editor, bool isNew)
    {
        try
        {
            IsNew = isNew;
            FormTitle = (isNew ? "Tambah " : "Ubah ") + EntityName;
            FormError = null;
            OnFormOpening(editor, isNew);
            Editor = editor;
            IsFormOpen = true;
        }
        catch (Exception ex)
        {
            Dialog.Error(DbErrorTranslator.Translate(ex));
        }
    }

    [RelayCommand]
    private void Delete(T? item)
    {
        item ??= SelectedItem;
        if (item is null) return;
        if (!Dialog.Confirm($"Hapus {EntityName}?", $"{Describe(item)} akan dihapus permanen. Lanjutkan?")) return;

        var result = DeleteItem(item);
        if (!result.Success)
        {
            Dialog.Error(result.ErrorText, "Tidak Dapat Menghapus");
            return;
        }
        Notifications.Show($"{EntityName} berhasil dihapus");
        if (IsFormOpen) IsFormOpen = false;
        Refresh();
    }

    [RelayCommand]
    private void Save()
    {
        if (Editor is null) return;
        CommitFocusedTextBox();

        var result = SaveItem(Editor);
        if (!result.Success)
        {
            FormError = "• " + string.Join(Environment.NewLine + "• ", result.Errors);
            return;
        }
        IsFormOpen = false;
        Notifications.Show(IsNew ? $"{EntityName} berhasil ditambahkan" : "Perubahan berhasil disimpan");
        Refresh();
    }

    [RelayCommand]
    private void CancelForm()
    {
        IsFormOpen = false;
        FormError = null;
    }

    /// <summary>Pushes the text of the still-focused field into the editor before saving.</summary>
    private static void CommitFocusedTextBox()
    {
        if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox tb)
            tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
    }
}
