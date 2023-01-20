using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using WolvenKit.Core.Extensions;
using WolvenKit.Core.Interfaces;
using WolvenKit.Functionality.Services;
using WolvenKit.ViewModels.Dialogs;

namespace WolvenKit.ViewModels.HomePage
{


    public class PluginsToolViewModel : DialogViewModel
    {
        private readonly ILoggerService _logger;
        [Reactive] public IPluginService _pluginService { get; set; }


        public delegate Task ReturnHandler(PluginsToolViewModel? file);
        public ReturnHandler? FileHandler;

        public PluginsToolViewModel()
        {
            _logger = Locator.Current.GetService<ILoggerService>().NotNull();
            _pluginService = Locator.Current.GetService<IPluginService>().NotNull();

            CancelCommand = ReactiveCommand.Create(() => {
                FileHandler?.Invoke(null);
            });
            SyncCommand = ReactiveCommand.CreateFromTask(SyncAsync);
        }


        [Reactive] public PluginViewModel? SelectedPlugin { get; set; }


        public override ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> SyncCommand { get; set; }

        public override ReactiveCommand<Unit, Unit> OkCommand => throw new NotImplementedException();

        public async Task SyncAsync() => await _pluginService.CheckForUpdatesAsync();

    }


}
