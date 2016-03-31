
using Newtonsoft.Json;
using System.Collections.Generic;


namespace Softphone.Model
{
    class CmeCode
    {
        string _code = "";
        [JsonProperty(PropertyName = "code")]
        public string Code { get { return _code; } set { _code = value; } }

        string _description = "";
        [JsonProperty(PropertyName = "description")]
        public string Description { get { return _description; } set { _description = value; } }
    }

    class NotReadyCodes
    {
        [JsonProperty(PropertyName = "notreadycodes")]
        public List<CmeCode> Notreadycodes { get; set; }
    }
    class DisconnectCodes
    {
        [JsonProperty(PropertyName = "disconnectcodes")]
        public List<CmeCode> Disconnectcodes { get; set; }
    }
}
