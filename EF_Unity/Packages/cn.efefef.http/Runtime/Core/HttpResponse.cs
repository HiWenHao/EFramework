/*
 * ================================================
 * Describe:      HTTP 统一响应模型。发送器把各类 DownloadHandler 的结果收敛到本对象，门面按需要取出类型化结果。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 统一响应，承载文本 / 字节 / 纹理 / 资源包 / 文件路径等结果
    /// <para>Unified HTTP response carrying text, bytes, texture, asset bundle or file path</para>
    /// </summary>
    public sealed class HttpResponse
    {
        /// <summary>
        /// 是否成功（result == Success）
        /// <para>Whether the request succeeded</para>
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// HTTP 状态码（UnityWebRequest.responseCode）
        /// <para>HTTP status code</para>
        /// </summary>
        public long StatusCode;

        /// <summary>
        /// 失败原因（result != Success 时）
        /// <para>Failure reason when result != Success</para>
        /// </summary>
        public string Error;

        /// <summary>
        /// 文本结果（Result == Text 时有效）
        /// <para>Text result (valid when Result == Text)</para>
        /// </summary>
        public string Text;

        /// <summary>
        /// 字节结果（Result == Text 时有效）
        /// <para>Byte result (valid when Result == Text)</para>
        /// </summary>
        public byte[] Bytes;

        /// <summary>
        /// 纹理结果（Result == Texture 时有效）
        /// <para>Texture result (valid when Result == Texture)</para>
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// 资源包结果（Result == AssetBundle 时有效）
        /// <para>AssetBundle result (valid when Result == AssetBundle)</para>
        /// </summary>
        public AssetBundle Bundle;

        /// <summary>
        /// 文件保存路径（Result == File 时有效）
        /// <para>File save path (valid when Result == File)</para>
        /// </summary>
        public string SavePath;
    }
}