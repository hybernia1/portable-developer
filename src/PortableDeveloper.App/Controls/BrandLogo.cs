using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using PathShape = System.Windows.Shapes.Path;

namespace PortableDeveloper.App.Controls;

public sealed class BrandLogo : Viewbox
{
    public static readonly DependencyProperty BrandProperty = DependencyProperty.Register(
        nameof(Brand),
        typeof(string),
        typeof(BrandLogo),
        new PropertyMetadata(string.Empty, OnBrandChanged));

    private static readonly IReadOnlyDictionary<string, BrandDefinition> Definitions =
        new Dictionary<string, BrandDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["apache"] = new("apache.svg", "BrandApacheBrush"),
            ["composer"] = new("composer.svg", "BrandComposerBrush"),
            ["firefox"] = new("firefox.svg", "BrandFirefoxBrush"),
            ["googlechrome"] = new("googlechrome.svg", "BrandGoogleChromeBrush"),
            ["mariadb"] = new("mariadb.svg", "BrandMariaDbBrush"),
            ["notepadplusplus"] = new("notepadplusplus.svg", "BrandNotepadPlusPlusBrush"),
            ["nodejs"] = new("nodejs.svg", "BrandNodeJsBrush"),
            ["php"] = new("php.svg", "BrandPhpBrush"),
            ["phpmyadmin"] = new("phpmyadmin.svg", "BrandPhpMyAdminBrush"),
            ["python"] = new("python.svg", "BrandPythonBrush"),
            ["selenium"] = new("selenium.svg", "BrandSeleniumBrush")
        };

    private static readonly ConcurrentDictionary<string, Geometry> GeometryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly PathShape _path = new();

    public BrandLogo()
    {
        Stretch = Stretch.Uniform;
        Child = _path;
    }

    public string Brand
    {
        get => (string)GetValue(BrandProperty);
        set => SetValue(BrandProperty, value);
    }

    private static void OnBrandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((BrandLogo)dependencyObject).ApplyBrand(args.NewValue as string);
    }

    private void ApplyBrand(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand) || !Definitions.TryGetValue(brand, out var definition))
        {
            _path.Data = Geometry.Empty;
            return;
        }

        _path.Data = GeometryCache.GetOrAdd(definition.FileName, LoadGeometry);
        _path.Fill = System.Windows.Application.Current.TryFindResource(definition.BrushResource) as Brush
            ?? throw new InvalidOperationException($"Brand brush {definition.BrushResource} was not found.");
    }

    private static Geometry LoadGeometry(string fileName)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "resources", "logos", fileName);
        var document = XDocument.Load(path);
        var data = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "path")?.Attribute("d")?.Value;

        if (string.IsNullOrWhiteSpace(data))
        {
            throw new InvalidDataException($"Brand logo {fileName} does not contain a path.");
        }

        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }

    private sealed record BrandDefinition(string FileName, string BrushResource);
}
