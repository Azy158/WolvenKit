using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.GraphEditor;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.Documents;

public partial class RDTGraphViewModel2 : RedDocumentTabViewModel
{
    protected readonly IRedType _data;
    private readonly INodeWrapperFactory _nodeWrapperFactory;
    private readonly AppViewModel _appViewModel;

    [ObservableProperty] private RedGraph? _mainGraph;

    public ObservableCollection<RedGraph> History { get; } = new();

    public RDTGraphViewModel2(IRedType data, RedDocumentViewModel file, INodeWrapperFactory nodeWrapperFactory, AppViewModel appViewModel) : base(file, "Graph View")
    {
        _data = data;
        _nodeWrapperFactory = nodeWrapperFactory;
        _appViewModel = appViewModel;
        _appViewModel.DockedViews.CollectionChanged += DockedViews_CollectionChanged;
    }

    public void OpenSubGraph(GraphEditor.NodeViewModel nvm)
    {
        if (nvm.Data is questPhaseNodeDefinition ph)
        {
            if (ph.PhaseResource.IsSet)
            {
                // TODO: different file graph
            }
            else
            {
                var subGraph = new QuestRedGraph($"{MainGraph!.Title} [{ph.Id}]", ph.PhaseGraph.Chunk!, Parent, _nodeWrapperFactory);
                subGraph.SubGraphIdChain = $"{MainGraph!.SubGraphIdChain}.{ph.Id}";
                MainGraph = subGraph;
                MainGraph.GenerateGraph();
                History.Add(subGraph);
            }
        }
    }

    public void GoBackFromSubGraph(GraphEditor.NodeViewModel nvm)
    {
        if (History.Count <= 1)
        {
            return;
        }

        var graph = History[^1];
        graph.Dispose();
        History.RemoveAt(History.Count - 1);
        MainGraph = History[^1];
    }

    public void JumpToHistoryRedGraph(RedGraph graph)
    {
        if (History.Count <= 1)
        {
            return;
        }

        for (var i = History.Count - 1; i >= 0; i--)
        {
            var historyGraph = History[i];
            if (!ReferenceEquals(historyGraph, graph))
            {
                historyGraph.Dispose();
                History.RemoveAt(i);
                continue;
            }
            MainGraph = historyGraph;
            break;
        }
    }

    private void DockedViews_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Remove)
        {
            return;
        }

        foreach (var view in e.OldItems!)
        {
            if (!ReferenceEquals(view, Parent))
            {
                continue;
            }

            foreach (var graph in History)
            {
                graph.Dispose();
            }
            History.Clear();

            _appViewModel.DockedViews.CollectionChanged -= DockedViews_CollectionChanged;
        }
    }

    public override ERedDocumentItemType DocumentItemType => ERedDocumentItemType.MainFile;

    public override void Load()
    {
        if (MainGraph != null)
        {
            return;
        }

        if (_data is questQuestPhaseResource questResource && questResource.Graph != null)
        {
            MainGraph = new QuestRedGraph(Parent.Header, questResource.Graph.Chunk!, Parent, _nodeWrapperFactory);
            MainGraph.GenerateGraph();
            History.Add(MainGraph);
        }

        if (_data is scnSceneResource sceneResource && sceneResource.SceneGraph != null)
        {
            MainGraph = new SceneRedGraph(Parent.Header, sceneResource.SceneGraph.Chunk!, Parent, _nodeWrapperFactory);
            MainGraph.GenerateGraph();
            History.Add(MainGraph);
        }
    }
}