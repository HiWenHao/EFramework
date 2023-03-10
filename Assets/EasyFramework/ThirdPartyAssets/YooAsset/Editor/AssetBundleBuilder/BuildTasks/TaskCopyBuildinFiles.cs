using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YooAsset.Editor
{
	[TaskAttribute("拷贝内置文件到流目录")]
	public class TaskCopyBuildinFiles : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParametersContext = context.GetContextObject<BuildParametersContext>();
			if (buildParametersContext.Parameters.CopyBuildinFileOption != ECopyBuildinFileOption.None)
			{
				CopyBuildinFilesToStreaming(buildParametersContext);
			}
		}

		/// <summary>
		/// 拷贝首包资源文件
		/// </summary>
		private void CopyBuildinFilesToStreaming(BuildParametersContext buildParametersContext)
		{
			ECopyBuildinFileOption option = buildParametersContext.Parameters.CopyBuildinFileOption;
			string packageOutputDirectory = buildParametersContext.GetPackageOutputDirectory();
			string streamingAssetsDirectory = AssetBundleBuilderHelper.GetStreamingAssetsFolderPath();
			string buildPackageName = buildParametersContext.Parameters.BuildPackage;
			string outputPackageCRC = buildParametersContext.OutputPackageCRC;

			// 加载补丁清单
			PatchManifest patchManifest = AssetBundleBuilderHelper.LoadPatchManifestFile(packageOutputDirectory, buildPackageName, outputPackageCRC);

			// 清空流目录
			if (option == ECopyBuildinFileOption.ClearAndCopyAll || option == ECopyBuildinFileOption.ClearAndCopyByTags)
			{
				AssetBundleBuilderHelper.ClearStreamingAssetsFolder();
			}

			// 拷贝补丁清单文件
			{
				string manifestFileName = YooAssetSettingsData.GetPatchManifestFileName(buildPackageName, outputPackageCRC);
				string sourcePath = $"{packageOutputDirectory}/{manifestFileName}";
				string destPath = $"{streamingAssetsDirectory}/{manifestFileName}";
				EditorTools.CopyFile(sourcePath, destPath, true);
			}

			// 拷贝静态版本文件
			{
				string versionFileName = YooAssetSettingsData.GetStaticVersionFileName(buildPackageName);
				string sourcePath = $"{packageOutputDirectory}/{versionFileName}";
				string destPath = $"{streamingAssetsDirectory}/{versionFileName}";
				EditorTools.CopyFile(sourcePath, destPath, true);
			}

			// 拷贝文件列表（所有文件）
			if (option == ECopyBuildinFileOption.ClearAndCopyAll || option == ECopyBuildinFileOption.OnlyCopyAll)
			{		
				foreach (var patchBundle in patchManifest.BundleList)
				{
					string sourcePath = $"{packageOutputDirectory}/{patchBundle.FileName}";
					string destPath = $"{streamingAssetsDirectory}/{patchBundle.FileName}";
					EditorTools.CopyFile(sourcePath, destPath, true);
				}
			}

			// 拷贝文件列表（带标签的文件）
			if (option == ECopyBuildinFileOption.ClearAndCopyByTags || option == ECopyBuildinFileOption.OnlyCopyByTags)
			{
				string[] tags = buildParametersContext.Parameters.CopyBuildinFileTags.Split(';');
				foreach (var patchBundle in patchManifest.BundleList)
				{
					if (patchBundle.HasTag(tags) == false)
						continue;
					string sourcePath = $"{packageOutputDirectory}/{patchBundle.FileName}";
					string destPath = $"{streamingAssetsDirectory}/{patchBundle.FileName}";
					EditorTools.CopyFile(sourcePath, destPath, true);
				}
			}

			// 刷新目录
			AssetDatabase.Refresh();
			BuildRunner.Log($"内置文件拷贝完成：{streamingAssetsDirectory}");
		}
	}
}