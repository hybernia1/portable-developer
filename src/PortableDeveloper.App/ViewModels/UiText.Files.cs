using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string EditorSelection => IsCzech ? "Editor souborů" : "File editor";

    public string EditorSelectionHelp => IsCzech
        ? "Pro textové a zdrojové soubory můžete upřednostnit ověřený portable Notepad++, nebo vždy použít výchozí aplikaci Windows. Ostatní typy souborů se dál otevírají systémovou aplikací."
        : "For text and source files, prefer the verified portable Notepad++ or always use the Windows default application. Other file types continue to use their system application.";

    public string PreferPortableEditor => IsCzech ? "Portable Notepad++ (pokud je dostupný)" : "Portable Notepad++ (when available)";

    public string UseWindowsDefaultEditor => IsCzech ? "Výchozí aplikace Windows" : "Windows default application";

    public string EditorSelectionSaved => IsCzech ? "Volba editoru byla uložena." : "The editor preference was saved.";

    public string Up => IsCzech ? "Nahoru" : "Up";

    public string Back => IsCzech ? "Zpět" : "Back";

    public string RefreshFiles => IsCzech ? "Obnovit" : "Refresh";

    public string NewFile => IsCzech ? "Nový soubor" : "New file";

    public string NewFolder => IsCzech ? "Nová složka" : "New folder";

    public string Folder => IsCzech ? "Složka" : "Folder";

    public string File => IsCzech ? "Soubor" : "File";

    public string Type => IsCzech ? "Typ" : "Type";

    public string WorkspaceKindLabel(WorkspaceFileKind kind) => kind switch
    {
        WorkspaceFileKind.Folder => Folder,
        WorkspaceFileKind.Php => "PHP",
        WorkspaceFileKind.Python => "Python",
        WorkspaceFileKind.JavaScript => "JavaScript / TypeScript",
        WorkspaceFileKind.StyleSheet => IsCzech ? "Styl" : "Style sheet",
        WorkspaceFileKind.Html => "HTML",
        WorkspaceFileKind.Xml => "XML / SVG",
        WorkspaceFileKind.Json => "JSON",
        WorkspaceFileKind.Yaml => "YAML",
        WorkspaceFileKind.Markdown => "Markdown",
        WorkspaceFileKind.Text => IsCzech ? "Text" : "Text",
        WorkspaceFileKind.Document => IsCzech ? "Dokument" : "Document",
        WorkspaceFileKind.Spreadsheet => IsCzech ? "Tabulka" : "Spreadsheet",
        WorkspaceFileKind.Configuration => IsCzech ? "Konfigurace" : "Configuration",
        WorkspaceFileKind.Image => IsCzech ? "Obrázek" : "Image",
        WorkspaceFileKind.Archive => IsCzech ? "Archiv" : "Archive",
        WorkspaceFileKind.Database => IsCzech ? "Databáze" : "Database",
        WorkspaceFileKind.Executable => IsCzech ? "Spustitelný soubor" : "Executable",
        _ => File
    };

    public string WorkspacePageSummary(int first, int last, int total) => IsCzech
        ? $"{first}–{last} z {total}"
        : $"{first}–{last} of {total}";

    public string ItemsPerPage => IsCzech ? "Položek" : "Items";

    public string WorkspaceAddressHint => IsCzech
        ? "Zadejte cestu uvnitř projektu"
        : "Enter a path inside the project";

    public string FirstPage => IsCzech ? "První stránka" : "First page";

    public string PreviousPage => IsCzech ? "Předchozí stránka" : "Previous page";

    public string NextPage => IsCzech ? "Další stránka" : "Next page";

    public string LastPage => IsCzech ? "Poslední stránka" : "Last page";

    public string Open => IsCzech ? "Otevřít" : "Open";

    public string Edit => IsCzech ? "Upravit" : "Edit";

    public string Copy => IsCzech ? "Kopírovat" : "Copy";

    public string Cut => IsCzech ? "Vyjmout" : "Cut";

    public string Paste => IsCzech ? "Vložit" : "Paste";

    public string Rename => IsCzech ? "Přejmenovat" : "Rename";

    public string Delete => IsCzech ? "Smazat" : "Delete";

    public string EmptyFolder => IsCzech ? "Složka je prázdná." : "The folder is empty.";

    public string Name => IsCzech ? "Název" : "Name";

    public string Size => IsCzech ? "Velikost" : "Size";

    public string Modified => IsCzech ? "Změněno" : "Modified";

    public string Actions => IsCzech ? "Akce" : "Actions";

    public string CreateFileTitle => IsCzech ? "Nový soubor" : "New file";

    public string CreateFolderTitle => IsCzech ? "Nová složka" : "New folder";

    public string EnterFileName => IsCzech ? "Zadejte název nového souboru." : "Enter the new file name.";

    public string EnterFolderName => IsCzech ? "Zadejte název nové složky." : "Enter the new folder name.";

    public string Confirm => IsCzech ? "Potvrdit" : "Confirm";

    public string Cancel => IsCzech ? "Zrušit" : "Cancel";

    public string WorkspaceItemNameRequired => IsCzech
        ? "Nejdříve zadejte platný název souboru nebo složky."
        : "Enter a valid file or directory name first.";

    public string DeleteItemQuestion(string name) => IsCzech
        ? $"Opravdu smazat {name}? U neprázdné složky se smaže celý její obsah."
        : $"Delete {name}? A non-empty folder and all its contents will be removed.";

    public string DeleteItemsQuestion(int count) => IsCzech
        ? $"Opravdu smazat vybrané položky ({count})? U neprázdných složek se smaže celý jejich obsah."
        : $"Delete the selected items ({count})? Non-empty folders and all their contents will be removed.";

    public string DeleteItemTitle => IsCzech ? "Smazání položky projektu" : "Delete project item";

    public string WorkspaceOperationFailed(string detail) => IsCzech
        ? $"Operace se souborem selhala: {detail}"
        : $"File operation failed: {detail}";

    public string WorkspaceCopyNameSuffix => IsCzech ? " - kopie" : " - copy";

    public string WorkspaceItemCopied(string name) => IsCzech
        ? $"{name} je připraven k překopírování."
        : $"{name} is ready to be copied.";

    public string WorkspaceItemsCopied(int count) => IsCzech
        ? $"Vybrané položky jsou připraveny ke kopírování ({count})."
        : $"The selected items are ready to be copied ({count}).";

    public string WorkspaceItemCut(string name) => IsCzech
        ? $"{name} je připraven k přesunutí."
        : $"{name} is ready to be moved.";

    public string WorkspaceItemsCut(int count) => IsCzech
        ? $"Vybrané položky jsou připraveny k přesunutí ({count})."
        : $"The selected items are ready to be moved ({count}).";

    public string WorkspacePasteCompleted => IsCzech ? "Položka byla vložena." : "The item was pasted.";

    public string WorkspaceItemsPasteCompleted(int completed, int total) => IsCzech
        ? $"Vloženo položek: {completed} z {total}."
        : $"Pasted items: {completed} of {total}.";

    public string WorkspaceItemsMoved(int completed, int total) => IsCzech
        ? $"Přesunuto položek: {completed} z {total}."
        : $"Moved items: {completed} of {total}.";

    public string WorkspaceItemsImported(int count) => (IsCzech, count) switch
    {
        (true, 0) => "Nebyla přidána žádná položka.",
        (true, 1) => "Položka byla přidána do projektu.",
        (true, _) => $"Položky byly přidány do projektu ({count}).",
        (false, 0) => "No items were added.",
        (false, 1) => "The item was added to the project.",
        _ => $"Items were added to the project ({count})."
    };

    public string WorkspaceTransferSkipped => IsCzech ? "Položka byla přeskočena." : "The item was skipped.";

    public string WorkspaceConflictTitle => IsCzech ? "Položka již existuje" : "Item already exists";

    public string WorkspaceConflictMessage(WorkspaceConflict conflict) => IsCzech
        ? $"V cíli „{conflict.DestinationRelativePath}“ už existuje položka se stejným názvem. Co udělat s „{conflict.Name}“?"
        : $"The destination “{conflict.DestinationRelativePath}” already contains an item with the same name. What should happen to “{conflict.Name}”?";

    public string Overwrite => IsCzech ? "Přepsat" : "Overwrite";

    public string RenameCopy => IsCzech ? "Přejmenovat kopii" : "Rename copy";

    public string Skip => IsCzech ? "Přeskočit" : "Skip";

    public string ApplyToRemainingConflicts => IsCzech
        ? "Použít tuto volbu pro všechny další kolize"
        : "Use this choice for all remaining conflicts";

    public string WorkspaceClipboardUnavailable => IsCzech
        ? "Položku lze vložit pouze v projektu, ve kterém byla zkopírována nebo vyjmuta."
        : "The item can be pasted only within the project where it was copied or cut.";

    public string WorkspaceDraggedItemUnavailable => IsCzech
        ? "Přetahovaná položka už není součástí aktivního projektu."
        : "The dragged item is no longer inside the active project.";

}
