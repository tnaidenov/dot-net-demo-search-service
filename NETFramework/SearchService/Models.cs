using System.Collections.Generic;

namespace SearchServiceNS
{
    public class SearchMethodParam
    {
        public string operation { get; set; }
        public string search { get; set; }
        public string id { get; set; }
        public int limit { get; set; }
        public int categoryLimit { get; set; }
    }

    public class SearchResultAction
    {
        public string method { get; set; }

        [DOT.AGM.Services.ServiceOperationField(Name = "params")]
        public DOT.AGM.Value parameters { get; set; }
    }
    public class SearchResultItem
    {
        public string id { get; set; }
        public string type { get; set; }
        public string category { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public string iconURL { get; set; }

        public SearchResultAction action;

        public SearchResultItem(string myId, string myType, string myCategory)
        {
            id = myId;
            type = myType;
            category = myCategory;
        }
    }

    public class SearchResultParam
    {
        public string queryId { get; set; }
        public string status { get; set; } = "done";
        public List<SearchResultItem> items { get; set; } = new List<SearchResultItem>();

        public SearchResultParam(string id)
        {
            queryId = id;
        }
    }
}

