using System.Reflection;

using Glue;
using Glue.Services;

namespace SearchServiceNS
{
    public class SearchService
    {
        public static string SEARCH_PROVIDER_METHOD = "T42.Search.Provider";
        public static string SEARCH_RESULTS_METHOD = "T42.Search.Client";

        public static string OPERATION_SEARCH = "search";
        public static string OPERATION_CANCEL = "cancel";

        public static string STATUS_DONE = "done";
        public static string STATUS_IN_PROGRESS = "in-progress";


        private Glue42 glue_;

        private static IAGMObjectSerializer ser_ = new AGMObjectSerializer(new ObjectSerializerSettings()
        {
            AlwaysForceLowerCamelCaseMemberName = false,
            BindingFlags = BindingFlags.Instance | BindingFlags.Public,
            Options = SerializationOptions.SerializeEnumsAsStrings | SerializationOptions.UseCompositeArrayToSerializeLists
        });
        public static IAGMObjectSerializer ser { get => ser_; }

        public static IReadOnlyDictionary<string, object> toDictionary(object obj)
        {
            return ser.Deserialize<Dictionary<string, object>>(ser.Serialize(obj));
        }

        public SearchService(Glue42 glue)
        {
            glue_ = glue;
        }

        public async Task Register()
        {
            IServerMethod interopMethod = await glue_.Interop.RegisterEndpoint(
                mdb => mdb.SetName(SEARCH_PROVIDER_METHOD),
                HandleSearchRequest);
        }

        public void HandleSearchRequest(InvocationContext ictx)
        {
            try
            {
                var param = ser.Deserialize<SearchMethodParam>(ser.Serialize(ictx.Arguments));
                if(string.IsNullOrEmpty(param.operation))
                {
                    throw new Exception($"No operation provided");
                }
                
                if (param.operation.Equals(OPERATION_CANCEL))
                {
                    ictx.ResultBuilder.Succeed();
                    return;
                }
                if (param.operation.Equals(OPERATION_SEARCH))
                {
                    if (param.search is null)
                    {
                        throw new Exception($"No search term provided");
                    }
                    // Generate search id
                    var nowISOString = DateTime.UtcNow.ToString("O");
                    var id = nowISOString + "_" + param.search;
                    var toReturn = new SearchMethodReturn(id);

                    // ignore requests where the search string is too short or too long
                    if (param.search.Length < 4 || param.search.Length > 10)
                    {
                        Task.Run(() => SendSearchResults(id, null, STATUS_DONE, ictx.Caller));
                    } else
                    {
                        Task.Run(() => InitiateSearch(id, param.search, ictx.Caller));
                    }
                    ictx.ResultBuilder.Succeed(toDictionary(toReturn));
                    return;                    
                }
                throw new Exception($"Unsupported operation: '{param.operation}'");
            }
            catch (Exception e)
            {
                ictx.ResultBuilder.SetIsFailed(e.Message);
            }
        }

        public async Task SendSearchResults(string id, List<SearchResultItem>? items, string status, IGlueInstance? caller = null)
        {
            Action<ITargetFilterBuilder>? tfbAction = null;
            if (caller is IGlueInstance)
            {
                tfbAction = (tfb) => tfb.Matching(instance => instance.InstanceId.Equals(caller.InstanceId));
            }

            var invokeParam = new SearchResultParam(id)
            {
                status = status
            };
            if(items is List<SearchResultItem>)
            {
                invokeParam.items = items;
            }
            await glue_.Interop.Invoke(
                SEARCH_RESULTS_METHOD,
                toDictionary(invokeParam),
                tfbAction
            );
        }

        public async Task InitiateSearch(string id, string searchString, IGlueInstance caller)
        {
            // simulate delay from getting results from external system
            await Task.Delay(1000);

            // "transform the results" into search result items
            List<SearchResultItem> items = new List<SearchResultItem>();
            for(int counter = 1; counter <=3; ++counter)
            {
                items.Add(generateSampleResultItem(searchString, counter));
            }

            await SendSearchResults(id, items, STATUS_DONE, caller);
        }

        public SearchResultItem generateSampleResultItem(string searchString, int counter)
        {
            var category = "Cat A";
            if (counter % 2 == 0)
            {
                category = "Cat B";
            }
            var itemId = $"{category}_{counter}_{searchString}";
            var type = $"Type {searchString}";

            var result = new SearchResultItem(itemId, type, category)
            {
                displayName = $"Item {searchString} #{counter}",
                description = $"Description for {searchString} #{counter}",
                action = new SearchResultAction()
                {
                    method = Action01Handler.ACTION_HANDLER_METHOD,
                    parameters = ser.Serialize(new Action01Parameters()
                    {
                        counter = counter,
                        id = itemId
                    })
                }
            };
            return result;
        }
    }
}
