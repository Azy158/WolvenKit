using System;
using WolvenKit.App.Controllers;
using WolvenKit.App.Factories;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Quest;
using WolvenKit.Common;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor.Nodes;

public static class QuestNodeWrapperFactory
{
    public static BaseQuestViewModel CreateViewModel(questNodeDefinition graphNode, questQuestPhaseResource resource,
        ILoggerService loggerService, INodeWrapperFactory nodeWrapper, IGameControllerFactory gameController, IArchiveManager archiveManager)
    {
        return graphNode switch
        {
            questAchievementManagerNodeDefinition node => new questAchievementManagerNodeDefinitionWrapper(node),
            questAudioNodeDefinition node => new questAudioNodeDefinitionWrapper(node),
            questCharacterManagerNodeDefinition node => new questCharacterManagerNodeDefinitionWrapper(node),
            questCheckpointNodeDefinition node => new questCheckpointNodeDefinitionWrapper(node),
            questClearForcedBehavioursNodeDefinition node => new questClearForcedBehavioursNodeDefinitionWrapper(node),
            questCombatNodeDefinition node => new questCombatNodeDefinitionWrapper(node),
            questConditionNodeDefinition node => new questConditionNodeDefinitionWrapper(node),
            questCrowdManagerNodeDefinition node => new questCrowdManagerNodeDefinitionWrapper(node),
            questCutControlNodeDefinition node => new questCutControlNodeDefinitionWrapper(node),
            questDeletionMarkerNodeDefinition node => new questDeletionMarkerNodeDefinitionWrapper(node),
            questDynamicSpawnSystemNodeDefinition node => new questDynamicSpawnSystemNodeDefinitionWrapper(node),
            questEndNodeDefinition node => new questEndNodeDefinitionWrapper(node),
            questEntityManagerNodeDefinition node => new questEntityManagerNodeDefinitionWrapper(node),
            questEnvironmentManagerNodeDefinition node => new questEnvironmentManagerNodeDefinitionWrapper(node),
            questEquipItemNodeDefinition node => new questEquipItemNodeDefinitionWrapper(node),
            questEventManagerNodeDefinition node => new questEventManagerNodeDefinitionWrapper(node),
            questFactsDBManagerNodeDefinition node => new questFactsDBManagerNodeDefinitionWrapper(node),
            questFlowControlNodeDefinition node => new questFlowControlNodeDefinitionWrapper(node),
            questForcedBehaviourNodeDefinition node => new questForcedBehaviourNodeDefinitionWrapper(node),
            questFXManagerNodeDefinition node => new questFXManagerNodeDefinitionWrapper(node),
            questGameManagerNodeDefinition node => new questGameManagerNodeDefinitionWrapper(node),
            questInputNodeDefinition node => new questInputNodeDefinitionWrapper(node),
            questInstancedCrowdControlNodeDefinition node => new questInstancedCrowdControlNodeDefinitionWrapper(node),
            questInteractiveObjectManagerNodeDefinition node => new questInteractiveObjectManagerNodeDefinitionWrapper(node),
            questItemManagerNodeDefinition node => new questItemManagerNodeDefinitionWrapper(node),
            questJournalNodeDefinition node => new questJournalNodeDefinitionWrapper(node),
            questLogicalAndNodeDefinition node => new questLogicalAndNodeDefinitionWrapper(node),
            questLogicalHubNodeDefinition node => new questLogicalHubNodeDefinitionWrapper(node),
            questLogicalXorNodeDefinition node => new questLogicalXorNodeDefinitionWrapper(node),
            questMappinManagerNodeDefinition node => new questMappinManagerNodeDefinitionWrapper(node),
            questMiscAICommandNode node => new questMiscAICommandNodeWrapper(node),
            questMovePuppetNodeDefinition node => new questMovePuppetNodeDefinitionWrapper(node),
            questOutputNodeDefinition node => new questOutputNodeDefinitionWrapper(node),
            questPauseConditionNodeDefinition node => new questPauseConditionNodeDefinitionWrapper(node),
            questPhaseNodeDefinition node => new questPhaseNodeDefinitionWrapper(node, loggerService, nodeWrapper, archiveManager),
            questPhoneManagerNodeDefinition node => new questPhoneManagerNodeDefinitionWrapper(node),
            questPuppetAIManagerNodeDefinition node => new questPuppetAIManagerNodeDefinitionWrapper(node),
            questRandomizerNodeDefinition node => new questRandomizerNodeDefinitionWrapper(node),
            questRenderFxManagerNodeDefinition node => new questRenderFxManagerNodeDefinitionWrapper(node),
            questRewardManagerNodeDefinition node => new questRewardManagerNodeDefinitionWrapper(node),
            questSceneManagerNodeDefinition node => new questSceneManagerNodeDefinitionWrapper(node),
            questSceneNodeDefinition node => new questSceneNodeDefinitionWrapper(node, loggerService, gameController, archiveManager),
            questSendAICommandNodeDefinition node => new questSendAICommandNodeDefinitionWrapper(node),
            questSpawnManagerNodeDefinition node => new questSpawnManagerNodeDefinitionWrapper(node),
            questStartNodeDefinition node => new questStartNodeDefinitionWrapper(node),
            questSwitchNodeDefinition node => new questSwitchNodeDefinitionWrapper(node),
            questTimeManagerNodeDefinition node => new questTimeManagerNodeDefinitionWrapper(node),
            questTransformAnimatorNodeDefinition node => new questTransformAnimatorNodeDefinitionWrapper(node),
            questTeleportPuppetNodeDefinition node => new questTeleportPuppetNodeDefinitionWrapper(node),
            questTriggerManagerNodeDefinition node => new questTriggerManagerNodeDefinitionWrapper(node),
            questUIManagerNodeDefinition node => new questUIManagerNodeDefinitionWrapper(node),
            questUseWorkspotNodeDefinition node => new questUseWorkspotNodeDefinitionWrapper(node),
            questVehicleNodeDefinition node => new questVehicleNodeDefinitionWrapper(node),
            questVehicleNodeCommandDefinition node => new questVehicleNodeCommandDefinitionWrapper(node),
            questVisionModesManagerNodeDefinition node => new questVisionModesManagerNodeDefinitionWrapper(node),
            questVoicesetManagerNodeDefinition node => new questVoicesetManagerNodeDefinitionWrapper(node),
            questWorldDataManagerNodeDefinition node => new questWorldDataManagerNodeDefinitionWrapper(node),
            tempshitMapPinManagerNodeDefinition node => new tempshitMapPinManagerNodeDefinitionWrapper(node),
            questNodeDefinition node => new questNodeDefinitionWrapper(node),
            _ => throw new ArgumentException($"Unsupported node type: {graphNode.GetType().Name}")
        };
    }
}
