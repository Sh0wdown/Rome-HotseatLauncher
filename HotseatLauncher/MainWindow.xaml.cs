using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace HotseatLauncher
{
    // - save game / factions desync warning (even while ingame)
    // - notifications
    // - save camera position
    // - consider faction deaths
    // - diplomacy
    // - disable automanaging
    // - other versions
    // - event messages

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Show & Hide Menus

        enum Menus
        {
            None,
            Settings,
            GameList,
            Faction,
            Creation,
            Pick
        }

        Menus lastMenu;
        Menus currentMenu;
        void ShowMenu(Menus menu)
        {
            if (currentMenu == menu)
                return;

            Grid menuGrid;
            switch (menu)
            {
                case Menus.GameList:
                    menuGrid = gamesGrid;
                    Session.LoadSessions();
                    gGamesListView.Items.Refresh();
                    SelectedSessionChange(null, null);
                    break;
                case Menus.Settings:
                    sPathTextBox.Text = "";
                    menuGrid = settingsGrid;
                    break;
                case Menus.Faction:
                    menuGrid = factionGrid;
                    RefreshFreeFactions();
                    break;
                case Menus.Creation:
                    menuGrid = creationGrid;
                    if (cInstallListView.SelectedIndex < 0 && cInstallListView.Items.Count > 0)
                        cInstallListView.SelectedIndex = 0;

                    cTransferTextBox.Text = GetTransferText(cTransferTextBox.Text);
                    break;
                case Menus.Pick:
                    menuGrid = pickGrid;
                    break;
                default:
                    return;
            }

            lastMenu = currentMenu;
            currentMenu = menu;

            // hide all other grids
            foreach (var child in mainGrid.Children)
                if (child != menuGrid && child is Grid)
                    ((Grid)child).Visibility = Visibility.Hidden;

            menuGrid.Visibility = Visibility.Visible;
        }

        #endregion

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                Session.OnFactionsChange += GameSession_OnFactionsChange;
                Session.sPropertyChanged += (s, e) => SelectedSessionChange(s, null);

                Settings.Load();
                UpdateCheckBoxes();

                if (Settings.Installations.Count() == 0)
                {
                    currentMenu = Menus.GameList; // so we return to the game list on settings toggle
                    ShowMenu(Menus.Settings);
                }
                else
                {
                    ShowMenu(Menus.GameList);
                }
                CheckRegistryForInstallations();
            }
            catch (Exception e)
            {
                Debug.ShowException(e);
            }
        }

        void CheckRegistryForInstallations()
        {
            foreach (CodeVersion version in CodeVersion.Versions)
            {
                string path = (string)Microsoft.Win32.Registry.GetValue(version.InstallRegKey, version.InstallRegValue, null);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    Settings.AddInstallPaths(path);
                }
            }
        }

        string GetTransferText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                if (!string.IsNullOrWhiteSpace(Settings.LastTransferPath))
                {
                    return Settings.LastTransferPath;
                }
                else if (gGamesListView.Items.Count > 0)
                {
                    return ((Session)gGamesListView.Items[0]).TransferPath;
                }
            }
            return text;
        }

        #region Settings

        void ToggleSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentMenu == Menus.Settings) // close
                {
                    if (Settings.Installations.Count() == 0)
                    {
                        Debug.ShowWarning("Please add an installation.");
                        return;
                    }

                    ShowMenu(lastMenu);
                }
                else
                {
                    ShowMenu(Menus.Settings);
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void OpenInstallPathDialog(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Choose a Rome: Total War installation directory.";
                    dialog.SelectedPath = sPathTextBox.Text;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        sPathTextBox.Text = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void AddInstallPath(object sender, RoutedEventArgs e)
        {
            Settings.AddInstallPaths(sPathTextBox.Text);
            Settings.Save();
            sInstallListView.Items.Refresh();
        }

        #region CheckBoxes

        void UpdateCheckBoxes()
        {
            sWindowedCheckBox.IsChecked = Settings.WindowedMode;
            sDisableAudioCheckBox.IsChecked = Settings.DisableAudio;
            sShowErrorsCheckBox.IsChecked = Settings.ShowErrors;
        }

        void sWindowedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.WindowedMode = (bool)sWindowedCheckBox.IsChecked;
            Settings.Save();
        }

        void sDisableAudioCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.DisableAudio = (bool)sDisableAudioCheckBox.IsChecked;
            Settings.Save();
        }

        void sShowErrorsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Settings.ShowErrors = (bool)sShowErrorsCheckBox.IsChecked;
            Settings.Save();
        }

        #endregion

        #endregion

        #region Game List

        void ShowGameList(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowMenu(Menus.GameList);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void StartGame(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Session)gGamesListView.SelectedItem).StartGame();
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void AddGame(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Choose the path of the game session transfer path. (f.e. .../DropBox/Rome/SessionName";
                    dialog.SelectedPath = GetTransferText(dialog.SelectedPath);

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string name = Path.GetFileName(dialog.SelectedPath);
                        string rename = name;

                        while (Session.Sessions.FirstOrDefault(s => string.Equals(s.Rename, rename, StringComparison.OrdinalIgnoreCase)) != null)
                        {
                            rename = InputBox.OpenDialog(name, "Session name already exists. Please rename:");
                            if (rename == null)
                            {
                                dialog.Dispose();
                                return;
                            }
                        }

                        Session session = Session.AddExisting(dialog.SelectedPath, rename);
                        gGamesListView.Items.Refresh();
                        gGamesListView.SelectedItem = session;

                        Settings.LastTransferPath = session.TransferPath;
                        Settings.Save();
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void RemoveGame(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null)
                    return;

                bool deleteSaves = false;
                if (session.HasSaveGamesStored())
                {
                    var result = MessageBox.Show("Delete backups?", string.Format("Remove '{0}'", session.Rename), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Cancel)
                        return;
                    if (result == MessageBoxResult.Yes)
                        deleteSaves = true;
                }
                session.Remove(deleteSaves);
                gGamesListView.Items.Refresh();
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void SelectedSessionChange(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session != null)
                {
                    gStartButton.IsEnabled = session.State == Session.Status.Ready;
                    gPickButton.IsEnabled = session.State == Session.Status.Warn;
                    gFactionButton.IsEnabled = session.State != Session.Status.Warn;
                    gDropOutButton.IsEnabled = session.State != Session.Status.Warn;
                }
                else
                {
                    gStartButton.IsEnabled = false;
                    gPickButton.IsEnabled = false;
                    gFactionButton.IsEnabled = false;
                    gDropOutButton.IsEnabled = false;
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        #endregion

        #region Creation

        SortedList<int, FactionInfo> availableFactions = new SortedList<int, FactionInfo>();
        IEnumerable<FactionInfo> AvailableFactions { get { return availableFactions.Values; } }

        SortedList<int, FactionInfo> selectedFactions = new SortedList<int, FactionInfo>();
        IEnumerable<FactionInfo> SelectedFactions { get { return availableFactions.Values; } }

        void ShowCreationMenu(object sender, RoutedEventArgs e)
        {
            ShowMenu(Menus.Creation);
        }

        void cInstallView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cModComboBox.Items.Clear();
                Installation installation = (Installation)cInstallListView.SelectedItem;
                if (installation == null)
                {
                    cModComboBox.IsEnabled = false;
                    return;
                }
                cModComboBox.IsEnabled = true;

                foreach (ModFolder mod in installation.Mods)
                    cModComboBox.Items.Add(mod);

                cModComboBox.SelectedIndex = 0;
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void cModComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cCampaignComboBox.Items.Clear();

                ModFolder modFolder = (ModFolder)cModComboBox.SelectedItem;
                if (modFolder == null)
                {
                    cCampaignComboBox.IsEnabled = false;
                    return;
                }
                cCampaignComboBox.IsEnabled = true;

                foreach (Campaign camp in modFolder.Campaigns)
                    cCampaignComboBox.Items.Add(camp);

                cCampaignComboBox.SelectedIndex = 0;
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void cCampaignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cAvailableFactionsListBox.Items.Clear();
                cSelectedFactionsListBox.Items.Clear();
                cFactionComboBox.Items.Clear();

                Campaign campaign = (Campaign)cCampaignComboBox.SelectedItem;
                if (campaign == null)
                {
                    cAvailableFactionsListBox.IsEnabled = false;
                    cSelectedFactionsListBox.IsEnabled = false;
                    cAddFactionButton.IsEnabled = false;
                    cRemoveFactionButton.IsEnabled = false;
                    cFactionComboBox.IsEnabled = false;
                    return;
                }
                cAvailableFactionsListBox.IsEnabled = true;
                cSelectedFactionsListBox.IsEnabled = true;
                cAddFactionButton.IsEnabled = true;
                cRemoveFactionButton.IsEnabled = true;
                cFactionComboBox.IsEnabled = true;

                IEnumerable<CampaignFaction> factions;
                if (cNonPlayablesComboBox.IsChecked == true)
                {
                    factions = campaign.Factions;
                }
                else
                {
                    factions = campaign.Factions.Where(f => f.Playable);
                }
                cAvailableFactionsListBox.AddItems(factions.OrderBy(f => f.Faction.Index));
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }


        void cNonPlayablesComboBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                Campaign campaign = (Campaign)cCampaignComboBox.SelectedItem;
                if (campaign == null)
                    return;

                if (cNonPlayablesComboBox.IsChecked == true)
                {
                    cAvailableFactionsListBox.AddItemsSorted(campaign.Factions.Where(p => !p.Playable), p => p.Faction.Index);
                }
                else
                {
                    cSelectedFactionsListBox.RemoveAll<CampaignFaction>(p => !p.Playable);
                    cFactionComboBox.RemoveAll<CampaignFaction>(p => !p.Playable);
                    cAvailableFactionsListBox.RemoveAll<CampaignFaction>(p => !p.Playable);
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void cAddFactionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (CampaignFaction)cAvailableFactionsListBox.SelectedItem;
                if (item == null)
                    return;

                int index = cAvailableFactionsListBox.SelectedIndex;

                cSelectedFactionsListBox.AddItemSorted(item, i => i.Faction.Index);
                cFactionComboBox.AddItemSorted(item, i => i.Faction.Index);
                cAvailableFactionsListBox.RemoveItem(item);

                cAvailableFactionsListBox.SelectedIndex = index < cAvailableFactionsListBox.Items.Count ? index : index - 1;
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void cRemoveFactionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (CampaignFaction)cSelectedFactionsListBox.SelectedItem;
                if (item == null)
                    return;

                int index = cSelectedFactionsListBox.SelectedIndex;

                cSelectedFactionsListBox.RemoveItem(item);
                cFactionComboBox.RemoveItem(item);
                cAvailableFactionsListBox.AddItemSorted(item, p => p.Faction.Index);

                cSelectedFactionsListBox.SelectedIndex = index < cSelectedFactionsListBox.Items.Count ? index : index - 1;
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void OpenTransferPathDialog(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Choose your transfer folder. (f.e. '../DropBox/Rome')";
                    dialog.SelectedPath = cTransferTextBox.Text;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        cTransferTextBox.Text = dialog.SelectedPath;
                        Settings.LastTransferPath = dialog.SelectedPath;
                        Settings.Save();
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void CreateGame(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = cNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Debug.ShowWarning("Please type in a game session name.");
                    cNameTextBox.Focus();
                    return;
                }

                int invalidChar = name.IndexOfAny(Path.GetInvalidFileNameChars());
                if (invalidChar >= 0)
                {
                    Debug.ShowWarning("The symbol {0} is not allowed in the game session name.", name[invalidChar].ToString());
                    cNameTextBox.Focus();
                    return;
                }

                if (Session.ContainsSession(name))
                {
                    Debug.ShowWarning("There is already a game session with the name " + name);
                    cNameTextBox.Focus();
                    return;
                }

                string transferPath = Path.GetFullPath(cTransferTextBox.Text);
                Directory.CreateDirectory(transferPath);

                string gameFile = Path.Combine(transferPath, name, Session.GameFileName);
                if (File.Exists(gameFile))
                {
                    Debug.ShowWarning("There is already a game session file in the following path. It might be from an older session which you left. If you are sure it's not used by / transferring to other players anymore delete it manually.\n\n" + gameFile);
                    return;
                }

                Difficulty difficulty = (byte)cDifficultyComboBox.SelectedIndex;
                if (difficulty == Difficulty.Unset)
                {
                    Debug.ShowWarning("Invalid difficulty.");
                    cDifficultyComboBox.Focus();
                    return;
                }

                if (cFactionComboBox.SelectedItem == null)
                {
                    Debug.ShowWarning("Please choose your starter faction.");
                    cFactionComboBox.Focus();
                    return;
                }

                FactionInfo faction = ((CampaignFaction)cFactionComboBox.SelectedItem).Faction;
                if (faction == null || faction.Index == 0 || string.IsNullOrWhiteSpace(faction.Name))
                {
                    Debug.ShowWarning("Invalid starter faction.");
                    cFactionComboBox.Focus();
                    return;
                }

                CreationArgs args = new CreationArgs();
                args.Installation = (Installation)cInstallListView.SelectedItem;
                args.Mod = (ModFolder)cModComboBox.SelectedItem;
                args.Campaign = (Campaign)cCampaignComboBox.SelectedItem;
                args.Name = name;
                args.TransferPath = transferPath;
                args.Difficulty = difficulty;
                args.StartFaction = faction;
                args.FreeFactions = cSelectedFactionsListBox.Items.Cast<CampaignFaction>().Select(f => f.Faction);
                args.AutoSolve = (bool)cAutoSolveComboBox.IsChecked;
                args.AutoManage = (bool)cAutoManageComboBox.IsChecked;
                Session.Create(args);

                Settings.LastTransferPath = transferPath;
                Settings.Save();

                ShowMenu(Menus.GameList);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        #endregion

        #region Choose Faction

        void ShowFactionMenu(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null)
                    return;

                ShowMenu(Menus.Faction);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void GameSession_OnFactionsChange(Session obj)
        {
            if (currentMenu == Menus.Faction && obj == gGamesListView.SelectedItem)
                RefreshFreeFactions();
        }

        void RefreshFreeFactions()
        {
            Session session = (Session)gGamesListView.SelectedItem;
            if (session == null)
                return;

            FactionInfo playerFaction = session.GetPlayerFaction();

            fFactionComboBox.Items.Clear();
            if (playerFaction != null)
            {
                fFactionComboBox.Items.Add(playerFaction);
                fFactionComboBox.SelectedIndex = 0;
            }

            foreach (FactionInfo faction in session.GetFreeFactions().OrderBy(f => f.Index))
                fFactionComboBox.Items.Add(faction);

            if (fFactionComboBox.Items.Count == 0)
            {
                fFactionComboBox.Text = "Game is full.";
                fFactionComboBox.IsEnabled = false;
            }
            else
            {
                fFactionComboBox.IsEnabled = true;
            }
        }

        void ChooseFaction(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null)
                    return;

                object item = fFactionComboBox.SelectedItem;
                session.ChooseFaction(item is FactionInfo ? (FactionInfo)fFactionComboBox.SelectedItem : null);

                ShowMenu(Menus.GameList);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void DropOut(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null)
                    return;

                session.ChooseFaction(null);

                ShowMenu(Menus.GameList);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        #endregion

        #region Pick Installation

        void gPickButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null || session.State != Session.Status.Warn)
                    return;

                pTextBox.Text = session.Version.Name;
                pInstallListView.Items.Clear();
                pInstallListView.AddItems(Settings.Installations.Where(i => session.IsAcceptableInstallation(i)));
                if (pInstallListView.Items.Count == 0)
                {
                    Debug.ShowWarning("No suitable installations found!");
                    return;
                }
                pInstallListView.SelectedIndex = 0;
                ShowMenu(Menus.Pick);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        void pAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Session session = (Session)gGamesListView.SelectedItem;
                if (session == null || session.State != Session.Status.Warn)
                    return;

                Installation installation = (Installation)pInstallListView.SelectedItem;
                if (installation == null)
                    return;

                session.SetInstallation(installation);

                ShowMenu(Menus.GameList);
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }

        #endregion

        void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.Delete && currentMenu == Menus.GameList)
                {
                    Session session = (Session)gGamesListView.SelectedItem;
                    if (session == null)
                        return;

                    RemoveGame(null, null);
                }
            }
            catch (Exception exc)
            {
                Debug.ShowException(exc);
            }
        }
    }
}
