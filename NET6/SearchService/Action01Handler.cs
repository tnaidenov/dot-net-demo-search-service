using System.Runtime.InteropServices;

using Glue;
using Glue.Services;

namespace SearchServiceNS
{
    public class Action01Parameters
    {
        public int counter { get; set; }
        public string? id { get; set; }
    }

    public class Action01Handler
    {
        public static string ACTION_HANDLER_METHOD = "HandleAction01";
        private Glue42 glue_;

        [DllImport("User32.dll", EntryPoint = "MessageBoxW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(int hWnd, string text, string caption, uint type);

        public Action01Handler (Glue42 glue)
        {
            glue_ = glue;
        }

        public async Task Register()
        {
            IServerMethod interopMethod = await glue_.Interop.RegisterEndpoint(
                mdb => mdb.SetName(ACTION_HANDLER_METHOD),
                HandleInvocation);
        }

        public void HandleInvocation(InvocationContext ictx)
        {
            try
            {
                var ser = glue_.Serializer;
                var dict = ser.Deserialize<Dictionary<string, object>>(ser.Serialize(ictx.Arguments));
                var jsonString = System.Text.Json.JsonSerializer.Serialize(dict);

                MessageBoxW(0,
                    "Parameters as JSON:\n" + jsonString,
                    "Action 01 Invoked",
                    0x00000040 /*MB_ICONINFORMATION*/ |
                    0x00010000 /*MB_SETFOREGROUND*/ |
                    0x00040000 /*MB_TOPMOST*/
                );
                ictx.ResultBuilder.Succeed();
            }
            catch (Exception e)
            {
                ictx.ResultBuilder.SetIsFailed(e.Message);
            }
        }
    }
}
