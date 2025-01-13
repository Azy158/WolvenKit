using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.Documents;
using WolvenKit.App.ViewModels.GraphEditor.Nodes;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.Core.Interfaces;

namespace WolvenKit.App.ViewModels.GraphEditor;

// TODO: Figure out how to dispose the objects only when the tab/file is closed and not unload.
public abstract class RedGraph
{
    protected readonly ILoggerService _loggerService;
    protected readonly INodeWrapperFactory _nodeWrapperFactory;

    public string Title { get; }
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
    public PendingConnectionViewModel PendingConnection { get; } = new();
    public RedGraphLayoutManager LayoutManager { get; }

    public RedDocumentViewModel DocumentViewModel { get; set; }

    public ICommand ConnectNodeCommand { get; }
    public ICommand DisconnectNodeCommand { get; }
    public ICommand NodesDragCompletedCommand { get; }

    public RedGraph(string title, RedDocumentViewModel file, ILoggerService log, INodeWrapperFactory nodeWrapperFactory)
    {
        _loggerService = log;
        _nodeWrapperFactory = nodeWrapperFactory;
        Title = title;
        LayoutManager = new RedGraphLayoutManager(this);
        DocumentViewModel = file;

        ConnectNodeCommand = new RelayCommand(ConnectNode);
        DisconnectNodeCommand = new RelayCommand<BaseConnectorViewModel>(DisconnectNode);
        NodesDragCompletedCommand = new RelayCommand(NodesDragCompleted);

    }

    private void ConnectNode()
    {
        if (PendingConnection.Target is IDynamicInputNode dynIn)
        {
            PendingConnection.Target = dynIn.AddInput();
        }

        if (PendingConnection.Target is IDynamicOutputNode dynOut)
        {
            PendingConnection.Target = dynOut.AddOutput();
        }

        if (PendingConnection is { Source: OutputConnectorViewModel output1, Target: InputConnectorViewModel input1 })
        {
            // If connection between the sockets already exist, act as removing.
            for (var i = Connections.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Connections[i].Source, output1) ||
                    !ReferenceEquals(Connections[i].Target, input1))
                {
                    continue;
                }
                RemoveConnection(Connections[i]);
                return;
            }
            AddConnection(output1, input1);
        }

        if (PendingConnection is { Source: InputConnectorViewModel input2, Target: OutputConnectorViewModel output2 })
        {
            // If connection between the sockets already exist, act as removing.
            for (var i = Connections.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Connections[i].Source, input2) ||
                    !ReferenceEquals(Connections[i].Target, output2))
                {
                    continue;
                }
                RemoveConnection(Connections[i]);
                return;
            }
            AddConnection(output2, input2);
        }
    }

    public void DisconnectNode(BaseConnectorViewModel? baseConnectorViewModel)
    {
        if (baseConnectorViewModel is OutputConnectorViewModel outputConnector)
        {
            for (var i = Connections.Count - 1; i >= 0; i--)
            {
                if (Connections[i].Source == outputConnector)
                {
                    RemoveConnection(Connections[i]);
                }
            }
        }

        if (baseConnectorViewModel is InputConnectorViewModel inputConnector)
        {
            for (var i = Connections.Count - 1; i >= 0; i--)
            {
                if (Connections[i].Target == inputConnector)
                {
                    RemoveConnection(Connections[i]);
                }
            }
        }
    }

    public void RemoveNodes(IList<object> nodes)
    {
        var removableNodes = new List<NodeViewModel>();
        foreach (var node in nodes)
        {
            if (node is not NodeViewModel nvm)
            {
                throw new Exception();
            }

            removableNodes.Add(nvm);
        }

        foreach (var node in removableNodes)
        {
            RemoveNode(node);
        }
    }

    public void NodesDragCompleted() => LayoutManager.SaveGraphLayout();

    public abstract void GenerateGraph();
    public abstract void RecalculateSockets(IGraphProvider nodeViewModel);

    public abstract void AddConnection(OutputConnectorViewModel source, InputConnectorViewModel target);
    public abstract void RemoveConnection(ConnectionViewModel connection);
    public abstract void CreateNode(Type type, System.Windows.Point point);
    public abstract void RemoveNode(NodeViewModel node);

    public abstract List<Type> GetNodeTypes();
    public abstract string GetCleanTypeName(string typeName);
    public abstract ChunkViewModel? GetNodesChunkViewModel();
    public abstract uint GetNodeId(NodeViewModel node);
}