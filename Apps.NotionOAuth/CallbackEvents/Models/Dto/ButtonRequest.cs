using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.NotionOAuth.CallbackEvents.Models.Dto;
public class ButtonRequest
{
    [JsonProperty("data")]
    public Data Data { get; set; }
}

public class Data
{
    [JsonProperty("id")]
    public string Id { get; set; }
}