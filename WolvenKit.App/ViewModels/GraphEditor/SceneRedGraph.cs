using System;
using System.Collections.Generic;
using System.Linq;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.Documents;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene.Internal;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor;

public class SceneRedGraph : RedGraph
{
    private static List<Type>? s_sceneNodeTypes;
    private readonly scnSceneResource _data;
    private uint _currectSceneNodeID;

    public SceneRedGraph(string title, scnSceneResource data, RedDocumentViewModel file,
        ILoggerService log, INodeWrapperFactory nodeWrapperFactory )
        : base(title, file, log, nodeWrapperFactory)
    {
        _data = data;
    }

    public override void GenerateGraph()
    {
        if (_data.SceneGraph == null)
        {
            _loggerService.Debug("Scene does not have any existing graph.");
            return;
        }

        var nodeCache = new Dictionary<uint, BaseSceneViewModel>();
        foreach (var nodeHandle in _data.SceneGraph.Chunk!.Graph)
        {
            ArgumentNullException.ThrowIfNull(nodeHandle.Chunk);

            var node = nodeHandle.Chunk;

            var nvm = WrapNode(node, false);
            if (nodeCache.TryAdd(nvm.UniqueId, nvm))
            {
                Nodes.Add(nvm);
                _currectSceneNodeID = Math.Max(_currectSceneNodeID, nvm.UniqueId);
            }
            else
            {
                _loggerService.Warning($"Duplicate node ID: {nvm.UniqueId}. Some nodes may be missing in the graph view. File: {Title}");
            }
        }

        foreach (BaseSceneViewModel node in Nodes)
        {
            foreach (SceneOutputConnectorViewModel outputConnector in node.Output)
            {
                foreach (var destination in outputConnector.Data.Destinations)
                {
                    if (!nodeCache.TryGetValue(destination.NodeId.Id, out var targetNode))
                    {
                        _loggerService.Error($"NodeId {destination.NodeId.Id} is missing. Delete all existing connections to this NodeId.");
                        continue;
                    }

                    if (targetNode is IDynamicInputNode dynamicInputNode)
                    {
                        while (dynamicInputNode.Input.Count <= destination.IsockStamp.Ordinal)
                        {
                            dynamicInputNode.AddInput();
                        }
                    }

                    if (destination.IsockStamp.Ordinal >= targetNode.Input.Count)
                    {
                        _loggerService.Warning($"Output isock ordinal ({destination.IsockStamp.Ordinal}) of node {node.UniqueId} " +
                            $"is higher than node {targetNode.UniqueId} input max ordinal ({targetNode.Input.Count - 1}). " +
                            $"Some connections may be missing in graph view. File: " + Title);
                        continue;
                    }

                    Connections.Add(new SceneConnectionViewModel(outputConnector, targetNode.Input[destination.IsockStamp.Ordinal]));
                }
            }
        }
    }

    public override void RecalculateSockets(IGraphProvider nodeViewModel) => throw new NotImplementedException();

    public override void AddConnection(OutputConnectorViewModel source, InputConnectorViewModel target)
    {
        var sceneSource = (SceneOutputConnectorViewModel)source;
        var sceneTarget = (SceneInputConnectorViewModel)target;

        var input = new scnInputSocketId
        {
            NodeId = new scnNodeId
            {
                Id = sceneTarget.OwnerId
            },
            IsockStamp = new scnInputSocketStamp
            {
                Name = 0,
                Ordinal = sceneTarget.Ordinal
            }
        };

        sceneSource.Data.Destinations.Add(input);
        Connections.Add(new SceneConnectionViewModel(sceneSource, sceneTarget));
        RefreshCVM(sceneSource.Data);
    }

    public override void RemoveConnection(ConnectionViewModel connection)
    {
        var sceneConnection = (SceneConnectionViewModel)connection;
        var sceneSource = (SceneOutputConnectorViewModel)sceneConnection.Source;
        var sceneTarget = (SceneInputConnectorViewModel)connection.Target;

        for (var i = sceneSource.Data.Destinations.Count - 1; i >= 0; i--)
        {
            var socket = sceneSource.Data.Destinations[i];
            if (socket.NodeId.Id == sceneTarget.OwnerId && socket.IsockStamp.Ordinal == sceneTarget.Ordinal)
            {
                sceneSource.Data.Destinations.RemoveAt(i);
                sceneSource.IsConnected = sceneSource.Data.Destinations.Count > 0;
                break;
            }
        }

        sceneTarget.IsConnected = false;
        foreach (var node in Nodes)
        {
            foreach (SceneOutputConnectorViewModel output in node.Output)
            {
                foreach (var destination in output.Data.Destinations)
                {
                    if (destination.NodeId.Id == sceneTarget.OwnerId && node.Output.Count > 0)
                    {
                        sceneTarget.IsConnected = true;
                        return;
                    }
                }
            }
        }

        Connections.Remove(sceneConnection);
        RefreshCVM(sceneSource.Data);
    }

    public override void CreateNode(Type type, System.Windows.Point point)
    {
        var instance = CreateNodeInstance(type);
        var nvm = WrapNode(instance, true);
        nvm.Location = point;

        _data.SceneGraph.Chunk!.Graph.Add(new CHandle<scnSceneGraphNode>(instance));

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

        var graph = _data.SceneGraph.Chunk!.Graph!;
        for (var i = graph.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(graph[i].Chunk, node.Data))
            {
                graph.RemoveAt(i);

                if (GetNodesChunkViewModel() is { } nodes)
                {
                    nodes.RecalculateProperties();
                }
            }
        }

        Nodes.Remove(node);
    }

    public override List<Type> GetNodeTypes() => s_sceneNodeTypes ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(scnSceneGraphNode).IsAssignableFrom(x) && !x.IsAbstract)
                .ToList();

    public override string GetCleanTypeName(string typeName)
    {
        if (typeName.StartsWith("scn"))
        {
            typeName = typeName[3..];
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

        if (dataViewModel.Chunks[0].GetPropertyFromPath("sceneGraph.graph") is not { } nodes)
        {
            return null;
        }

        return nodes;
    }

    public override uint GetNodeId(NodeViewModel node) => ((scnSceneGraphNode)node.Data).NodeId.Id;

    public BaseSceneViewModel WrapNode(scnSceneGraphNode node, bool isNew)
    {
        var nvm = (BaseSceneViewModel)_nodeWrapperFactory.CreateViewModel(node, _data);

        if (isNew)
        {
            nvm.CreateDefaultState();
        }

        nvm.GenerateSockets();

        return nvm;
    }

    public void RefreshCVM(scnOutputSocket socket)
    {
        if (GetNodesChunkViewModel() is { } nodes)
        {
            var list = new List<ChunkViewModel>();
            foreach (var property in nodes.GetAllProperties())
            {
                if (ReferenceEquals(property.Data, socket.Destinations))
                {
                    list.Add(property.Parent!);
                }
            }
            foreach (var model in list)
            {
                model.RecalculateProperties();
            }
        }
    }

    private scnSceneGraphNode CreateNodeInstance(Type type)
    {
        var instance = System.Activator.CreateInstance(type);
        if (instance is not scnSceneGraphNode sceneNode)
        {
            throw new Exception();
        }

        sceneNode.NodeId.Id = ++_currectSceneNodeID;

        return sceneNode;
    }
}
