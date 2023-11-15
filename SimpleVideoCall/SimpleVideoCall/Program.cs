using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleVideoCall
{
    class Program
    {
        public static readonly string TRUECONF_ID = "azobov@team.trueconf.com";
        public static async Task Main(string[] args)
        {
            TrueConfVideoSDKWrapper sdk = new TrueConfVideoSDKWrapper(false);

            sdk.OnEvent += OnEvent;
            sdk.OnMethod += OnMethod;

            sdk.OpenSession("127.0.0.1", 9090, "123");

            sdk.call(TRUECONF_ID);

            Task.Delay(TimeSpan.FromMilliseconds(2000)).Wait();

            Console.WriteLine(">>>>>>>>>>>>>> Press any key to exit... <<<<<<<<<<<<<<<<<<");
            Console.ReadKey();
        }

        static void OnEvent(object source, string response)
        {
            var clr = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OnEvent: {0}", response);
            Console.ForegroundColor = clr;
        }
        static void OnMethod(object source, string response)
        {
            var clr = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("OnMethod: {0}", response);
            Console.ForegroundColor = clr;
        }
    }
}
