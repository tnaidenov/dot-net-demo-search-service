using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Tick42;
using DOT.AGM;
using DOT.AGM.Server;
using System.Linq;

namespace SearchServiceNS
{
    public class Action01Parameters
    {
        public int counter { get; set; }
        public string id { get; set; }
    }
    public class Action01Handler
    {
        public static string ACTION_HANDLER_METHOD = "HandleAction01";
        private Glue42 glue_;

        [DllImport("User32.dll", EntryPoint = "MessageBoxW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(int hWnd, string text, string caption, uint type);

        public Action01Handler(Glue42 glue)
        {
            glue_ = glue;
        }

        public void Register()
        {
            IServerMethod interopMethod = glue_.Interop.RegisterEndpoint(
                mdb => mdb.SetMethodName(ACTION_HANDLER_METHOD),
                HandleInvocation);
        }

        public void HandleInvocation(IServerMethod method, IMethodInvocationContext context, IInstance caller, IServerMethodResultBuilder resultBuilder, Action<IServerMethodResult> asyncResponseCallback, object cookie = null)
        {
            try
            {
                var ser = glue_.AGMObjectSerializer;
                var dict = ser.Deserialize<Dictionary<string,object>>(new Value(context.Arguments.ToArray(), false));
                var jsonString = System.Text.Json.JsonSerializer.Serialize(dict);
                MessageBoxW(0,
                    "Parameters as JSON:\n" + jsonString,
                    "Action 01 Invoked",
                    0x00000040 /*MB_ICONINFORMATION*/ |
                    0x00010000 /*MB_SETFOREGROUND*/ |
                    0x00040000 /*MB_TOPMOST*/
                );
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
    }
}
