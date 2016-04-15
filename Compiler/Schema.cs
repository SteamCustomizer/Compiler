using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;

using FreeImageAPI;

namespace Compiler.Schema
{
    public class SkinFile
    {
        // Models

        public class Metadata
        {
            public class TemplateMetadata
            {
                [JsonRequired]
                public string name { get; set; }

                [JsonRequired]
                public int version { get; set; }

                [JsonRequired]
                public string skinBase { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string author { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string authorURL { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string description { get; set; }
            }

            [JsonRequired]
            public TemplateMetadata template { get; set; }

            public class SkinMetadata
            {
                [JsonProperty(Required = Required.DisallowNull)]
                public string name { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public int revision { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string skinURL { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string author { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string authorURL { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string description { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string primaryColor { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string primaryTextColor { get; set; }
                
                [JsonProperty(Required = Required.DisallowNull)]
                public string accentColor { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string accentTextColor { get; set; }
                
                [JsonProperty(Required = Required.DisallowNull)]
                public string id { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string thumbnail { get; set; }
            }

            [JsonRequired]
            public SkinMetadata skin { get; set; }
        }

        public class File
        {
            [JsonProperty(Required = Required.DisallowNull)]
            public JArray remove { get; set; }
            [JsonProperty(Required = Required.DisallowNull)]
            public JObject add { get; set; }
            [JsonProperty(Required = Required.DisallowNull)]
            public JObject change { get; set; }
        }

        public class Attachment
        {
            /// <summary>
            /// Transform
            /// </summary>
            public class Transform
            {
                [JsonProperty(Required = Required.Always)]
                public double angle { get; set; }
                [JsonProperty(Required = Required.Always)]
                public double x { get; set; }
                [JsonProperty(Required = Required.Always)]
                public double y { get; set; }
                [JsonProperty(Required = Required.Always)]
                public double scaleX { get; set; }
                [JsonProperty(Required = Required.Always)]
                public double scaleY { get; set; }
                [JsonProperty(Required = Required.DisallowNull)]
                public string scaleFilter { get; set; }
            }

            /// <summary>
            /// Filters
            /// </summary>
            public class Filter
            {
                [JsonProperty(Required = Required.Always)]
                public string name { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public int amount { get; set; }

                [JsonProperty(Required = Required.DisallowNull)]
                public string color { get; set; }
            }
            /// <summary>
            /// Attachment properties
            /// </summary>
            public string path { get; set; }
            public string data { get; set; }
            [JsonProperty(Required = Required.DisallowNull)]
            public string type { get; set; }

            [JsonProperty(Required = Required.DisallowNull)]
            public Transform transform { get; set; }

            [JsonProperty(Required = Required.DisallowNull)]
            public List<Filter> filters { get; set; }

            [JsonProperty(Required = Required.DisallowNull)]
            public Dictionary<int, int[]> spritesheet { get; set; }

            [JsonProperty(Required = Required.DisallowNull)]
            public Dictionary<int, string> spritesheetFiles { get; set; }
        }

        /// <summary>
        /// Skin file properties
        /// </summary>
        [JsonRequired]
        public Metadata metadata { get; set; }

        [JsonRequired]
        public Dictionary<string, File> files { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public List<Attachment> attachments { get; set; }
    }
}
