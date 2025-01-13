using System;
using System.Collections.Generic;
using System.Linq;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene.Internal;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene;

public class scnEndNodeWrapper : BaseSceneViewModel<scnEndNode>
{
    private readonly scnExitPoint _scnExitPoint;

    public string Name
    {
        get => _scnExitPoint.Name.GetResolvedText()!;
        set => _scnExitPoint.Name = value;
    }

    public Enums.scnEndNodeNsType Type
    {
        get => _castedData.Type;
        set => _castedData.Type = value;
    }

    public IEnumerable<Enums.scnEndNodeNsType> Types => Enum.GetValues(typeof(Enums.scnEndNodeNsType)).Cast<Enums.scnEndNodeNsType>();

    public scnEndNodeWrapper(scnEndNode scnEndNode, scnSceneResource resource) : base(scnEndNode)
    {
        var endPoint = resource
                .ExitPoints
                .FirstOrDefault(x => x.NodeId.Id == scnEndNode.NodeId.Id);

        if (endPoint == null)
        {
            endPoint = new scnExitPoint { NodeId = new scnNodeId { Id = scnEndNode.NodeId.Id } };
            resource.ExitPoints.Add(endPoint);
        }
        _scnExitPoint = endPoint;
    }

    internal override void GenerateSockets() => Input.Add(new SceneInputConnectorViewModel("In", "In", UniqueId, 0));
}