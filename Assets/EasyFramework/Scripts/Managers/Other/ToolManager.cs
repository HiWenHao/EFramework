/* 
 * ================================================
 * Describe:      This script is used to help other managers. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-07-15 10:43:32
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-07-15 10:43:32
 * Version:       0.1
 * ===============================================
*/
using EasyFramework.Framework.Core;
using System;
using UnityEngine;
using XHTools;

namespace EasyFramework.Managers
{
    /// <summary>
    /// To help other managers.
    /// </summary>
    public class ToolManager : Singleton<ToolManager>, ISingleton, IManager
    {
        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {

        }

        #region Related to find.  查找相关
        /// <summary>
        /// Recursive search transform with name. 根据名字递归查找
        /// </summary>
        /// <param name="parent">Want to find object`s parent. 想要查找物体的父级</param>
        /// <param name="name">object name. 对象名字</param>
        /// <returns></returns>
        public Transform RecursiveSearch(Transform parent, string name)
        {
            Transform _target = parent.Find(name);
            if (_target)
                return _target;

            for (int i = 0; i < parent.childCount; i++)
            {
                _target = RecursiveSearch(parent.GetChild(i), name);

                if (_target != null)
                    return _target;
            }

            return _target;
        }

        /// <summary>
        /// Recursive search transform with name. 根据名字递归查找类型
        /// </summary>
        /// <typeparam name="T">The component type with T. T类型组件</typeparam>
        /// <param name="parent">Want to find object`s parent. 想要查找物体的父级</param>
        /// <param name="name">object name. 对象名字</param>
        /// <returns></returns>
        public T RecursiveSearch<T>(Transform parent, string name) where T : Component
        {
            Transform _target = parent.Find(name);
            T _component = null;
            if (_target)
            {
                if (_target.TryGetComponent(out _component))
                    return _component;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                _target = RecursiveSearch(parent.GetChild(i), name);

                if (_target)
                {
                    if (_target.TryGetComponent(out _component))
                        return _component;
                }
            }
            if (_target && !_component)
                D.Error($"The object of name:{name} has been found, but the type of {typeof(T)} component not found. ");
            else if (!_target)
                D.Error($"The object of name:{name} has been not found.");
            return _component;
        }
        #endregion

        #region Related to audio. 音频相关
        /// <summary>
        ///  Byte stream data to audio clip.字节流数据转音频文件
        /// </summary>
        /// <param name="data">The byte stream.字节流数据</param>
        /// <returns>AudioClip.</returns>
        public AudioClip BytesToAudioClip(byte[] data)
        {
            float[] _clipData = new float[data.Length / 2];

            for (int i = 0; i < _clipData.Length; i++)
            {
                _clipData[i] = byteFileToFloat(data[i * 2], data[i * 2 + 1]);
            }

            AudioClip _clip = AudioClip.Create("audioClip", _clipData.Length, 1, 16000, false);
            _clip.SetData(_clipData, 0);
            return _clip;
        }

        /// <summary>
        ///  Float array to audio clip.单精度浮点型数组转音频文件
        /// </summary>
        /// <param name="data">The byte stream.字节流数据</param>
        /// <returns>AudioClip.</returns>
        public AudioClip FloatArrayToAudioClip(float[] data)
        {
            AudioClip _clip = AudioClip.Create("audioClip", data.Length, 1, 16000, false);
            _clip.SetData(data, 0);
            return _clip;
        }
        private float byteFileToFloat(byte first, byte second)
        {
            short s;
            if (BitConverter.IsLittleEndian)
                s = (short)(second << 8 | first);
            else
                s = (short)(first << 8 | second);
            return s / 32768.0f;
        }
        #endregion

        #region Related to screen.屏幕相关
        /// <summary>
        /// Set screen orientation.   设置屏幕朝向
        /// </summary>
        /// <param name="isPortrait">set to portrait? 设为竖屏?</param>
        public void ChangeScreenOrientation(OrientationType orientation)
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            switch (orientation)
            {
                case OrientationType.Portrait:
                    Screen.autorotateToPortrait = true;
                    break;
                case OrientationType.PortraitUpsideDown:
                    Screen.autorotateToPortraitUpsideDown = true;
                    break;
                case OrientationType.LandscapeLeft:
                    Screen.autorotateToLandscapeLeft = true;
                    break;
                case OrientationType.LandscapeRight:
                    Screen.autorotateToLandscapeRight = true;
                    break;
            }
        }

        /// <summary>
        /// Screen position to world point.屏幕坐标转世界坐标
        /// </summary>
        /// <param name="screenPoint">screen point.  屏幕坐标，一般为Input.mousePosition</param>
        /// <param name="camera"> The camera to use to lookover .用来观察的相机.</param>
        /// <param name="planeZ">Position with z axial.Z轴位置. </param>
        /// <returns></returns>
        public Vector3 ScreenPointToWorldPoint(Vector2 screenPoint, Camera camera, float planeZ)
        {
            Vector3 position = new Vector3(screenPoint.x, screenPoint.y, planeZ);
            Vector3 worldPoint = camera.ScreenToWorldPoint(position);
            return worldPoint;
        }
        #endregion


        /// <summary>
        /// Determine whether to re-enter the sector area.判断是否在扇形范围
        /// </summary>
        /// <param name="observer">观察者</param>
        /// <param name="target">被观察目标</param>
        /// <param name="angle">扇形角度</param>
        /// <param name="radius">扇形半径</param>
        /// <returns>bool</returns>
        public bool InTheSector(Transform observer, Transform target, float angle, float radius)
        {
            Vector3 _dis = target.position - observer.position;

            float _angle = Mathf.Acos(Vector3.Dot(_dis.normalized, observer.forward)) * Mathf.Rad2Deg;

            if (_angle < angle * 0.5f && _dis.magnitude < radius)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Uesr the camera shot a texture2d.通过摄像机截取一张图片
        /// </summary>
        /// <param name="camera">截图片的相机</param>
        /// <param name="width">宽</param>
        /// <param name="height">长</param>
        /// <returns>Texture2D</returns>
        public Texture2D Screenshots(Camera camera, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 16);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            Texture2D t = new Texture2D(width, height);
            t.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            t.Apply();
            return t;
        }
    }
    /// <summary>
    /// The screen orientation type.屏幕方向类型
    /// </summary>
    public enum OrientationType
    {
        /// <summary>
        /// 竖屏
        /// </summary>
        Portrait = 0,
        /// <summary>
        /// 竖屏反转
        /// </summary>
        PortraitUpsideDown,
        /// <summary>
        /// 左横屏
        /// </summary>
        LandscapeLeft,
        /// <summary>
        /// 右横屏
        /// </summary>
        LandscapeRight,
    }
}
