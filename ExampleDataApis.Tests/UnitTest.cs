using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Testing;
using ExampleDataApis.ServiceInterface;
using ExampleDataApis.ServiceModel;
using ServiceStack.Text;

namespace ExampleDataApis.Tests;

public class UnitTest
{
    private readonly ServiceStackHost appHost;

    public UnitTest()
    {
        appHost = new BasicAppHost().Init();
        appHost.Container.AddTransient<MyServices>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    [Test]
    public void Can_call_MyServices()
    {
        var service = appHost.Container.Resolve<MyServices>();

        var response = (HelloResponse)service.Any(new Hello { Name = "World" });

        Assert.That(response.Result, Is.EqualTo("Hello, World!"));
    }

    [Test, Explicit]
    public void Extract_Xkcd_Image_Dimensions()
    {
        var allLines = "xkcd-metadata.jsonl"
            .ReadAllText().Split("\n");
        using var jsonConfig = JsConfig.With(new Config
        {
            TextCase = TextCase.SnakeCase
        });
        
        var comics = allLines
            .Where(x => !x.IsNullOrEmpty())
            .Select(JsonSerializer.DeserializeFromString<XkcdComic>)
            .ToList();

        var results = new List<XkcdComicDimensions>();
        foreach (var comic in comics)
        {
            try
            {
                var image = comic.ImageUrl.GetBytesFromUrl();
                using var imageStream = new MemoryStream(image);
                var pngImage = Image.FromStream(imageStream);
                // Get the dimensions of the PNG image
                int width = pngImage.Width;
                int height = pngImage.Height;
                results.Add(new XkcdComicDimensions
                {
                    Id = comic.Id,
                    Width = width,
                    Height = height
                });
                // Dispose the image to release resources
                pngImage.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                results.Add(new XkcdComicDimensions
                {
                    Id = comic.Id,
                    Width = 0,
                    Height = 0
                });
            }
        }

        File.WriteAllText("xkcd-dimensions.json", JsonSerializer.SerializeToString(results));
    }
}

public class XkcdComicDimensions
{
    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}