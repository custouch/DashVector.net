using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DashVector.Models.Responses
{
    public class DocOpResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
