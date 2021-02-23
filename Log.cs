using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OMIChecklist
{
    public static class Log
    {
        public static bool Active = false;

        public static void Write(String msg)
        {
            if (Active)
              UnityEngine.Debug.Log("OMIC:" + msg);
        }
    }
}
