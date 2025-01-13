using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene.Internal;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene;
using WolvenKit.RED4.Types;
using System;

namespace WolvenKit.App.ViewModels.GraphEditor.Nodes;

public static class SceneNodeWrapperFactory
{
    public static BaseSceneViewModel CreateViewModel(scnSceneGraphNode graphNode, scnSceneResource resource)
    {
        return graphNode switch
        {
            scnAndNode node => new scnAndNodeWrapper(node),
            scnChoiceNode node => new scnChoiceNodeWrapper(node, resource),
            scnCutControlNode node => new scnCutControlNodeWrapper(node),
            scnDeletionMarkerNode node => new scnDeletionMarkerNodeWrapper(node),
            scnEndNode node => new scnEndNodeWrapper(node, resource),
            scnHubNode node => new scnHubNodeWrapper(node),
            scnInterruptManagerNode node => new scnInterruptManagerNodeWrapper(node),
            scnQuestNode node => new scnQuestNodeWrapper(node, resource),
            scnRandomizerNode node => new scnRandomizerNodeWrapper(node),
            scnRewindableSectionNode node => new scnRewindableSectionNodeWrapper(node, resource),
            scnSectionNode node => new scnSectionNodeWrapper(node, resource),
            scnStartNode node => new scnStartNodeWrapper(node, resource),
            scnXorNode node => new scnXorNodeWrapper(node),
            scnSceneGraphNode => new scnSceneGraphNodeWrapper(graphNode),
            _ => throw new ArgumentException($"Unsupported node type: {graphNode.GetType().Name}")
        };
    }
}
