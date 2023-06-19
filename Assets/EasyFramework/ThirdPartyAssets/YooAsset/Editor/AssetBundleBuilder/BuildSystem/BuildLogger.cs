using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;

namespace YooAsset.Editor
{
	public static class BuildLogger
	{
		private static bool _enableLog = true;

		public static void InitLogger(bool enableLog)
		{
			_enableLog = enableLog;
		}

		public static void Log(string message)
		{
			if (_enableLog)
			{
				D.Log("[YooAsset] ► " + message);
			}
		}
		public static void Warning(string message)
		{
			D.Warning("[YooAsset] ► " + message);
		}
		public static void Error(string message)
		{
			D.Error("[YooAsset] ► " + message);
		}
	}
}