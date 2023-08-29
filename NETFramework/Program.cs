using System;
using System.Threading.Tasks;
using System.Linq;

using Tick42;
using Tick42.AppManager;

namespace NETFramework
{
    internal class Program
    {
        private static Glue42 glue_;
        private static SearchServiceNS.SearchService sps_;
        private static SearchServiceNS.Action01Handler handler01_;

        static void Main(string[] args)
        {
            var task = InitGlueAndService();
            task.Wait();

            var keyPrompt = "Press \"Escape\" to exit, \"G\" to launch global search";
            Console.WriteLine(keyPrompt);
            for (; ; )
            {
                var keyInfo = Console.ReadKey(true);
                Console.WriteLine($"Key pressed: {keyInfo.Key}");
                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    break;
                }
                if (keyInfo.Key == ConsoleKey.G)
                {
                    var app = glue_?.AppManager.Applications.FirstOrDefault(a => a.Name.Equals("glue42-global-search"));
                    if(app is object)
                    {
                        app.Start(AppManagerContext.CreateNew());
                    }
                    continue;
                }
                Console.WriteLine(keyPrompt);
            }
        }

        static async Task InitGlueAndService()
        {
            if (glue_ is Glue42)
            {
                // already initialized
                return;
            }
            Console.WriteLine("Connecting to platform...");
            glue_ = await Glue42.InitializeGlue();

            Console.WriteLine("Initializing Service...");
            sps_ = new SearchServiceNS.SearchService(glue_);
            sps_.Register();

            Console.WriteLine("Registering sample action handler...");
            handler01_ = new SearchServiceNS.Action01Handler(glue_);
            handler01_.Register();

            Console.WriteLine("Initialization complete.");
        }
    }
}
