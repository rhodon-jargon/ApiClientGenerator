using System;
using System.Collections.Generic;

namespace GoLive.Generator.ApiClientGenerator
{
    public class RouteGeneratorSettings
    {
        public string OutputFile { get; set; }
        public List<string> OutputFiles { get; set; }
        public List<string> Includes { get; set; }

        public string CustomDiscriminator { get; set; }
        public string Namespace { get; set; }

        public List<string> PreAppendLines { get; set; }
        public List<string> PostAppendLines { get; set; }
        
        public List<string> HideUrlsRegex { get; set; }

        public string PrefixUrl { get; set; }

        public bool UseResponseWrapper { get; set; }
        public bool IncludeResponseWrapper { get; set; }
        public string ResponseWrapperType { get; set; }

        public string QueryStringBuilder { get; set; }
        public string QueryStringDictionary { get; set; }
        public string QueryStringFormat { get; set; }
    }
}