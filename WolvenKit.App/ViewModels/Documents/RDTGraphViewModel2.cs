using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.GraphEditor;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.Documents;

public partial class RDTGraphViewModel2 : RedDocumentTabViewModel
{
    protected readonly IRedType _data;

    [ObservableProperty] private RedGraph? _mainGraph;

    [ObservableProperty] private bool _isLoaded;

    public RDTGraphViewModel2(IRedType data, RedDocumentViewModel file, INodeWrapperFactory nodeWrapperFactory) : base(file, "Graph View")
    {
        _data = data;

        if (_data is questQuestPhaseResource questResource)
        {
            _mainGraph = new QuestRedGraph(Parent.Header, questResource, file, file.GetLoggerService(), nodeWrapperFactory);
        }
        if (_data is scnSceneResource sceneResource)
        {
            _mainGraph = new SceneRedGraph(Parent.Header, sceneResource, file, file.GetLoggerService(), nodeWrapperFactory);
        }
    }

    //protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    //{
    //    if (MainGraph == null)
    //    {
    //        return;
    //    }

    //    if (e.PropertyName == nameof(MainGraph))
    //    {
    //        History.Clear();
    //    }

    //    base.OnPropertyChanging(e);
    //}

    //protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    //{
    //    if (MainGraph == null)
    //    {
    //        return;
    //    }

    //    if (e.PropertyName == nameof(MainGraph))
    //    {
    //        History.Add(MainGraph);
    //    }

    //    base.OnPropertyChanged(e);
    //}

    public override ERedDocumentItemType DocumentItemType => ERedDocumentItemType.MainFile;

    public List<RedGraph> History { get; } = new();

    public override void Load()
    {
        if (IsLoaded)
        {
            return;
        }

        MainGraph?.GenerateGraph();

        IsLoaded = true;
    }
}