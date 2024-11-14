using DashVector;
using DashVector.Enums;
using DashVector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DashSearch
{
    /// <summary>
    /// Client for interacting with DashSearch services.
    /// </summary>
    public class DashSearchClient
    {
        private DashVectorClient _vectorStore;
        private HttpClient _dashScopeClient;
        const string content_field = "__content__";

        /// <summary>
        /// Initializes a new instance of the <see cref="DashSearchClient"/> class.
        /// </summary>
        /// <param name="dashscope_apikey">API key for DashScope.</param>
        /// <param name="dashvector_apikey">API key for DashVector.</param>
        /// <param name="dashvector_endpoint">Endpoint for DashVector.</param>
        /// <param name="clientFactory">HTTP client factory.</param>
        public DashSearchClient(string dashscope_apikey, string dashvector_apikey, string dashvector_endpoint, IHttpClientFactory clientFactory)
        {
            this._vectorStore = new DashVectorClient(dashvector_apikey, dashvector_endpoint, clientFactory.CreateClient());
            this._dashScopeClient = clientFactory.CreateClient();
            this._dashScopeClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dashscope_apikey);
        }

        /// <summary>
        /// Creates a new collection in DashVector.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="fieldsScheme">Schema of the fields in the collection.</param>
        public async Task CreateCollectionAsync(string collectionName, Dictionary<string, FieldType> fieldsScheme)
        {
            await _vectorStore.CreateCollectionAsync(new DashVector.Models.Requests.CreateCollectionRequest()
            {
                DataType = DataType.FLOAT,
                Dimension = 1024,
                FieldsSchema = fieldsScheme,
                Metric = DashVector.Models.CollectionInfo.Metric.Dotproduct,
                Name = collectionName
            });
        }

        /// <summary>
        /// Checks if a collection is ready for use.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>True if the collection is ready, otherwise false.</returns>
        public async Task<bool> CollectionIsReadyAsync(string collectionName)
        {
            try
            {
                var status = await _vectorStore.DescribeCollectionAsync(collectionName);
                return status.OutPut?.Status == CollectionStatus.SERVING;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new partition in a collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="partion">Name of the partition.</param>
        public async Task CreatePartionAsync(string collectionName, string partion)
        {
            await _vectorStore.CreatePartitionAsync(new DashVector.Models.Requests.CreatePartitionRequest()
            {
                name = partion
            }, collectionName);
        }

        /// <summary>
        /// Adds a new record to a collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="record">Record to add.</param>
        /// <param name="partion">Optional partition name.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task AddRecordAsync(string collectionName, DashSearchRecord record, string? partion = null, CancellationToken token = default)
        {
            var (embeddings, sparse_embedding) = await GenerateEmbeddingAsync(record.Content);

            var fields = record.Fields.ToDictionary(x => x.Key, x => new FieldValue(x.Value));

            fields.Add(content_field, new FieldValue(record.Content));

            await _vectorStore.UpsertDocAsync(new DashVector.Models.Requests.UpsertDocRequest()
            {
                Docs = [
                         new DashVector.Models.Doc()
                             {
                                 Id = record.RecordId,
                                 Vector = embeddings,
                                 SparseVector = sparse_embedding,
                                 Fields = fields
                             }
                     ],
                Partition = partion
            }, collectionName, token);
        }

        /// <summary>
        /// Searches for records in a collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="query">Search query.</param>
        /// <param name="topK">Number of top results to return.</param>
        /// <param name="partion">Optional partition name.</param>
        /// <param name="tagFilters">Optional tag filters.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of matching records.</returns>
        public async Task<List<DashSearchRecord>> SearchAsync(string collectionName, string query, int topK = 5, string? partion = null, Dictionary<string, string>? tagFilters = null, CancellationToken token = default)
        {
            var (embeddings, sparse_embedding) = await GenerateEmbeddingAsync(query);

            var filter = tagFilters == null ? null : string.Join("and", tagFilters.Select(x => $" {x.Key}:{x.Value} "));

            var results = await _vectorStore.QueryDocAsync(new DashVector.Models.Requests.QueryDocRequest()
            {
                Vector = embeddings,
                SparseVector = sparse_embedding,
                TopK = topK,
                Partition = partion,
                Filter = filter
            }, collectionName, token);

            return results.OutPut.Select(x => new DashSearchRecord()
            {
                Score = x.Score,
                RecordId = x.Id,
                Content = x.Fields[content_field].GetValue<string>(),
                Fields = x.Fields.Where(x => x.Key != content_field).ToDictionary(x => x.Key, x => x.Value.RawValue)
            }).ToList();
        }

        /// <summary>
        /// Generates embeddings for the given content.
        /// </summary>
        /// <param name="content">Content to generate embeddings for.</param>
        /// <returns>Tuple containing dense and sparse embeddings.</returns>
        private async Task<(float[] embeddings, SortedDictionary<int, float> sparse_embedding)> GenerateEmbeddingAsync(string content)
        {
            const string url = "https://dashscope.aliyuncs.com/api/v1/services/embeddings/text-embedding/text-embedding";

            var result = await _dashScopeClient.PostAsJsonAsync(url, new
            {
                model = "text-embedding-v3",
                input = new
                {
                    texts = new string[] { content }
                },
                parameters = new
                {
                    output_type = "dense&sparse"
                }
            });

            var response = await result.Content.ReadFromJsonAsync<JsonObject>();

            var embeddings = response["output"]["embeddings"][0]["embedding"].AsArray().Select(x => float.Parse(x.ToString())).ToArray();
            var sparse_embedding = response["output"]["embeddings"][0]["sparse_embedding"].AsArray().Select(x => new KeyValuePair<int, float>(int.Parse(x["index"].ToString()), float.Parse(x["value"].ToString()))).ToDictionary(x => x.Key, x => x.Value);

            return (embeddings, new SortedDictionary<int, float>(sparse_embedding));
        }
    }

    /// <summary>
    /// Represents a record in DashSearch.
    /// </summary>
    public class DashSearchRecord
    {
        /// <summary>
        /// Gets or sets the score of the record.
        /// </summary>
        public float? Score { get; set; }
        /// <summary>
        /// Gets or sets the record ID.
        /// </summary>
        public string RecordId { get; set; }
        /// <summary>
        /// Gets or sets the content of the record.
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Gets or sets the fields of the record.
        /// </summary>
        public Dictionary<string, object> Fields { get; set; }
    }
}
