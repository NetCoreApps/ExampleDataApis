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
        
        var existingDimensions = "xkcd-dimensions.json"
            .ReadAllText().FromJson<List<XkcdComicDimensions>>();

        var updatedDimensions = "xkcd-dimensions_updated.json"
            .ReadAllText().FromJson<List<XkcdComicDimensions>>();
        
        for (var index = 0; index < existingDimensions.Count; index++)
        {
            var existingDimension = existingDimensions[index];
            if(existingDimension?.Width != 0)
                continue;
            var comic = comics.FirstOrDefault(x => x.Id == existingDimension.Id);
            if (comic == null || comic.ImageUrl.IsNullOrEmpty())
            {
                continue;
            }
            
            var image = comic.ImageUrl.GetBytesFromUrl();
            using var imageStream = new MemoryStream(image);
            var pngImage = Image.FromStream(imageStream);

            if (pngImage.Width == 0)
                throw new Exception("Width 0");
            existingDimension.Width = pngImage.Width;
            existingDimension.Height = pngImage.Height;
            File.WriteAllText("xkcd-dimensions_updated.json", JsonSerializer.SerializeToString(existingDimensions));
        }

        File.WriteAllText("xkcd-dimensions_updated.json", JsonSerializer.SerializeToString(existingDimensions));
    }
}

public class XkcdComicDimensions
{
    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}