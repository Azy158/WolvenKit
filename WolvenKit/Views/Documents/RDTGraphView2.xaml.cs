using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WolvenKit.App.ViewModels.GraphEditor;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Quest;
using WolvenKit.App.ViewModels.GraphEditor.Nodes.Scene;

namespace WolvenKit.Views.Documents;
/// <summary>
/// Interaktionslogik für RDTGraphView2.xaml
/// </summary>
public partial class RDTGraphView2
{
    public RDTGraphView2()
    {
        InitializeComponent();

        KeyDown += OnKeyDown;
    }

    private void Editor_OnNodeDoubleClick(object sender, RoutedEventArgs e) => HandleSubGraph();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            HandleSubGraph();
        }
    }

    private void HandleSubGraph()
    {
        if (Editor.SelectedNode is IGraphProvider provider)
        {
            ViewModel.OpenSubGraph(Editor.SelectedNode);
        }

        if (Editor.SelectedNode is questInputNodeDefinitionWrapper or questOutputNodeDefinitionWrapper)
        {
            ViewModel.GoBackFromSubGraph(Editor.SelectedNode);
        }

        if (Editor.SelectedNode is scnStartNodeWrapper or scnEndNodeWrapper)
        {
            ViewModel.GoBackFromSubGraph(Editor.SelectedNode);
        }
    }

    private void BreadcrumbElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { Tag: RedGraph graph } block)
        {
            return;
        }

        ViewModel.JumpToHistoryRedGraph(graph);
    }
}
