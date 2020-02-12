using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Page
    {
        public string Url { get; set; }
        public string Title { get; set; }

        public List<string> SubURLs { get; set; }
        public override string ToString()
        {
            return $"URL : {Url} , Title : {Title}";
        }
        //public string HTML { get; set; }
        public bool VisitedSiteMap { get; set; } = false;

        public bool HasForm { get; set; } = false;
    }
}
