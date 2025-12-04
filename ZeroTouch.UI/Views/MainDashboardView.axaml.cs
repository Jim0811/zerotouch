using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using BruTile.Web;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroTouch.UI.ViewModels;

namespace ZeroTouch.UI.Views
{
    public partial class MainDashboardView : UserControl
    {
        private DispatcherTimer? _navigationTimer;

        private List<MPoint> _routePath = new List<MPoint>();

        private int _currentStepIndex = 0;

        private MemoryLayer? _vehicleLayer;
        private MapControl? _mapControl;

        public MainDashboardView()
        {
            InitializeComponent();

            InitializeMap();
        }

        private void MusicSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (DataContext is MainDashboardViewModel vm)
            {
                vm.SeekCommand.Execute((long)e.NewValue);
            }
        }

        private void InitializeMap()
        {
            var mapControl = this.FindControl<MapControl>("MapControl");
            if (mapControl == null) return;

            var map = new Map();

            var urlFormatter = new HttpTileSource(
                new BruTile.Predefined.GlobalSphericalMercator(),
                "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png",
                new[] { "a", "b", "c", "d" },
                name: "CartoDB Voyager"
            );

            map.Layers.Add(new TileLayer(urlFormatter));

            var lonLats = new[]
            {
                // start point
                (120.28471712200883, 22.73226013221393),

                (120.29053110655967, 22.73249458710715),
                (120.29239839952325, 22.732232361349443),
                (120.29172547199514, 22.727218383170385),

                (120.29532144100894, 22.72667258631913),
                (120.29591697595505, 22.72637869480983),
                (120.29629763948877, 22.72595883940386),

                (120.29661829404742, 22.725467958231206),

                // destination
                (120.29775246573561, 22.723400189901515)
            };

            foreach (var (lon, lat) in lonLats)
            {
                var p = SphericalMercator.FromLonLat(lon, lat);
                _routePath.Add(new MPoint(p.x, p.y));
            }

            var routeLayer = CreateRouteLayer(_routePath);
            map.Layers.Add(routeLayer);

            _vehicleLayer = CreateVehicleLayer(_routePath[0]);
            map.Layers.Add(_vehicleLayer);

            // Remove default widgets
            while (map.Widgets.TryDequeue(out _)) { }

            mapControl.Map = map;

            // Ensure the map is loaded before performing operations
            mapControl.Loaded += (s, e) =>
            {
                // Get coordinates
                var coords = SphericalMercator.FromLonLat(120.2845411717568, 22.73356348850075);

                var point = new MPoint(coords.x, coords.y);

                // Navigate to the point
                map.Navigator.CenterOn(point);
                map.Navigator.ZoomTo(2.0);
            };
        }

        private MemoryLayer CreateRouteLayer(List<MPoint> pathPoints)
        {
            var coordinates = new Coordinate[pathPoints.Count];
            for (int i = 0; i < pathPoints.Count; i++)
            {
                coordinates[i] = new Coordinate(pathPoints[i].X, pathPoints[i].Y);
            }

            var lineString = new LineString(coordinates);

            var feature = new GeometryFeature
            {
                Geometry = lineString
            };

            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Color.FromArgb(200, 33, 150, 243), 6) // Blue
            });

            return new MemoryLayer
            {
                Name = "RouteLayer",
                Features = new[] { feature }
            };
        }

        private MemoryLayer CreateVehicleLayer(MPoint startPoint)
        {
            var pointFeature = new GeometryFeature
            {
                Geometry = new NetTopologySuite.Geometries.Point(startPoint.X, startPoint.Y)
            };

            pointFeature.Styles.Add(new SymbolStyle
            {
                Fill = new Brush(Color.Red),
                Outline = new Pen(Color.White, 2),
                SymbolScale = 0.5f,
                SymbolType = SymbolType.Ellipse
            });

            return new MemoryLayer
            {
                Name = "VehicleLayer",
                Features = new[] { pointFeature }
            };
        }

        private void StartNavigationSimulation()
        {
            if (_mapControl?.Map?.Navigator == null) return;

            _mapControl.Map.Navigator.CenterOn(_routePath[0]);
            _mapControl.Map.Navigator.ZoomTo(1.0);

            _navigationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _navigationTimer.Tick += (s, e) =>
            {
                _currentStepIndex++;

                if (_currentStepIndex >= _routePath.Count)
                {
                    _currentStepIndex = 0;
                }

                var newLocation = _routePath[_currentStepIndex];

                if (_vehicleLayer != null)
                {
                    var newFeature = new GeometryFeature
                    {
                        Geometry = new NetTopologySuite.Geometries.Point(newLocation.X, newLocation.Y)
                    };

                    newFeature.Styles.Add(_vehicleLayer.Features.First().Styles.First());

                    _vehicleLayer.Features = new[] { newFeature };

                    _vehicleLayer.DataHasChanged();
                }

                _mapControl.Map.Navigator.CenterOn(newLocation);

                _mapControl.RefreshGraphics();
            };

            _navigationTimer.Start();
        }
    }
}
