using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Linq;
using ZeroTouch.UI.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;

namespace ZeroTouch.UI.Views
{
    public partial class MainDashboardView : UserControl
    {
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

            map.Layers.Add(OpenStreetMap.CreateTileLayer());

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
    }
}
