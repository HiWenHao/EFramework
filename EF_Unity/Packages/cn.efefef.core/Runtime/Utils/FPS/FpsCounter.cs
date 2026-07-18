/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-11-14 11:12:28
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-11-14 11:12:28
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework
{
    /// <summary>
    /// FPS 计算中心
    /// </summary>
    public sealed class FpsCounter
    {
        private int _frames;
        private float _timeLeft;
        private float _currentFps;
        private float _accumulator;
        private float _updateInterval;

        /// <summary>
        /// FPS 计算中心
        /// </summary>
        /// <param name="updateInterval">计算间隔</param>
        public FpsCounter(float updateInterval)
        {
            if (updateInterval <= 0f)
            {
                D.Error("Update interval is invalid.");
                return;
            }

            _updateInterval = updateInterval;
            Reset();
        }

        /// <summary>
        /// 计算间隔
        /// </summary>
        public float UpdateInterval
        {
            get
            {
                return _updateInterval;
            }
            set
            {
                if (value <= 0f)
                {
                    D.Error("Update interval is invalid.");
                    return;
                }

                _updateInterval = value;
                Reset();
            }
        }

        /// <summary>
        /// 当前FPS
        /// </summary>
        public float CurrentFps
        {
            get
            {
                return _currentFps;
            }
        }

        public void Update(float realElapseSeconds)
        {
            _frames++;
            _accumulator += realElapseSeconds;
            _timeLeft -= realElapseSeconds;

            if (_timeLeft <= 0f)
            {
                _currentFps = _accumulator > 0f ? _frames / _accumulator : 0f;
                _frames = 0;
                _accumulator = 0f;
                _timeLeft += _updateInterval;
            }
        }

        private void Reset()
        {
            _currentFps = 0f;
            _frames = 0;
            _accumulator = 0f;
            _timeLeft = 0f;
        }
    }
}
