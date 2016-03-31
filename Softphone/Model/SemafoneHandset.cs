using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Softphone.Model
{
    #region Enter Secure Mode Handeset
    public class SemafoneEnterSecureModeHandset
    {
        [JsonProperty(PropertyName = "semafone")]
        public SemafoneEnterSecureMode Semafone { get; set; }
    }
    public class SemafoneEnterSecureMode
    {
       [JsonProperty(PropertyName = "entered-secure-mode")]
       public bool Enteredsecuremode{get;set;}
        [JsonProperty(PropertyName = "genesysId")]
       public string GenesysId { get; set; }
        [JsonProperty(PropertyName = "semafoneUrl")]
       public string SemafoneUrl { get; set; }
        [JsonProperty(PropertyName = "semafoneCr")]
       public string SemafoneCr { get; set; }
    }
    #endregion
    
    #region Exit Secure Mode Handset
    public class SemafoneExitSecureModeHandset
    {
        [JsonProperty(PropertyName = "semafone")]
        public SemafoneExitSecureMode Semafone { get; set; }
    }
    public class SemafoneExitSecureMode
    {
        [JsonProperty(PropertyName = "exit-secure-mode")]
        public bool Exitsecuremode { get; set; }
    }
    #endregion
    
    #region Reset PAN Handset
    public class SemafoneResetPanHandset
    {
        [JsonProperty(PropertyName = "semafone")]
        public SemafoneResetPan Semafone { get; set; }
    }
    public class SemafoneResetPan
    {
        [JsonProperty(PropertyName = "pan-reset")]
        public bool Panreset { get; set; }
    }
    #endregion

    #region Reset CVC Handset
    public class SemafoneResetCvcHandset
    {
        [JsonProperty(PropertyName = "semafone")]
        public SemafoneResetCvc Semafone { get; set; }
    }
    public class SemafoneResetCvc
    {
        [JsonProperty(PropertyName = "cvc-reset")]
        public bool Cvcreset { get; set; }
    }
    #endregion
    #region DTMF Handset
    public class SemafoneDtmf
    {
        [JsonProperty(PropertyName = "semafone")]
        public SemafonePciElement Semafone { get; set; }
    }
    public class SemafonePciElement
    {
        [JsonProperty(PropertyName = "dtmf")]
        public PciElement Dtmf { get; set; }
    }
    #endregion
    #region PCIElement
    public class PciElement
    {
        string _state = "";
        [JsonProperty(PropertyName = "state")]
        public string State { get { return _state; } set { _state = value; } }

        string _validationState = "";
        [JsonProperty(PropertyName = "validationState")]
        public string ValidationState { get { return _validationState; } set { _validationState = value; } }

        string _enabled = "";
        [JsonProperty(PropertyName = "enabled")]
        public string Enabled { get { return _enabled; } set { _enabled = value; } }

        string _name = "";
        [JsonProperty(PropertyName = "name")]
        public string Name { get { return _name; } set { _name = value; } }

        string _data = "";
        [JsonProperty(PropertyName = "data")]
        public string Data { get { return _data; } set { _data = value; } }

        string _length = "";
        [JsonProperty(PropertyName = "length")]
        public string Length { get { return _length; } set { _length = value; } }

        string _sizemin = "";
        [JsonProperty(PropertyName = "sizemin")]
        public string Sizemin { get { return _sizemin; } set { _sizemin = value; } }

        string _sizemax = "";
        [JsonProperty(PropertyName = "sizemax")]
        public string Sizemax { get { return _sizemax; } set { _sizemax = value; } }
    }
    #endregion
}
