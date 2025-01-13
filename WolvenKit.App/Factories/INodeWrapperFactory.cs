using WolvenKit.App.ViewModels.GraphEditor;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.Factories;

public interface INodeWrapperFactory
{
    public NodeViewModel CreateViewModel(IRedType node, IRedType resource);
}