using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Layered;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.GraphEditor;

public partial class RedGraphLayoutManager : ObservableObject
{
    private readonly RedGraph _redGraph;
    private bool _allowGraphSave;

    [ObservableProperty] private System.Windows.Point _viewportLocation;
    [ObservableProperty] private System.Windows.Size _viewportSize;
    [ObservableProperty] private double _viewportZoom;
    [ObservableProperty] private bool _isLayoutLoaded;

    public RedGraphLayoutManager(RedGraph graph)
    {
        _redGraph = graph;
    }

    public void SaveGraphLayout()
    {
        if (_redGraph.DocumentViewModel != null && _allowGraphSave)
        {
            var proj = _redGraph.DocumentViewModel.GetActiveProject();
            if (proj != null)
            {
                var statePath = Path.Combine(proj.ProjectDirectory, "GraphEditorStates", _redGraph.DocumentViewModel!.RelativePath /*+ StateParents*/ + ".json");
                var parentFolder = Path.GetDirectoryName(statePath);

                if (parentFolder != null && !Directory.Exists(parentFolder))
                {
                    Directory.CreateDirectory(parentFolder);
                }

                if (File.Exists(statePath))
                {
                    File.Delete(statePath);
                }

                var jNodes = new JArray();
                foreach (var node in _redGraph.Nodes)
                {
                    uint nodeID = _redGraph.GetNodeId(node);

                    JObject newPerfSet = new(
                        new JProperty("NodeID", nodeID),
                        new JProperty("X", node.Location.X),
                        new JProperty("Y", node.Location.Y)
                    );

                    jNodes.Add(newPerfSet);
                }

                var jRoot = new JObject
                {
                    new JProperty("EditorX", ViewportLocation.X),
                    new JProperty("EditorY", ViewportLocation.Y),
                    new JProperty("EditorZ", ViewportZoom),
                    new JProperty("Nodes", jNodes)
                };

                File.WriteAllText(statePath, JsonConvert.SerializeObject(jRoot));
            }
        }
    }

    public void LoadGraphLayout()
    {
        if (IsLayoutLoaded)
        {
            return;
        }

        var loaded = false;

        var proj = _redGraph.DocumentViewModel?.GetActiveProject();
        if (proj != null)
        {
            var statePath = Path.Combine(proj.ProjectDirectory, "GraphEditorStates", _redGraph.DocumentViewModel!.RelativePath /*+ StateParents*/ + ".json");
            if (File.Exists(statePath))
            {
                Dictionary<uint, System.Windows.Point> nodesLocs = new();

                var jsonData = JObject.Parse(File.ReadAllText(statePath));
                var nodesArray = jsonData.SelectTokens("Nodes.[*]");
                foreach (var node in nodesArray)
                {
                    var nodeID = node.SelectToken("NodeID") as JValue;
                    var nodeX = node.SelectToken("X") as JValue;
                    var nodeY = node.SelectToken("Y") as JValue;

                    if (nodeID != null && nodeX != null && nodeY != null)
                    {
                        nodesLocs.TryAdd(
                            nodeID.ToObject<uint>(),
                            new System.Windows.Point(
                                nodeX.ToObject<double>(),
                                nodeY.ToObject<double>()
                            )
                        );
                    }
                }

                foreach (var node in _redGraph.Nodes)
                {
                    uint nodeID = _redGraph.GetNodeId(node);
                    
                    if (nodesLocs.ContainsKey(nodeID))
                    {
                        node.Location = nodesLocs[nodeID];
                    }
                }

                var editorX = jsonData.SelectToken("EditorX") as JValue;
                var editorY = jsonData.SelectToken("EditorY") as JValue;
                var editorZ = jsonData.SelectToken("EditorZ") as JValue;
                if (editorX != null && editorY != null && editorZ != null)
                {
                    ViewportZoom = editorZ.ToObject<double>();
                    ViewportLocation = new System.Windows.Point(editorX.ToObject<double>(), editorY.ToObject<double>());
                }

                loaded = true;
            }
        }

        if (!loaded)
        {
            var rect = ArrangeNodes();
            FitNodesToScreen(rect);
        }

        IsLayoutLoaded = true;
        _allowGraphSave = true;
    }

    public System.Windows.Rect ArrangeNodes(double xOffset = 0, double yOffset = 0)
    {
        var graph = new GeometryGraph();
        var msaglNodes = new Dictionary<uint, Node>();

        foreach (var node in _redGraph.Nodes)
        {
            var msaglNode = new Node(CurveFactory.CreateRectangle(node.Size.Width, node.Size.Height, new Microsoft.Msagl.Core.Geometry.Point()))
            {
                UserData = node
            };

            msaglNodes.Add(node.UniqueId, msaglNode);
            graph.Nodes.Add(msaglNode);
        }

        foreach (var connection in _redGraph.Connections.Reverse())
        {
            graph.Edges.Add(new Edge(msaglNodes[connection.Source.OwnerId], msaglNodes[connection.Target.OwnerId]));
        }

        var settings = new SugiyamaLayoutSettings
        {
            Transformation = PlaneTransformation.Rotation(Math.PI / 2),
            EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.Spline }
        };

        var layout = new LayeredLayout(graph, settings);
        layout.Run();

        double maxX = 0;
        double minX = 0;
        double maxY = 0;
        double minY = 0;

        foreach (var node in graph.Nodes)
        {
            var nvm = (NodeViewModel)node.UserData;
            nvm.Location = new System.Windows.Point(
                node.Center.X - graph.BoundingBox.Center.X - (nvm.Size.Width / 2) + xOffset,
                node.Center.Y - graph.BoundingBox.Center.Y - (nvm.Size.Height / 2) + yOffset);

            maxX = Math.Max(maxX, nvm.Location.X + nvm.Size.Width);
            minX = Math.Min(minX, nvm.Location.X);
            maxY = Math.Max(maxY, nvm.Location.Y + nvm.Size.Height);
            minY = Math.Min(minY, nvm.Location.Y);
        }

        return new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public void CenterOnNode(object node)
    {
        if (node is not NodeViewModel nvm)
        {
            return;
        }

        ViewportZoom = 1;
        ViewportLocation = new System.Windows.Point(
            nvm.Location.X - (ViewportSize.Width / 2) + (nvm.Size.Width / 2),
            nvm.Location.Y - (ViewportSize.Height / 2) + (nvm.Size.Height / 2));
    }

    public void FitNodesToScreen(System.Windows.Rect rect)
    {
        if (rect.Width > 0 && rect.Height > 0)
        {
            ViewportZoom = Math.Min(ViewportSize.Width / rect.Width, ViewportSize.Height / rect.Height);
            var centerPoint = new System.Windows.Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
            ViewportLocation = new System.Windows.Point(centerPoint.X - ViewportSize.Width / 2, centerPoint.Y - ViewportSize.Height / 2);
        }
    }
}
