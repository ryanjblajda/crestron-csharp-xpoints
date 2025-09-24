using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using SIMPL;

namespace Blajda.xPoints
{
    public static class xPointUtilities
    {
        public static bool IsDebug;
        public static bool IsVerbose;

        public static void Verbose(ushort enable)
        {
            xPointUtilities.IsVerbose = SIMPL.Conversion.ConvertToBool(enable);
        }

        public static void Debug(ushort enable)
        {
            xPointUtilities.IsDebug = SIMPL.Conversion.ConvertToBool(enable);
        }
    }
}