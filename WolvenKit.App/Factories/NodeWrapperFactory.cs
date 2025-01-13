using WolvenKit.App.Controllers;
using WolvenKit.App.ViewModels.GraphEditor;
using WolvenKit.App.ViewModels.GraphEditor.Nodes;
using WolvenKit.Common;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.Factories;

public class NodeWrapperFactory : INodeWrapperFactory
{
    private readonly ILoggerService _loggerService;
    private readonly IArchiveManager _archiveManager;
    private readonly IGameControllerFactory _gameController;

    public NodeWrapperFactory(ILoggerService loggerService, IArchiveManager archiveManager, IGameControllerFactory gameController)
    {
        _loggerService = loggerService;
        _archiveManager = archiveManager;
        _gameController = gameController;
    }

    public NodeViewModel CreateViewModel(IRedType node, IRedType resource)
    {
        if (node is questNodeDefinition questNode && resource is questQuestPhaseResource questResource)
        {
            return QuestNodeWrapperFactory.CreateViewModel(questNode, questResource, _loggerService, this, _gameController, _archiveManager);
        }
        if (node is scnSceneGraphNode sceneNode && resource is scnSceneResource sceneResource)
        {
            return SceneNodeWrapperFactory.CreateViewModel(sceneNode, sceneResource);
        }
        throw new System.ArgumentException($"Node '{node.GetType()}' cannot be wrapped due to unexisting NodeWrapperFactory.");
    }
}