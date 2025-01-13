using System.Linq;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene.Internal;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene;

public class scnStartNodeWrapper : BaseSceneViewModel<scnStartNode>
{
    private readonly scnEntryPoint _scnEntryPoint;

    public string Name
    {
        get => _scnEntryPoint.Name.GetResolvedText()!;
        set => _scnEntryPoint.Name = value;
    }

    public scnStartNodeWrapper(scnStartNode scnStartNode, scnSceneResource resource) : base(scnStartNode)
    {
        var entryPoint = resource
                .EntryPoints
                .FirstOrDefault(x => x.NodeId.Id == scnStartNode.NodeId.Id);

        if (entryPoint == null)
        {
            entryPoint = new scnEntryPoint { NodeId = new scnNodeId { Id = scnStartNode.NodeId.Id } };
            resource.EntryPoints.Add(entryPoint);
        }
        _scnEntryPoint = entryPoint;
    }

    internal override void GenerateSockets()
    {
        for (var i = 0; i < _castedData.OutputSockets.Count; i++)
        {
            Output.Add(new SceneOutputConnectorViewModel($"Out{i}", $"Out{i}", UniqueId, _castedData.OutputSockets[i]));
        }
    }
}