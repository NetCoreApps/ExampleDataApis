using System.Data;
using ExampleDataApis.ServiceModel;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ExampleDataApis;

public static class ConfigureDbXkcd
{
    public static void SeedXkcd(this IDbConnection db)
    {
        var allLines = "static_data/xkcd-metadata.jsonl"
            .ReadAllText().Split("\n");
        using var jsonConfig = JsConfig.With(new Config
        {
            TextCase = TextCase.SnakeCase
        });
        var comics = allLines
            .Where(x => !x.IsNullOrEmpty())
            .Select(JsonSerializer.DeserializeFromString<XkcdComic>)
            .ToList();
        
        if(db.CreateTableIfNotExists<XkcdComic>())
            db.InsertAll(comics);
    }
}