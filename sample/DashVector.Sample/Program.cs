using DashVector;
using DashVector.Models;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;

const string demoCollection = nameof(demoCollection);

// get console cancellation token

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// ready configuration

var configuration = new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .AddUserSecrets<Program>()
                        .Build();

var apikey = configuration["DashVector:ApiKey"];
var endpoint = configuration["DashVector:Endpoint"];

ArgumentNullException.ThrowIfNull(apikey, nameof(apikey));
ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));


var openaiApiKey = configuration["OpenAI:ApiKey"];
var openaiEndpoint = configuration["OpenAI:Endpoint"];
var openaiModel = configuration["OpenAI:Model"];

ArgumentNullException.ThrowIfNull(openaiApiKey, nameof(openaiApiKey));
ArgumentNullException.ThrowIfNull(openaiEndpoint, nameof(openaiEndpoint));

// create dashvector client
var dashVector = new DashVectorClient(apikey, endpoint);

// ensure collection exists

try
{
    var collection = await dashVector.DescribeCollectionAsync(demoCollection, cts.Token);
}
catch (DashVectorException ex) when (ex.Message.Contains("Not found collection"))
{
    await dashVector.CreateCollectionAsync(new DashVector.Models.Requests.CreateCollectionRequest()
    {
        Name = demoCollection,
        DataType = DashVector.Enums.DataType.FLOAT,
        Dimension = 1536,
        Metric = CollectionInfo.Metric.Cosine,
        FieldsSchema = new()
        {
            ["file"] = DashVector.Enums.FieldType.STRING
        }
    }, cts.Token);

    // wait for collection to be ready
    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
}

// create embedding

EmbeddingClient embeddingClient = new(openaiModel, new ApiKeyCredential(openaiApiKey), new OpenAIClientOptions()
{
    Endpoint = new Uri(openaiEndpoint)
});

const string demoText = "This is a demo text for embedding";

var embeddings = await embeddingClient.GenerateEmbeddingsAsync([demoText], cancellationToken: cts.Token);

var embedding = embeddings.Value.First().ToFloats();

// insert document

var result = await dashVector.InsertDocAsync(new DashVector.Models.Requests.InsertDocRequest()
{
    Docs =
    [
         new Doc()
         {
              Vector = embedding.ToArray(),
              Fields = new()
              {
                  ["file"] = "demo.txt"
              }
         }
    ]
}, demoCollection, cts.Token);


var docId = result.OutPut.First().Id;

// query by id
var queryDocById = await dashVector.QueryDocAsync(new DashVector.Models.Requests.QueryDocRequest()
{
    Id = docId
}, demoCollection, cts.Token);

// query by vector

var queryDocByVector = await dashVector.QueryDocAsync(new DashVector.Models.Requests.QueryDocRequest()
{
    Vector = embedding.ToArray()
}, demoCollection, cts.Token);

// query by filter 

var queryDocByFilter = await dashVector.QueryDocAsync(new DashVector.Models.Requests.QueryDocRequest()
{
    Filter = "file='demo.txt'"
}, demoCollection, cts.Token);

// query by group 
var queryByGroup = await dashVector.QueryGroupByAsync(new DashVector.Models.Requests.QueryGroupByRequest()
{
    GroupByField = "file",
    Id = docId
}, demoCollection, cts.Token);

// fetch document
var fetchDoc = await dashVector.FetchDocAsync(new DashVector.Models.Requests.FetchDocRequest()
{
    Ids = [docId]
}, demoCollection, cts.Token);

// delete document
var deleteDoc = await dashVector.DeleteDocAsync(new DashVector.Models.Requests.DeleteDocRequest()
{
    Ids = [docId]
}, demoCollection, cts.Token);

// delete collection
await dashVector.DeleteCollectionAsync(demoCollection, cts.Token);

