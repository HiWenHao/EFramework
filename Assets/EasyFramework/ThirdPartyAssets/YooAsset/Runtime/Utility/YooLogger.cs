using System.Diagnostics;
using EasyFramework;

namespace YooAsset
{
	internal static class YooLogger
	{
		/// <summary>
		/// 日志
		/// </summary>
		[Conditional("DEBUG")]
		public static void Log(string info)
		{
			D.Log(info);
		}

		/// <summary>
		/// 警告
		/// </summary>
		public static void Warning(string info)
		{
			D.Warning(info);
		}

		/// <summary>
		/// 错误
		/// </summary>
		public static void Error(string info)
		{
			D.Error(info);
		}

		/// <summary>
		/// 异常
		/// </summary>
		public static void Exception(System.Exception exception)
		{
			D.Fatal(exception);
		}
	}
}