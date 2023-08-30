using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

using Tick42;
using DOT.AGM;
using DOT.AGM.Server;
using DOT.AGM.Services;


namespace SearchServiceNS
{
    public class SearchService
    {
        public static string SEARCH_PROVIDER_METHOD = "T42.Search.Provider";
        public static string SEARCH_RESULTS_METHOD = "T42.Search.Client";

        public static string OPERATION_SEARCH = "search";
        public static string OPERATION_CANCEL = "cancel";

        public static string STATUS_DONE = "done";

        private Glue42 glue_;

        private static IAGMObjectSerializer ser_ = new AGMObjectSerializer(new ObjectSerializerSettings()
        {
            AlwaysForceLowerCamelCaseMemberName = false,
            BindingFlags = BindingFlags.Instance | BindingFlags.Public,
            Options = SerializationOptions.SerializeEnumsAsStrings | SerializationOptions.UseCompositeArrayToSerializeLists
        });
        public static IAGMObjectSerializer ser { get => ser_; }

        public static IDictionary<string, object> toDictionary(object obj)
        {
            return ser.Deserialize<Dictionary<string, object>>(ser.Serialize(obj));
        }

        public SearchService(Glue42 glue)
        {
            glue_ = glue;
        }
        public void Register()
        {
            IServerMethod interopMethod = glue_.Interop.RegisterEndpoint(
                mdb => mdb.SetMethodName(SEARCH_PROVIDER_METHOD),
                HandleSearchRequest);
        }

        public void HandleSearchRequest(IServerMethod method, IMethodInvocationContext context, IInstance caller, IServerMethodResultBuilder resultBuilder, Action<IServerMethodResult> asyncResponseCallback, object cookie = null)
        {
            try
            {
                var param = ser.Deserialize<SearchMethodParam>(new Value(context.Arguments.ToArray(), false));

                if (param.operation.Equals(OPERATION_CANCEL))
                {
                    return;
                }
                if (param.operation.Equals(OPERATION_SEARCH))
                {
                    // Generate search id
                    var nowISOString = DateTime.UtcNow.ToString("O");
                    var id = nowISOString + "_" + param.search;
                    resultBuilder.SetContext(cb => cb.AddValue("id", id));

                    // ignore requests where the search string is too short or too long
                    if (param.search.Length < 4 || param.search.Length > 10)
                    {
                        Task.Run(() => SendSearchResults(id, null, STATUS_DONE, caller));
                    }
                    else
                    {
                        Task.Run(() => InitiateSearch(id, param.search, caller));
                    }
                    return;
                }
                throw new Exception($"Unsupported operation: '{param.operation}'");
            }
            catch (Exception e)
            {
                resultBuilder.SetContext(cb => { });
                resultBuilder.SetIsFailed(true);
                resultBuilder.SetMessage(e.Message);
            }
            finally
            {
                asyncResponseCallback(resultBuilder.Build());
            }
        }

        public async Task SendSearchResults(string id, List<SearchResultItem> items, string status, IInstance caller = null)
        {
            TargetSettings tSettings = null;
            if (caller is IInstance)
            {
                tSettings = new TargetSettings();
                tSettings.WithTargetSelector((method, instance) => instance.InstanceId.Equals(caller.InstanceId));
            }
            var invokeParam = new SearchResultParam(id)
            {
                status = status
            };
            if (items is List<SearchResultItem>)
            {
                invokeParam.items = items;
            }

            await glue_.Interop.Invoke(
                SEARCH_RESULTS_METHOD,
                toDictionary(invokeParam),
                tSettings
            );
        }

        public async Task InitiateSearch(string id, string searchString, IInstance caller)
        {
            try
            {
                // simulate delay from getting results from external system
                await Task.Delay(1000);

                // "transform the results" into search result items
                List<SearchResultItem> items = new List<SearchResultItem>();
                for (int counter = 1; counter <= 3; ++counter)
                {
                    items.Add(generateSampleResultItem(searchString, counter));
                }

                await SendSearchResults(id, items, STATUS_DONE, caller);
            }
            catch (Exception)
            {
                // complete the search with an empty result
                _ = SendSearchResults(id, null, STATUS_DONE, caller);
            }
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
                    @params = new Action01Parameters()
                    {
                        counter = counter,
                        id = itemId
                    }
                }
            };
            return result;
        }
    }
}
