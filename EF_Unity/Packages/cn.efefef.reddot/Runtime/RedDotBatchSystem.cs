/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 15:12:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:12:19
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.RedDot
{
	/// <summary>
	/// 批处理系统：支持批量修改后统一刷新
	/// <para>English: Batch system - supports batch modifications and unified refresh</para>
	/// </summary>
	public class RedDotBatchSystem
	{
		private int _batchDepth;  // 批处理嵌套深度

		/// <summary>
		/// 是否处于批处理模式
		/// <para>English: Whether in batch mode</para>
		/// </summary>
		public bool IsBatching => _batchDepth > 0;

		/// <summary>
		/// 开始批处理
		/// <para>English: Begin batch processing</para>
		/// </summary>
		public void Begin()
		{
			_batchDepth++;
		}

		/// <summary>
		/// 结束批处理，若深度归零则触发刷新
		/// <para>English: End batch processing, trigger flush when depth reaches zero</para>
		/// </summary>
		public void End()
		{
			_batchDepth--;
			if (_batchDepth < 0)
				_batchDepth = 0;
			if (_batchDepth == 0)
			{
				RedDotManager.Instance.DirtySystem.Flush();
			}
		}
	}
}
