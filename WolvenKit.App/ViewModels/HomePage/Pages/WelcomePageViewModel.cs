using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using Splat;
using WolvenKit.Core.Extensions;
using WolvenKit.Functionality.Helpers;
using WolvenKit.Functionality.ProjectManagement;
using WolvenKit.Functionality.WKitGlobal;
using WolvenKit.Interaction;
using WolvenKit.ViewModels.HomePage;
using WolvenKit.ViewModels.Shell;

namespace WolvenKit.ViewModels.Shared
{
    public partial class WelcomePageViewModel : PageViewModel
    {
        #region Fields

        private readonly IRecentlyUsedItemsService _recentlyUsedItemsService;
        private readonly AppViewModel _mainViewModel;

        private readonly ReadOnlyObservableCollection<RecentlyUsedItemModel> _recentlyUsedItems;

        #endregion Fields

        #region Constructors

        public WelcomePageViewModel(
            IRecentlyUsedItemsService recentlyUsedItemsService
            )
        {

            _mainViewModel = Locator.Current.GetService<AppViewModel>().NotNull();

            _recentlyUsedItemsService = recentlyUsedItemsService;

            OpenProjectCommand = ReactiveCommand.Create<string>(s => _mainViewModel.OpenProjectCommand.Execute(s).Subscribe());
            DeleteProjectCommand = ReactiveCommand.Create<string>(s => _mainViewModel.DeleteProjectCommand.Execute(s).Subscribe());

#pragma warning disable IDE0053 // Doesn't compile with lambda expressions
            NewProjectCommand = ReactiveCommand.Create(() =>
            {
                _mainViewModel.NewProjectCommand.Execute().Subscribe();
            });
#pragma warning restore IDE0053 // Doesn't compile with lambda expressions

            recentlyUsedItemsService.Items
                .Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _recentlyUsedItems)
                .Subscribe(OnRecentlyUsedItemsChanged);
        }

        private void OnRecentlyUsedItemsChanged(IChangeSet<RecentlyUsedItemModel, string> obj) => ConvertRecentProjects();

        #endregion Constructors

        #region Properties


        public string DiscordLink = "https://discord.gg/tKZXma5SaA";
        public string OpenCollectiveLink = "https://opencollective.com/redmodding";
        public string PatreonLink = "https://www.patreon.com/m/RedModdingTools";
        public string TwitterLink = "https://twitter.com/ModdingRed";
        public string YoutubeLink = "https://www.youtube.com/channel/UCl3JpsP49JgYLMYAYQvoaLg";


        [ObservableProperty] private ObservableCollection<FancyProjectObject> _fancyProjects  = new();

        [ObservableProperty] private List<RecentlyUsedItemModel> _pinnedItems = new();        

        public ReactiveCommand<string, Unit> OpenProjectCommand { get; }
        public ReactiveCommand<string, Unit> DeleteProjectCommand { get; }
        public ReactiveCommand<Unit, Unit> NewProjectCommand { get; }

        public readonly ReactiveCommand<string, Unit> OpenLinkCommand = ReactiveCommand.Create<string>(
            link =>
            {
                var ps = new ProcessStartInfo(link)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            });


        #endregion Properties

        [RelayCommand]
        private async void OpenInExplorer(string parameter)
        {
            if (!File.Exists(parameter))
            {
                parameter = await LocateMissingProjectAsync(parameter);
            }

            if (string.IsNullOrEmpty(parameter))
            {
                return;
            }

            Process.Start("explorer.exe", $"/select, \"{parameter}\"");
        }


        private async Task<string> LocateMissingProjectAsync(string parameter)
        {
            var result = await Interactions.ShowMessageBoxAsync("The file doesn't seem to exist. Would you like to locate it?", "Project not found");
            var delete = false;
            switch (result)
            {
                case WMessageBoxResult.OK:
                case WMessageBoxResult.Yes:
                    delete = true;
                    break;
                case WMessageBoxResult.None:
                case WMessageBoxResult.Cancel:
                case WMessageBoxResult.No:
                case WMessageBoxResult.Custom:
                default:
                    break;
            }
            if (!delete)
            {
                var items = _recentlyUsedItemsService.Items.Items
                    .Where(_ => _.Name == parameter)
                    .ToList();
                if (items.Count > 0)
                {
                    var f = items.FirstOrDefault();
                    if (f is not null)
                    {
                        _recentlyUsedItemsService.RemoveItem(f);
                    }
                }
                return "";
            }
            else
            {
                var dlg = new CommonOpenFileDialog
                {
                    AllowNonFileSystemItems = false,
                    Multiselect = false,
                    IsFolderPicker = false,
                    Title = "Locate the WolvenKit project"
                };
                dlg.Filters.Add(new CommonFileDialogFilter("Cyberpunk 2077 Project", "*.cpmodproj"));

                if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return "";
                }

                var file = dlg.FileName;
                if (string.IsNullOrEmpty(file))
                {
                    return "";
                }

                parameter = file;

                var items = _recentlyUsedItemsService.Items.Items
                    .Where(_ => Path.GetFileName(_.Name) == Path.GetFileName(parameter))
                    .ToList();
                if (items.Count > 0)
                {
                    var item = items.First();
                    _recentlyUsedItemsService.AddItem(new RecentlyUsedItemModel(parameter, item.DateTime, item.Modified));
                    _recentlyUsedItemsService.RemoveItem(item);
                    return parameter;
                }
            }

