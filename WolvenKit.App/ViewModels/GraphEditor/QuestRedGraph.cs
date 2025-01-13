using System;
using System.Collections.Generic;
using System.Linq;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.Documents;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Quest;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Quest.Internal;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor;

public class QuestRedGraph : RedGraph
{
    private static List<Type>? s_questNodeTypes;
    private readonly questQuestPhaseResource _data;
    private ushort _currentQuestNodeID;

    public QuestRedGraph(string title, questQuestPhaseResource data, RedDocumentViewModel file,
        ILoggerService log, INodeWrapperFactory nodeWrapperFactory)
        : base(title, file, log, nodeWrapperFactory)
    {
        _data = data;
    }

    public override void GenerateGraph()
    {
        if (_data.Graph == null)
        {
            _loggerService.Debug("Quest does not have any existing graph.");
            return;
        }

        var socketNodeLookup = new Dictionary<graphGraphSocketDefinition, QuestInputConnectorViewModel>();
        var nodeCache = new Dictionary<uint, BaseQuestViewModel>();
        foreach (var nodeHandle in _data.Graph.Chunk!.Nodes)
        {
            ArgumentNullException.ThrowIfNull(nodeHandle.Chunk);

            var node = nodeHandle.Chunk;

            if (node is questNodeDefinition nodeDefinition)
            {
                _currentQuestNodeID = Math.Max(_currentQuestNodeID, nodeDefinition.Id);
            }

            var nvm = WrapNode(node, false);
            if (nodeCache.TryAdd(nvm.UniqueId, nvm))
            {
                Nodes.Add(nvm);
            }
            else
            {
                _loggerService.Warning($"Duplicate node ID: {nvm.UniqueId}. Some nodes may be missing in the graph view. File: {Title}");
            }
            
            foreach (QuestInputConnectorViewModel inputConnector in nvm.Input)
            {
                socketNodeLookup.Add(inputConnector.Data, inputConnector);
            }
        }

        foreach (BaseQuestViewModel node in Nodes)
        {
            foreach (QuestOutputConnectorViewModel outputConnector in node.Output)
            {
                foreach (var connectionHandle in outputConnector.Data.Connections)
                {
                    var connection = connectionHandle.Chunk!;
                    Connections.Add(new QuestConnectionViewModel(outputConnector, socketNodeLookup[connection.Destination.Chunk!], connection));
                }
            }
        }
    }

    public override void RecalculateSockets(IGraphProvider nodeViewModel)
    {
        if (nodeViewModel is not BaseQuestViewModel nvm)
        {
            return;
        }

        var inputCache = new Dictionary<string, List<QuestOutputConnectorViewModel>>();
        var outputCache = new Dictionary<string, List<QuestInputConnectorViewModel>>();
        foreach (QuestInputConnectorViewModel inputConnector in nvm.Input)
        {
            if (!inputConnector.IsConnected)
            {
                continue;
            }

            var connectionList = new List<QuestOutputConnectorViewModel>();
            foreach (var connection in inputConnector.Data.Connections)
            {
                var source = connection.Chunk!.Source.Chunk!;

                for (var i = source.Connections.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(source.Connections[i].Chunk, connection.Chunk))
                    {
                        source.Connections.RemoveAt(i);
                    }
                }

                for (var i = Connections.Count - 1; i >= 0; i--)
                {
                    var outputConnector = (QuestOutputConnectorViewModel)Connections[i].Source;

                    if (ReferenceEquals(outputConnector.Data, source) &&
                        ReferenceEquals(Connections[i].Target, inputConnector))
                    {
                        outputConnector.IsConnected = outputConnector.Data.Connections.Count > 0;

                        connectionList.Add(outputConnector);
                        Connections.RemoveAt(i);
                        break;
                    }
                }
            }
            inputCache.Add(inputConnector.Name, connectionList);
        }

