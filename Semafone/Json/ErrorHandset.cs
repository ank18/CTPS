using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Semafone.Json
{
    public enum ErrorSeverity { Info = 1, Warnning = 2, Error = 3 }
    /// <summary>
    /// Json Error response send back to softphone client
    /// </summary>
    class ErrorResponse
    {
        [JsonProperty(PropertyName = "error")]
        public ErrorObject Error { get; set; }
        public ErrorResponse(ErrorObject errorObject)
        {
            Error = errorObject;
        }
    }
    class ErrorObject
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }
        [JsonProperty(PropertyName = "datetime")]
        public string Datetime { get; set; }
        [JsonProperty(PropertyName = "error_severity")]
        public string ErrorSeverity { get; set; }
        [JsonProperty(PropertyName = "error_label")]
        public string ErrorLabel { get; set; }
        [JsonProperty(PropertyName = "error_code")]
        public int ErrorCode { get; set; }
        [JsonProperty(PropertyName = "error_message")]
        public string ErrorMessage { get; set; }
    }
}
