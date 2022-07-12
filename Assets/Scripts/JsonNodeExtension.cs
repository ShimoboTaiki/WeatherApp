using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Nodes;

public static class JsonNodeExtension
{
    public static Dictionary<string, JsonNode> AsDictionary(this JsonNode node)
    {
        Dictionary<string, JsonNode> dict = new Dictionary<string, JsonNode>();
        foreach (var VARIABLE in node.AsObject())
        {
            dict.Add(VARIABLE.Key,VARIABLE.Value);
        }

        
        
        //return ((JsonObject) node).ToDictionary<string,JsonNode>();
        return dict;
    }
}
