using System;
using System.Collections.Generic;
using System.Text;

namespace DevelopCommon.Extensions
{
    public static class ArgumentExtensions
    {
		public static void CheckArgument(this object arg, string argName)
		{
			if (arg == null)
				throw new ArgumentNullException(argName);
		}
		public static void CheckNullOrEmpty(this string arg, string argName)
		{
			arg.CheckArgument(argName);

			if (string.IsNullOrEmpty(arg))
				throw new ArgumentException(argName);
		}
		public static void CheckNullOrWhiteSpace(this string arg, string argName)
		{
			if (string.IsNullOrWhiteSpace(arg))
				throw new ArgumentNullException(argName);
		}
	}
}
