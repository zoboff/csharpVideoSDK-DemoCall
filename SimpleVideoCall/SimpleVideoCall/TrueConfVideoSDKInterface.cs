using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleVideoCall
{
    interface TrueConfVideoSDKInterface
    {
        // Methods
        void call(string trueconf_id);

        // Events
        event EventHandler<string> OnEvent;
        event EventHandler<string> OnMethod;
    }
}
