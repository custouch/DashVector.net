using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DashVector.Models.Requests
{
    public class QueryGroupByRequest
    {
        /// <summary>
        /// 按指定字段的值来分组检索，目前不支持schema-free字段
        /// </summary>
        [JsonPropertyName("group_by_field")]
        public string GroupByField { get; set; }

        /// <summary>
        /// 最多返回的分组个数，尽力而为参数，一般可以返回group_count个分组。 (0,64] ，默认为1
        /// </summary>
        [JsonPropertyName("group_count")]
        public int? GroupCount { get; set; } = 1;

        /// <summary>
        /// 每个分组返回group_topk条相似性结果，尽力而为参数，优先级低于group_count。(0,16], 默认为1
        /// </summary>
        [JsonPropertyName("group_topk")]
        public int GroupTopK { get; set; } = 1;

        /// <summary>
        /// 向量数据
        /// </summary>
        [JsonPropertyName("vector")]
        public List<float>? Vector { get; set; }

        /// <summary>
        /// 稀疏向量
        /// </summary>
        [JsonPropertyName("sparse_vector")]
        public Dictionary<string, float>? SparseVector { get; set; }

        /// <summary>
        /// 主键，表示根据主键对应的向量进行相似性检索
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 过滤条件，需满足SQL where子句规范，<see href="https://help.aliyun.com/document_detail/2513006.html?spm=a2c4g.2715274.0.0.e9823be5TIh8qH">详见</see>
        /// </summary>
        [JsonPropertyName("filter")]
        public string? Filter { get; set; }

        /// <summary>
        /// 是否返回向量数据，默认false
        /// </summary>
        [JsonPropertyName("include_vector")]
        public bool? IncludeVector { get; set; } = false;

        /// <summary>
        /// 返回field的字段名列表，默认返回所有Fields
        /// </summary>
        [JsonPropertyName("output_fields")]
        public List<string>? OutputFields { get; set; }

        /// <summary>
        /// 使用多向量检索的一个向量执行分组检索。
        /// </summary>
        [JsonPropertyName("vector_field")]
        public string? VectorField { get; set; }

        /// <summary>
        /// Partition名称
        /// </summary>
        [JsonPropertyName("partition")]
        public string? Partition { get; set; }
    }

    public class GroupDocs
    {
        [JsonPropertyName("group_id")]
        public string GroupId { get; set; }

        [JsonPropertyName("docs")]
        public List<Doc> Docs { get; set; }
    }
}
