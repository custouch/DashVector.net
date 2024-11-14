using System.Net.Http;
using DashSearch;
using DashVector.Enums;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHttpClient();

var dashScopeApikey = Environment.GetEnvironmentVariable("DASH_SCOPE_APIKEY")!;
var dashVectorApiKey = Environment.GetEnvironmentVariable("DASH_VECTOR_APIKEY")!;
var dashVectorEndpoint = Environment.GetEnvironmentVariable("DASH_VECTOR_ENDPOINT")!;

services.AddSingleton(sp => new DashSearchClient(dashScopeApikey, dashVectorApiKey, dashVectorEndpoint, sp.GetRequiredService<IHttpClientFactory>()));


var serviceProvider = services.BuildServiceProvider();
var dashSearchClient = serviceProvider.GetRequiredService<DashSearchClient>();

var collectionName = "dash_search_collection";

await dashSearchClient.CreateCollectionAsync(collectionName, new Dictionary<string, FieldType>
{
    { "title", FieldType.STRING }
});

while (await dashSearchClient.CollectionIsReadyAsync(collectionName) == false)
{
    await Task.Delay(TimeSpan.FromSeconds(5));
}

Console.WriteLine("Collection is ready for use.");


Dictionary<string, string> poems = new Dictionary<string, string>
        {
            { "静夜思", "床前明月光，疑是地上霜。\n举头望明月，低头思故乡。" },
            { "登鹳雀楼", "白日依山尽，黄河入海流。\n欲穷千里目，更上一层楼。" },
            { "春晓", "春眠不觉晓，处处闻啼鸟。\n夜来风雨声，花落知多少。" },
            { "望庐山瀑布", "日照香炉生紫烟，遥看瀑布挂前川。\n飞流直下三千尺，疑是银河落九天。" },
            { "早发白帝城", "朝辞白帝彩云间，千里江陵一日还。\n两岸猿声啼不住，轻舟已过万重山。" },
            { "送别", "长亭外，古道边，芳草碧连天。\n晚风拂柳笛声残，夕阳山外山。" },
            { "黄鹤楼", "黄鹤楼中吹玉笛，江城五月落梅花。\n岁月不待人，黄鹤楼空留古人迹。" },
            { "将进酒", "君不见，黄河之水天上来，奔流到海不复回。\n君不见，高堂明镜悲白发，朝如青丝暮成雪。" },
            { "清平调", "云想衣裳花想容，春风拂槛露华浓。\n若非群玉山头见，会向瑶台月下逢。" }
        };

foreach (var poem in poems)
{
    await dashSearchClient.AddRecordAsync(collectionName, new DashSearchRecord()
    {
        RecordId = poems.Keys.ToList().IndexOf(poem.Key).ToString(),
        Content = poem.Value,
        Fields = new()
         {
             { "title", poem.Key }
         }
    });
}


Console.WriteLine("Records added to collection.");


var searchResults = await dashSearchClient.SearchAsync(collectionName, "床前明月光");


foreach (var result in searchResults)
{
    Console.WriteLine($"Score: {result.Score}");
    Console.WriteLine($"Title: {result.Fields["title"]}");
    Console.WriteLine($"Content: {result.Content}");
    Console.WriteLine();
}