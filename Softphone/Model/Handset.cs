using Genesyslab.Platform.Commons.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Softphone.Model
{
    /// <summary>
    /// class used for Json response
    /// </summary>    
    
    public class Handset 
    {
        [JsonProperty(PropertyName = "status")]
        public Status Status { get; set; }
        [JsonProperty(PropertyName = "subscribedToConcierge")]
        public string SubscribedToConcierge { get; set; }
        [JsonProperty(PropertyName = "concierge")]
        public Dictionary<string, object> Concierge { get; set; }
        [JsonProperty(PropertyName = "interaction")]
        public KeyValueCollection Interaction { get; set; }
        [JsonProperty(PropertyName = "actions")]
        public Actions Actions { get; set; }
        [JsonProperty(PropertyName = "lines")]
        public Lines Lines { get; set; }
    }
  
    public class Status
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName ="id" )]
        public string Id { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "datetime")]
        public string Datetime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "code")]
        public int Code { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "extension")]
        public string Extension { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "agentId")]
        public string Agentid { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "queue")]
        public string Queue { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "reasonCode")]
        public string ReasonCode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "focusedLine")]
        public string FocusedLine { get; set; }
    }
  
    public class Lines
    {

        [JsonProperty(PropertyName = "1")]
        public Line Line1 { get; set; }
        [JsonProperty(PropertyName = "2")]
        public Line Line2 { get; set; }
    }
  
    public class Line
    {
        [JsonProperty(PropertyName = "connectedTo")]
        public string ConnectedTo { get; set; }
        [JsonProperty(PropertyName = "callDirection")]
        public string CallDirection { get; set; }
        [JsonProperty(PropertyName = "lineStatus")]
        public string LineStatus { get; set; }
        [JsonProperty(PropertyName = "actions")]
        public Actions Actions { get; set; }
        public Line()
        {
            this.ConnectedTo = "";
            this.CallDirection = "";
            this.LineStatus = "";
            this.Actions = new Actions(null, null, null, null, null, null, null, null, null);
        }
        public Line(string connectedTo = "", string callDirection = "", string lineStatus = "", Actions actions = null)
        {
            this.ConnectedTo = connectedTo;
            this.CallDirection = callDirection;
            this.LineStatus = lineStatus;
            this.Actions = actions ?? new Actions(null, null,null,null,null,null,null,null,null);
        }
    }

    public class Actions
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "hangUp")]
        public EventButton HangUp { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "dial")]
        public EventButton Dial { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "hold")]
        public EventButton Hold { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "retrieve")]
        public EventButton Retrieve { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "cold")]
        public EventButton Cold { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "warm")]
        public EventButton Warm { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "conference")]
        public EventButton Conference { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "completeWarm")]
        public EventButton CompleteWarm { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "completeConference")]
        public EventButton CompleteConference { get; set; }

        public Actions(EventButton hangup, EventButton dial, EventButton hold, EventButton retrieve, EventButton cold, EventButton warm, EventButton conference, EventButton completeWarm, EventButton completeConference)
        {
            this.HangUp = hangup;
            this.Dial = dial;
            this.Hold = hold;
            this.Retrieve = retrieve;
            this.Cold = cold;
            this.Warm = warm;
            this.Conference = conference;
            this.CompleteWarm = completeWarm;
            this.CompleteConference = completeConference;
        }
    }

    public class EventButton
    {
        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }
        [JsonProperty(PropertyName = "textId")]
        public string TextId { get; set; }
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }
        public EventButton(string action = "", string textId = "", bool enabled = false)
        {
            this.Action = action;
            this.TextId = textId;
            this.Enabled = enabled;
        }
    }
    
}


