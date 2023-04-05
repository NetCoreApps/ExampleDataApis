using ServiceStack;

namespace ExampleDataApis.ServiceModel;

[Route("/xkcd")]
public class QueryXkcdComics : QueryDb<XkcdComic>
{
    public int[] Ids { get; set; }
}

public class XkcdComic
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ImageTitle { get; set; }
    public string Url { get; set; }
    public string ImageUrl { get; set; }
    public string ExplainedUrl { get; set; }
    public string Transcript { get; set; }
    public string Explanation { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
}