        foreach (QuestOutputConnectorViewModel outputConnector in nvm.Output)
        {
            if (!outputConnector.IsConnected)
            {
                continue;
            }

            var connectionList = new List<QuestInputConnectorViewModel>();
            foreach (var connection in outputConnector.Data.Connections)
            {
                var destination = connection.Chunk!.Destination.Chunk!;

                for (var i = destination.Connections.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(destination.Connections[i].Chunk, connection.Chunk))
                    {
                        destination.Connections.RemoveAt(i);
                    }
                }

                for (var i = Connections.Count - 1; i >= 0; i--)
                {
                    var inputConnector = (QuestInputConnectorViewModel)Connections[i].Target;

                    if (ReferenceEquals(Connections[i].Source, outputConnector) &&
                        ReferenceEquals(inputConnector.Data, destination))
                    {
                        inputConnector.IsConnected = inputConnector.Data.Connections.Count > 0;

                        connectionList.Add(inputConnector);
                        Connections.RemoveAt(i);
                        break;
                    }
                }
            }
            outputCache.Add(outputConnector.Name, connectionList);
        }

        nodeViewModel.RecalculateSockets();

        foreach (QuestInputConnectorViewModel inputConnector in nvm.Input)
        {
            if (inputCache.TryGetValue(inputConnector.Name, out var sockets))
            {
                foreach (var outputConnector in sockets)
                {
                    AddConnection(outputConnector, inputConnector);
                }
            }
        }

        foreach (QuestOutputConnectorViewModel outputConnector in nvm.Output)
        {
            if (outputCache.TryGetValue(outputConnector.Name, out var sockets))
            {
                foreach (var inputConnector in sockets)
                {
                    AddConnection(outputConnector, inputConnector);
                }
            }
        }
    }

    public override void AddConnection(OutputConnectorViewModel source, InputConnectorViewModel target)
    {
        var questSource = (QuestOutputConnectorViewModel)source;
        var questTarget = (QuestInputConnectorViewModel)target;

        var connection = new graphGraphConnectionDefinition()
        {
            Source = new CWeakHandle<graphGraphSocketDefinition>(questSource.Data),
            Destination = new CWeakHandle<graphGraphSocketDefinition>(questTarget.Data)
        };

        questSource.Data.Connections.Add(new CHandle<graphGraphConnectionDefinition>(connection));
        questTarget.Data.Connections.Add(new CHandle<graphGraphConnectionDefinition>(connection));

        Connections.Add(new QuestConnectionViewModel(questSource, questTarget, connection));
        RefreshCVM([questSource.Data, questTarget.Data]);
    }

    public override void RemoveConnection(ConnectionViewModel connection)
    {
        var questConnection = (QuestConnectionViewModel)connection;
        var questSource = (QuestOutputConnectorViewModel)connection.Source;
        var questTarget = (QuestInputConnectorViewModel)connection.Target;

        for (var i = questSource.Data.Connections.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(questSource.Data.Connections[i].Chunk, questConnection.ConnectionDefinition))
            {
                questSource.Data.Connections.RemoveAt(i);
                questSource.IsConnected = questSource.Data.Connections.Count > 0;
                break;
            }
        }

        for (var i = questTarget.Data.Connections.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(questTarget.Data.Connections[i].Chunk, questConnection.ConnectionDefinition))
            {
                questTarget.Data.Connections.RemoveAt(i);
                questTarget.IsConnected = questTarget.Data.Connections.Count > 0;
                break;
            }
        }
        Connections.Remove(questConnection);
        RefreshCVM([questSource.Data, questTarget.Data]);
    }

    public override void CreateNode(Type type, System.Windows.Point point)
    {
        var instance = CreateNodeInstance(type);
        var nvm = WrapNode(instance, true);
        nvm.Location = point;

        _data.Graph.Chunk!.Nodes.Add(new CHandle<graphGraphNodeDefinition>(instance));

        if (GetNodesChunkViewModel() is { } nodes)
        {
            nodes.RecalculateProperties();
        }

        Nodes.Add(nvm);
    }

    public override void RemoveNode(NodeViewModel node)
    {
        foreach (var inputConnectorViewModel in node.Input)
        {
            DisconnectNode(inputConnectorViewModel);
        }

        foreach (var outputConnectorViewModel in node.Output)
        {
            DisconnectNode(outputConnectorViewModel);
        }

        for (var i = _data.Graph.Chunk!.Nodes.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(_data.Graph.Chunk!.Nodes[i].Chunk, node.Data))
            {
                continue;
            }

            _data.Graph.Chunk!.Nodes.RemoveAt(i);

            if (GetNodesChunkViewModel() is { } nodeViews)
            {
                nodeViews.RecalculateProperties();
            }
            break;
        }

        Nodes.Remove(node);
    }

    public override List<Type> GetNodeTypes() => s_questNodeTypes ??= AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => typeof(graphGraphNodeDefinition).IsAssignableFrom(x) && !x.IsAbstract)
            .ToList();

    public override string GetCleanTypeName(string typeName)
    {
        if (typeName.StartsWith("quest"))
        {
            typeName = typeName[5..];
        }

        if (typeName.EndsWith("NodeDefinition"))
        {
            typeName = typeName[..^14];
        }
        else if (typeName.EndsWith("Node"))
        {
            typeName = typeName[..^4];
        }

        return typeName;
    }

    public override ChunkViewModel? GetNodesChunkViewModel()
    {
        if (DocumentViewModel?.GetMainFile() is not RDTDataViewModel dataViewModel)
        {
            return null;
        }

        if (dataViewModel.Chunks[0].GetPropertyFromPath("graph.nodes") is not { } nodes)
        {
            return null;
        }

        return nodes;
    }

    public override uint GetNodeId(NodeViewModel node) => ((questNodeDefinition)node.Data).Id;

    public BaseQuestViewModel WrapNode(graphGraphNodeDefinition node, bool isNew)
    {
        var nvm = (BaseQuestViewModel)_nodeWrapperFactory.CreateViewModel(node, _data);

        if (isNew)
        {
            nvm.CreateDefaultSockets();
        }

        nvm.GenerateSockets();

        return nvm;
    }

    public void RefreshCVM(questSocketDefinition[] sockets)
    {
        if (GetNodesChunkViewModel() is { } nodes)
        {
            var list = new List<ChunkViewModel>();
            foreach (var property in nodes.GetAllProperties())
            {
                foreach (var socket in sockets)
                {
                    if (ReferenceEquals(property.Data, socket.Connections))
                    {
                        list.Add(property.Parent!);
                    }
                }
            }
            foreach (var model in list)
            {
                model.RecalculateProperties();
            }
        }
    }

    private graphGraphNodeDefinition CreateNodeInstance(Type type)
    {
        var instance = System.Activator.CreateInstance(type);
        if (instance is not graphGraphNodeDefinition questNode)
        {
            throw new Exception();
        }

        if (instance is questNodeDefinition nodeDefinition)
        {
            nodeDefinition.Id = ++_currentQuestNodeID;
        }

        return questNode;
    }
}