            return "";
        }

        private void ConvertRecentProjects() // Converts Recent projects for the homepage.
        {
            DispatcherHelper.RunOnMainThread(() => FancyProjects.Clear());

            var sorted = _recentlyUsedItems.ToList();
            sorted.Sort(delegate (RecentlyUsedItemModel a, RecentlyUsedItemModel b)
            {
                DateTime ad, bd;
                if (a.Modified != default)
                {
                    ad = a.Modified;
                }
                else
                {
                    ad = a.DateTime;
                }

                if (b.Modified != default)
                {
                    bd = b.Modified;
                }
                else
                {
                    bd = b.DateTime;
                }

                return bd.CompareTo(ad);
            });

            foreach (var item in sorted)
            {
                var fi = new FileInfo(item.Name);

                var n = item.Name;
                var cd = item.Modified != default ? item.Modified : item.DateTime;
                var p = item.Name;

                var newfo = fi.Name.Split('.');
                var newfi = fi.Directory + "\\" + newfo[0] + "\\" + "img.png";

                var IsThere = File.Exists(newfi);
                File.GetLastWriteTime(item.Name);

                FancyProjectObject NewItem;
                if (Path.GetExtension(item.Name).TrimStart('.') == EProjectType.cpmodproj.ToString())
                {
                    if (!IsThere)
                    { newfi = "pack://application:,,,/Resources/Media/Images/Application/V Male Logo Cropped.png"; }
                    NewItem = new FancyProjectObject(fi.Name, cd, "Cyberpunk 2077", p, newfi);
                    DispatcherHelper.RunOnMainThread(() => FancyProjects.Add(NewItem));
                }
                if (Path.GetExtension(item.Name).TrimStart('.') == EProjectType.w3modproj.ToString())
                {
                    if (!IsThere)
                    { newfi = "pack://application:,,,/Resources/Media/Images/Application/tw3proj.png"; }

                    NewItem = new FancyProjectObject(n, cd, "The Witcher 3", p, newfi);
                    DispatcherHelper.RunOnMainThread(() => FancyProjects.Add(NewItem));
                }
            }
        }

        [RelayCommand]
        private void PinItem(string parameter) => _recentlyUsedItemsService.PinItem(parameter);

        [RelayCommand]
        private void CloseHomePage() => _mainViewModel.CloseModalCommand.Execute(null);

        //private void OnRecentlyUsedItemsServiceUpdated(object sender, EventArgs e)
        //{
        //    UpdateRecentlyUsedItems();
        //    UpdatePinnedItem();
        //}

        [RelayCommand]
        private void UnpinItem(string parameter)
        {
            //Argument.IsNotNullOrWhitespace(() => parameter);

            //_recentlyUsedItemsService.UnpinItem(parameter);
        }

        //private void UpdatePinnedItem() => PinnedItems = new List<RecentlyUsedItem>(_recentlyUsedItemsService.PinnedItems);

        //private void UpdateRecentlyUsedItems()
        //{
        //    //RecentlyUsedItems = new List<RecentlyUsedItem>(_recentlyUsedItemsService.Items);
        //    //ConvertRecentProjects();
        //}



        public class FancyProjectObject : ObservableObject
        {
            #region Constructors

            public FancyProjectObject(string name, DateTime createdate, string type, string path, string image)
            {
                Name = name;
                CreationDate = createdate;
                Type = type;
                ProjectPath = path;
                Image = image;
                SafeName = Path.GetFileNameWithoutExtension(name);
            }

            #endregion Constructors

            #region Properties

            public DateTime CreationDate { get; set; }
            public string Image { get; set; }
            public DateTime LastEditDate { get; set; }
            public string Name { get; set; }

            public string ProjectColor => ((uint)string.GetHashCode(ProjectPath) % 7).ToString();

            public string SafeName { get; set; }
            public string ProjectPath { get; set; }
            public string Type { get; set; }

            #endregion Properties
        }
    }
}
