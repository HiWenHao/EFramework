/*
 *  ★★★★★  自定义下载  ★★★★★
 *  
 *  一、权限
 *      1、IOS与Android端都需要写入权限
 *  
 *  二、支持功能
 *      1、单个文件下载
 *      2、多个文件 同时 下载
 *      3、多个文件 分批 下载
 *      4、获取当此所要下载的文件大小
 *      5、下载后，会比对本地文件与服务器文件的MD5，如果不相同则会替代本地原有文件 *          
 *          
 *      以上所有函数皆有完成回调
 *      此外：
 *      6、下载中的时候：可获取到
 *          ①、正在下载文件的大小
 *          ②、当前已下载的个数进度
 *          ③、当前已下载的字节总量
 *          ④、当前下载位置
 *          ⑤、本次下载总大小
 *  
 *  三、使用、参数说明
 *      1、使用：实例化一个DownloadTools对象.函数名(参数)
 *      2、参数说明：
 *          assetName：单个资源名称                        eg：thisVideo.mp4
 *         assetsName：一个List<string>, 全部资源名称      eg：new List<string>(){ thisVideo.mp4,thisVideo.mp4,thisVideo.mp4,thisVideo.mp4}
 *     isDoneCallback：请求成功后的回调函数
 *        porjectName：资源路径名称，首先会创建自身文件夹，然后会在该文件下下创建video与audio两个文件夹存放资源文件
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using CustomUwrDownload;

//判断是否为分批下载处理
enum DownloadType
{
    None,
    Concurrently
}

/// <summary>
/// 资源下载控制器
/// </summary>
public class DownloadTools
{
    /// <summary> 项目名称 </summary>
    internal readonly static string PorjectName = "xxyy";
    /// <summary> 服务器头地址 </summary>
    internal readonly static string UrlHead = @"http://192.168.29.11:8080/xxmusic/L0/"; //资源下载的地址头

    #region 构造
    readonly string _urlHead;

    /// <summary>
    /// 创建一个下载，使用默认url
    /// </summary>
    public DownloadTools()
    {
        _urlHead = UrlHead;
        CreatAssetFolder(PorjectName);
    }

    /// <summary>
    /// 创建一个下载
    /// </summary>
    /// <param name="urlHead">一个链接头</param>
    public DownloadTools(string urlHead)
    {
        _urlHead = urlHead;
        CreatAssetFolder(PorjectName);
    }

    #endregion

    #region 对外属性
    /// <summary>
    /// 正在下载单一文件的下载进度，单位：b
    /// </summary>
    public float CurrentDownloadProgress
    {
        get
        {
            return DownloadUtils.Instance.downloadProgress * _downloadSize;
        }
    }

    /// <summary> 
    /// 下载个数进度
    /// 可获得0 ~ 1 的下载进度
    /// </summary>
    public float DownloadNumberProgress
    {
        get
        {
            if (_downloadType == DownloadType.Concurrently)
                return _concurrently_downloadProgress;
            else
                return _normal_downloadProgress;
        }
    }

    /// <summary>
    /// 当前字节总量下载进度 
    /// </summary>
    public float CurrentSizeProgress => _downloadCurrentSize;

    /// <summary>
    /// 当前下载位置
    /// 可获得 0 ~ 全部下载数的 索引值
    /// </summary>
    public int CurrentIndexProgress
    {
        get
        {
            if (_downloadType == DownloadType.Concurrently)
                return _concurrently_downloadLocation;
            else
                return _normal_downloadLocation;
        }
    }

    /// <summary>
    /// 本次下载总大小
    /// </summary>
    public float DownloadSize 
    {
        get
        {
            return _downloadSize;
        }
    }
    #endregion

    #region 通用    
    //              已经开始？
    private bool _isDownloading;
    //              判断是否为分批下载
    private DownloadType _downloadType;
    //             本次下载总大小     本次已下载量
    private float _downloadSize, _downloadCurrentSize;
    /// <summary>
    /// 复原
    /// </summary>
    private void normalRegress()
    {
        _downloadType = DownloadType.None;

        _normal_downloadLocation = 0;
        _normal_downloadMaxCount = _normal_downloadProgress = _downloadSize = _downloadCurrentSize = 0;

        _concurrently_downloadLocation = _concurrently_CurrenMaxCount = _concurrently_downloadMaxCount = 0;
        _concurrently_downloadProgress = 0;
        _concurrently_downloadSize = null;
        _concurrently_isDoneCallback = null;
        _concurrently_IsDone = false;

        _normal_downloadSize = 0;
        _normal_sizeLength = _normal_sizeCount = 0;
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Dispose()
    {
        normalRegress();
    }
    #endregion

    #region 正常下载        _normal
    //              本次下载最大数量                     进度           
    private float _normal_downloadMaxCount, _normal_downloadProgress;
    //              当前下载位置
    private int _normal_downloadLocation;


    /// <summary>
    /// 下载单个资源
    /// </summary>
    /// <param name="assetName">所要下载的资源名字</param>
    /// <param name="isDoneCallback">下载完成回调</param>
    public void DownloadSingle(string assetName, Action isDoneCallback = null)
    {
        GetDownloadSizeSingle(assetName, (size) =>
        {
            _downloadSize = size;
        });
        DownloadUtils.Instance.StartDownload(assetName, _urlHead, isDoneCallback);
    }

    /// <summary>
    /// 下载全部资源，下载内容体量过大的时候会卡顿，建议使用分批下载
    /// </summary>
    /// <param name="assetsName">资源名字列表</param>
    /// <param name="isDoneCallback">下载完成回调</param>
    public void DownloadAll(List<string> assetsName, Action isDoneCallback = null)
    {
        if (_isDownloading)
        {
            Debug.LogError("请勿重复下载");
            return;
        }
        _isDownloading = true;

        normalRegress();
        GetDownloadSizeAll(assetsName,(size)=> 
        {
            _downloadSize = size;
        });
        downloadAll(assetsName, () =>
        {
            isDoneCallback?.Invoke();
            _isDownloading = false;
        });
    }

    /// <summary>
    /// 下载全部资源，下载内容体量过大的时候会卡顿，建议使用分批下载
    /// </summary>
    /// <param name="assetsName">资源名字列表</param>
    /// <param name="isDoneCallback">下载完成回调</param>
    private void downloadAll(List<string> assetsName, Action isDoneCallback = null)
    {
        _normal_downloadLocation = 0;
        _normal_downloadMaxCount = assetsName.Count;

        for (int i = assetsName.Count - 1; i >= 0; i--)
        {
            DownloadSingle(assetsName[i], () =>
              {
                  #region 回调
                  if (_downloadType == DownloadType.Concurrently)
                      _concurrently_downloadProgress = ++_concurrently_downloadLocation / (float)_concurrently_downloadMaxCount;
                  
                  _normal_downloadProgress = ++_normal_downloadLocation / _normal_downloadMaxCount;

                  if (_normal_downloadProgress == 1.0f || _concurrently_downloadProgress == 1.0f)
                  {
                      isDoneCallback?.Invoke();
                  }
                  #endregion
              });
        }
    }
    #endregion

    #region 分批下载        _concurrently
    //          同时最大下载数                     当前下载位置              当前位置+最大                     总数量
    int _concurrently_MaxLoadCount = 1, _concurrently_downloadLocation, _concurrently_CurrenMaxCount, _concurrently_downloadMaxCount;
    //          同时下载进度
    float _concurrently_downloadProgress;
    //          同时所下大小
    List<string> _concurrently_downloadSize;
    //  全部下载完成后的回调
    Action _concurrently_isDoneCallback;
    //  下载完成的标记
    bool _concurrently_IsDone;

    /// <summary>
    /// 分批下载，可同时最大下载数
    /// </summary>
    /// <param name="assetsName">资源名字列表</param>
    /// <param name="isDoneCallback">下载完成回调</param>
    /// <param name="maxLoadCount">同时最大下载数量</param>
    public void DownloadConcurrently(List<string> assetsName, int maxLoadCount, Action isDoneCallback = null)
    {
        if (_isDownloading)
        {
            Debug.LogError("请勿重复下载");
            return;
        }
        _isDownloading = true;

        normalRegress();
        _concurrently_downloadMaxCount = assetsName.Count;
        this._concurrently_MaxLoadCount = maxLoadCount;
        _concurrently_IsDone = false;
        _concurrently_isDoneCallback = isDoneCallback;
        _concurrently_downloadSize = new List<string>(maxLoadCount);
        _downloadType = DownloadType.Concurrently;

        GetDownloadSizeAll(assetsName, (size) =>
        {
            _downloadSize = size;
        });
        concurrentlyDownload(assetsName);
    }

    private void concurrentlyDownload(List<string> assetsName)
    {
        _concurrently_downloadSize.Clear();
        _concurrently_CurrenMaxCount = _concurrently_MaxLoadCount + _concurrently_downloadLocation;
        for (int i = _concurrently_downloadLocation; i < _concurrently_CurrenMaxCount; i++)
        {
            if (i < assetsName.Count)
            {
                _concurrently_downloadSize.Add(assetsName[i]);
            }
            else
            {
                _concurrently_IsDone = true;
                break;
            }
        }

        downloadAll(_concurrently_downloadSize, () =>
         {
             #region 一个已完成回调
             if (_concurrently_IsDone)
             {
                 _concurrently_isDoneCallback?.Invoke();
                 _isDownloading = false;
                 return;
             }

             concurrentlyDownload(assetsName);
             #endregion
         });
    }
    #endregion

    #region 获取文件大小    _normal
    //                  大小值
    private float _normal_downloadSize;
    //               总大小的长度
    private int _normal_sizeLength, _normal_sizeCount;
    /// <summary>
    /// 查看单个下载文件大小
    /// </summary>
    /// <param name="assetName">单个资源名称</param>
    /// <param name="isDoneCallback">返回文件大小</param>
    public void GetDownloadSizeSingle(string assetName, Action<float> isDoneCallback = null)
    {
        DownloadUtils.Instance.GetAllFileSize(assetName, _urlHead, isDoneCallback);
    }

    /// <summary>
    /// 查看所有下载文件大小
    /// </summary>
    /// <param name="assetsName">单个资源名称</param>
    /// <param name="isDoneCallback">返回所有文件的大小</param>
    public void GetDownloadSizeAll(List<string> assetsName, Action<float> isDoneCallback = null)
    {
        _normal_downloadSize = _normal_sizeLength = 0;
        _normal_sizeCount = assetsName.Count;
        for (int i = _normal_sizeCount - 1; i >= 0; i--)
        {
            GetDownloadSizeSingle(assetsName[i], (singleSize) =>
              {
                  _normal_downloadSize += singleSize;
                  if (++_normal_sizeLength == _normal_sizeCount)
                      isDoneCallback?.Invoke(_normal_downloadSize);
              });
        }
    }
    #endregion

    #region 建文件夹        _normal
    /// <summary>
    /// 创建persistentDataPath下该名字文件夹，和其子文件夹（下载缓存目录）
    /// </summary>
    /// <param name="porjectName">首文件夹名字</param>
    public void CreatAssetFolder(string porjectName)
    {
        UwrFolderHandling.CreatDownloadAssetsFolder(porjectName);
    }

    /// <summary>
    /// 删除persistentDataPath下该名字文件夹下的所有子文件夹
    /// </summary>
    /// <param name="porjectName">想要删除子文件夹的，文件夹名字</param>
    public void DeleteAssetFolder(string porjectName)
    {
        UwrFolderHandling.DeleteDownloadAssets(porjectName);
    }
    #endregion
}
