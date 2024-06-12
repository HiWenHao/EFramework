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

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace EasyFramework.Managers
{
    /// <summary>
    /// To help other managers.
    /// <para>工具管理器</para>
    /// </summary>
    public class ToolManager : Singleton<ToolManager>, IManager
    {
        int m_managerLevel = -99;
        int IManager.ManagerLevel
        {
            get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }

        void ISingleton.Init()
        {
            m_screenHalf = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
        }

        void ISingleton.Quit()
        {

        }

        #region Related to find.  查找相关
        /// <summary>
        /// Recursive search transform with name. 
        /// <para>根据名字递归查找</para>
        /// </summary>
        /// <param name="parent">Want to find object`s parent. 
        /// <para>想要查找物体的父级</para></param>
        /// <param name="name">object name. 
        /// <para>对象名字</para></param>
        /// <returns>Get the find object.
        /// <para>获取查找对象</para></returns>
        public Transform Find(Transform parent, string name)
        {
            Transform _target = parent.Find(name);
            if (_target)
                return _target;

            for (int i = 0; i < parent.childCount; i++)
            {
                _target = Find(parent.GetChild(i), name);

                if (_target != null)
                    return _target;
            }

            return _target;
        }

        /// <summary>
        /// Recursive search transform with name. 
        /// <para>根据名字递归查找类型</para>
        /// </summary>
        /// <typeparam name="T">The component type with T. 
        /// <para>T类型组件</para></typeparam>
        /// <param name="parent">Want to find object`s parent. 
        /// <para>想要查找物体的父级</para></param>
        /// <param name="name">object name. 
        /// <para>对象名字</para></param>
        /// <returns>Get the component with type of T. 
        /// <para> 获取T类型的对象</para></returns>
        public T Find<T>(Transform parent, string name) where T : Component
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
                _target = Find(parent.GetChild(i), name);

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
        /// Audio clipto byte stream data.
        /// <para>音频文件转字节流数据</para>
        /// </summary>
        /// <param name="clip">Audio clip data<para>音频数据</para></param>
        /// <returns>The byte[] for audio clip. 
        /// <para>音频的字节数据</para></returns>
        public byte[] AudioClipToBytes(AudioClip clip)
        {
            float[] _samples = new float[clip.samples];

            clip.GetData(_samples, 0);

            short[] _intData = new short[_samples.Length];

            byte[] _bytesData = new byte[_samples.Length * 2];

            int _rescaleFactor = 32767;

            for (int i = 0; i < _samples.Length; i++)
            {
                _intData[i] = (short)(_samples[i] * _rescaleFactor);
                byte[] _byteArr = new byte[2];
                _byteArr = BitConverter.GetBytes(_intData[i]);
                _byteArr.CopyTo(_bytesData, i * 2);
            }
            return _bytesData;
        }

        /// <summary>
        ///  Byte stream data to audio clip.
        ///  <para>字节流数据转音频文件</para>
        /// </summary>
        /// <param name="data">The byte stream.<para>字节流数据</para></param>
        /// <returns>AudioClip.<para>音频数据</para></returns>
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
        ///  Float array to audio clip.
        ///  <para>单精度浮点型数组转音频文件</para>
        /// </summary>
        /// <param name="data">The byte stream.
        /// <para>字节流数据</para></param>
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

        #region WAV
        // Force save as 16-bit .wav
        const int BlockSize_16Bit = 2;
        /// <summary>
        /// Load the audio clip with unity data path.
        /// <para>通过路径加载音频文件</para>
        /// </summary>
        /// <param name="filePath">The local file path. 
        /// <para>文件路径</para></param>
        /// <param name="name">the file name, please bring the suffix, eg: .wav
        /// <para>文件名字，请带着后缀,例如: .wav</para></param>
        public AudioClip GetAudioClipWithPath(string filePath, string name)
        {
            if (!filePath.StartsWith(Application.persistentDataPath) && !filePath.StartsWith(Application.dataPath))
            {
                D.Warning("This only supports files that are stored using Unity's Application data path. \nTo load bundled resources use 'Resources.Load(\"filename\") typeof(AudioClip)' method. \nhttps://docs.unity3d.com/ScriptReference/Resources.Load.html");
                return null;
            }
            byte[] fileBytes = File.ReadAllBytes(filePath + "/" + name);
            return ByteArrayToAudioClip(fileBytes, name);
        }

        /// <summary>
        /// Get one audio clip with byte array
        /// <para>根据字节数据获取音频文件</para>
        /// </summary>
        /// <param name="fileBytes">Byte[] - A byte array
        /// <para>一个字节数组</para></param>
        /// <param name="name"> Audio clip name for byte array.
        /// <para>被创建音频的名字</para></param>
        /// <returns></returns>
        /// <exception cref="Exception">不支持位深度</exception>
        public AudioClip ByteArrayToAudioClip(byte[] fileBytes, string name)
        {
            int subchunk1 = BitConverter.ToInt32(fileBytes, 16);
            ushort audioFormat = BitConverter.ToUInt16(fileBytes, 20);
            // NB: Only uncompressed PCM wav files are supported.
            string formatCode = FormatCode(audioFormat);
            Debug.AssertFormat(audioFormat == 1 || audioFormat == 65534, "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", audioFormat, formatCode);
            ushort channels = BitConverter.ToUInt16(fileBytes, 22);
            int sampleRate = BitConverter.ToInt32(fileBytes, 24);
            ushort bitDepth = BitConverter.ToUInt16(fileBytes, 34);
            int headerOffset = 16 + 4 + subchunk1 + 4;
            int subchunk2 = BitConverter.ToInt32(fileBytes, headerOffset);
            float[] data = bitDepth switch
            {
                8 => Convert8BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2),
                16 => Convert16BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2),
                24 => Convert24BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2),
                32 => Convert32BitByteArrayToAudioClipData(fileBytes, headerOffset, subchunk2),
                _ => throw new Exception(bitDepth + " bit depth is not supported."),
            };
            AudioClip audioClip = AudioClip.Create(name, data.Length, channels, sampleRate, false);
            audioClip.SetData(data, 0);
            return audioClip;
        }

        #region wav file bytes to Unity AudioClip conversion methods wav文件字节到Unity AudioClip的转换方法
        private float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 8-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);
            float[] data = new float[wavSize];
            sbyte maxValue = sbyte.MaxValue;
            int i = 0;
            while (i < wavSize)
            {
                data[i] = (float)source[i] / maxValue;
                ++i;
            }
            return data;
        }
        private float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 16-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);
            int x = sizeof(Int16); // block size = 2
            int convertedSize = wavSize / x;
            float[] data = new float[convertedSize];
            Int16 maxValue = Int16.MaxValue;
            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
                ++i;
            }
            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);
            return data;
        }
        private float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 24-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);
            int x = 3; // block size = 3
            int convertedSize = wavSize / x;
            int maxValue = Int32.MaxValue;
            float[] data = new float[convertedSize];
            byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset
            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                Buffer.BlockCopy(source, offset, block, 1, x);
                data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
                ++i;
            }
            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);
            return data;
        }
        private float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
        {
            int wavSize = BitConverter.ToInt32(source, headerOffset);
            headerOffset += sizeof(int);
            Debug.AssertFormat(wavSize > 0 && wavSize == dataSize, "Failed to get valid 32-bit wav size: {0} from data bytes: {1} at offset: {2}", wavSize, dataSize, headerOffset);
            int x = sizeof(float); // block size = 4
            int convertedSize = wavSize / x;
            Int32 maxValue = Int32.MaxValue;
            float[] data = new float[convertedSize];
            int offset = 0;
            int i = 0;
            while (i < convertedSize)
            {
                offset = i * x + headerOffset;
                data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
                ++i;
            }
            Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);
            return data;
        }
        #endregion

        /// <summary>
        /// Make one audio clip convert to byte array.
        /// <para>使一个音频文件转换为字节数组</para>
        /// </summary>
        /// <param name="audioClip">音频文件</param>
        /// <returns>Byte array<para>字节数组</para></returns>
        public byte[] AudioClipToByteArray(AudioClip audioClip)
        {
            MemoryStream stream = new MemoryStream();
            const int headerSize = 44;
            // get bit depth
            ushort bitDepth = 16; //BitDepth (audioClip);
                                  // NB: Only supports 16 bit
                                  //Debug.AssertFormat (bitDepth == 16, "Only converting 16 bit is currently supported. The audio clip data is {0} bit.", bitDepth);
                                  // total file size = 44 bytes for header format and audioClip.samples * factor due to float to Int16 / sbyte conversion
            int fileSize = audioClip.samples * BlockSize_16Bit + headerSize; // BlockSize (bitDepth)
                                                                             // chunk descriptor (riff)
            WriteFileHeader(ref stream, fileSize);
            // file header (fmt)
            WriteFileFormat(ref stream, audioClip.channels, audioClip.frequency, bitDepth);
            // data chunks (data)
            WriteFileData(ref stream, audioClip, bitDepth);
            byte[] bytes = stream.ToArray();
            // Validate total bytes
            //Debug.AssertFormat(bytes.Length == fileSize, "Unexpected AudioClip to wav format byte count: {0} == {1}", bytes.Length, fileSize);
            // Save file to persistant storage location
            stream.Dispose();
            return bytes;
        }

        #region write .wav file functions
        private int WriteFileHeader(ref MemoryStream stream, int fileSize)
        {
            int count = 0;
            int total = 12;
            // riff chunk id
            byte[] riff = Encoding.ASCII.GetBytes("RIFF");
            count += WriteBytesToMemoryStream(ref stream, riff, "ID");
            // riff chunk size
            int chunkSize = fileSize - 8; // total size - 8 for the other two fields in the header
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(chunkSize), "CHUNK_SIZE");
            byte[] wave = Encoding.ASCII.GetBytes("WAVE");
            count += WriteBytesToMemoryStream(ref stream, wave, "FORMAT");
            // Validate header
            Debug.AssertFormat(count == total, "Unexpected wav descriptor byte count: {0} == {1}", count, total);
            return count;
        }
        private int WriteFileFormat(ref MemoryStream stream, int channels, int sampleRate, ushort bitDepth)
        {
            int count = 0;
            int total = 24;
            byte[] id = Encoding.ASCII.GetBytes("fmt ");
            count += WriteBytesToMemoryStream(ref stream, id, "FMT_ID");
            int subchunk1Size = 16; // 24 - 8
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(subchunk1Size), "SUBCHUNK_SIZE");
            ushort audioFormat = 1;
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(audioFormat), "AUDIO_FORMAT");
            ushort numChannels = Convert.ToUInt16(channels);
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(numChannels), "CHANNELS");
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(sampleRate), "SAMPLE_RATE");
            int byteRate = sampleRate * channels * BytesPerSample(bitDepth);
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(byteRate), "BYTE_RATE");
            ushort blockAlign = Convert.ToUInt16(channels * BytesPerSample(bitDepth));
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(blockAlign), "BLOCK_ALIGN");
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(bitDepth), "BITS_PER_SAMPLE");
            // Validate format
            Debug.AssertFormat(count == total, "Unexpected wav fmt byte count: {0} == {1}", count, total);
            return count;
        }
        private int WriteFileData(ref MemoryStream stream, AudioClip audioClip, ushort bitDepth)
        {
            int count = 0;
            int total = 8;
            // Copy float[] data from AudioClip
            float[] data = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(data, 0);
            byte[] bytes = ConvertAudioClipDataToInt16ByteArray(data);
            byte[] id = Encoding.ASCII.GetBytes("data");
            count += WriteBytesToMemoryStream(ref stream, id, "DATA_ID");
            int subchunk2Size = Convert.ToInt32(audioClip.samples * BlockSize_16Bit); // BlockSize (bitDepth)
            count += WriteBytesToMemoryStream(ref stream, BitConverter.GetBytes(subchunk2Size), "SAMPLES");
            // Validate header
            Debug.AssertFormat(count == total, "Unexpected wav data id byte count: {0} == {1}", count, total);
            // Write bytes to stream
            count += WriteBytesToMemoryStream(ref stream, bytes, "DATA");
            // Validate audio data
            //Debug.AssertFormat(bytes.Length == subchunk2Size, "Unexpected AudioClip to wav subchunk2 size: {0} == {1}", bytes.Length, subchunk2Size);
            return count;
        }
        private byte[] ConvertAudioClipDataToInt16ByteArray(float[] data)
        {
            MemoryStream dataStream = new MemoryStream();
            int x = sizeof(Int16);
            Int16 maxValue = Int16.MaxValue;
            int i = 0;
            while (i < data.Length)
            {
                dataStream.Write(BitConverter.GetBytes(Convert.ToInt16(data[i] * maxValue)), 0, x);
                ++i;
            }
            byte[] bytes = dataStream.ToArray();
            // Validate converted bytes
            Debug.AssertFormat(data.Length * x == bytes.Length, "Unexpected float[] to Int16 to byte[] size: {0} == {1}", data.Length * x, bytes.Length);
            dataStream.Dispose();
            return bytes;
        }
        private int WriteBytesToMemoryStream(ref MemoryStream stream, byte[] bytes, string tag = "")
        {
            int count = bytes.Length;
            stream.Write(bytes, 0, count);
            //Debug.LogFormat ("WAV:{0} wrote {1} bytes.", tag, count);
            return count;
        }
        #endregion

        /// <summary>
        /// Calculates the bit depth of an AudioClip
        /// <para>计算音频文件的位深度</para>
        /// </summary>
        /// <returns>The bit depth. Should be 8 or 16 or 32 bit.
        /// <para>位深度, 应该是8位，16位或32位</para></returns>
        /// <param name="audioClip">Audio clip.<para>音频文件</para></param>
        public ushort BitDepth(AudioClip audioClip)
        {
            ushort bitDepth = Convert.ToUInt16(audioClip.samples * audioClip.channels * audioClip.length / audioClip.frequency);
            Debug.AssertFormat(bitDepth == 8 || bitDepth == 16 || bitDepth == 32, "Unexpected AudioClip bit depth: {0}. Expected 8 or 16 or 32 bit.", bitDepth);
            return bitDepth;
        }
        private int BytesPerSample(ushort bitDepth)
        {
            return bitDepth / 8;
        }
        private string FormatCode(ushort code)
        {
            switch (code)
            {
                case 1:
                    return "PCM";
                case 2:
                    return "ADPCM";
                case 3:
                    return "IEEE";
                case 7:
                    return "μ-law";
                case 65534:
                    return "WaveFormatExtensable";
                default:
                    D.Warning("Unknown wav code format:" + code);
                    return "";
            }
        }
        #endregion

        #endregion

        #region Related to screen.屏幕相关
        Vector3 m_screenHalf;

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

        /// <summary>
        /// Get the mouse position in screen. 获取鼠标在屏幕的坐标位置
        /// </summary>
        /// <param name="center">Start at the center of the screen.以屏幕中央开始</param>
        /// <returns>Current mouse position. 当前鼠标位置</returns>
        public Vector2 GetMousePosInScreen(bool center = true)
        {
            if (center)
                return Input.mousePosition - m_screenHalf;

            return Input.mousePosition;
        }

        /// <summary>
        /// Get the position orientation with screen center.获取对于屏幕中心的位置方向
        /// </summary>
        /// <param name="v2">Current position. 当前坐标</param>
        /// <returns>Position orientation. 位置方向</returns>
        public PositionOrientationType GetOrientationWithScreenCenter(Vector2 v2)
        {
            if (v2.x > m_screenHalf.x || v2.x < -m_screenHalf.x || v2.y > m_screenHalf.y || v2.y < -m_screenHalf.y)
                return PositionOrientationType.None;
            if (v2 == Vector2.zero)
                return PositionOrientationType.Center;

            if (v2.x >= 0.0f && v2.y >= 0.0f)
                return PositionOrientationType.UpperRight;
            if (v2.x >= 0.0f && v2.y < 0.0f)
                return PositionOrientationType.LowRight;
            if (v2.x < 0.0f && v2.y >= 0.0f)
                return PositionOrientationType.UpperLeft;
            if (v2.x < 0.0f && v2.y < 0.0f)
                return PositionOrientationType.LeftLower;

            return PositionOrientationType.None;
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
    /// The screen orientation type.<para>屏幕方向类型</para>
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

    /// <summary>
    /// Position type.<para>位置类型</para>
    /// </summary>
    public enum PositionOrientationType
    {
        /// <summary> 非屏幕坐标 </summary>
        None,
        /// <summary> 中心 </summary>
        Center,
        /// <summary> 左上 </summary>
        UpperLeft,
        /// <summary> 右上 </summary>
        UpperRight,
        /// <summary> 右下 </summary>
        LowRight,
        /// <summary> 左下 </summary>
        LeftLower,
    }
}
