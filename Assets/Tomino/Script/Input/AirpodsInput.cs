using System.Collections.Generic;
using Tomino.Model;
using UnityEngine;
using HearXR;

namespace Tomino.Input
{
    public class AirpodsInput : IPlayerInput
    {
        private PlayerAction? _playerAction;
        private Vector3 _lastRotation;
        private float _lastActionTime;
        
        // 动作检测的阈值和时间设置
        private const float HEAD_TILT_THRESHOLD = 15.0f; // 左右倾斜阈值（度）
        private const float HEAD_NOD_THRESHOLD = 15.0f;  // 点头阈值（度）
        private const float MIN_ACTION_INTERVAL = 0.3f;  // 动作间隔时间（秒）
        private const float HEAD_DOWN_DURATION_THRESHOLD = 0.3f; // 低头持续时间阈值（秒）
        private const float HEAD_UP_DURATION_THRESHOLD = 0.5f; // 抬头持续时间阈值（秒）
        
        private const string LOG_TAG = "[AirpodsInput]";
        private bool _isDebugMode = true; // 可以通过 Inspector 设置
        
        // 滤波相关参数
        private Vector3 _filteredAngles = Vector3.zero;
        private const float FILTER_FACTOR = 0.2f; // 滤波系数，值越小滤波效果越强
        private const int SAMPLE_WINDOW_SIZE = 5; // 移动平均窗口大小
        private Queue<Vector3> _anglesSampleWindow;
        private Vector3 _movingAverageAngles;

        // 头部动作检测相关
        private float _headDownStartTime = -1f;
        private float _headUpStartTime = -1f;
        private bool _isHeadDown = false;
        private bool _isHeadUp = false;
        
        public AirpodsInput()
        {
            LogInfo("AirpodsInput constructor called");
            _anglesSampleWindow = new Queue<Vector3>(SAMPLE_WINDOW_SIZE);
            try 
            {
                InitializeHeadphoneMotion();
            }
            catch (System.Exception e)
            {
                LogError($"Failed to initialize HeadphoneMotion: {e.Message}\n{e.StackTrace}");
            }
        }

        private void InitializeHeadphoneMotion()
        {
            LogInfo($"Running on platform: {Application.platform}");
            LogInfo($"Unity version: {Application.unityVersion}");
            
#if UNITY_IOS && !UNITY_EDITOR
            LogInfo("Initializing HeadphoneMotion on iOS...");
#else
            LogWarning("Not running on iOS device - HeadphoneMotion will not be available");
#endif

            HeadphoneMotion.Init();
            
            if (HeadphoneMotion.IsHeadphoneMotionAvailable())
            {
                LogInfo("Headphone motion is available");
                
                // 添加连接状态变化的监听
                HeadphoneMotion.OnHeadphoneConnectionChanged += (connected) =>
                {
                    Debug.Log($"{LOG_TAG} Headphone connection changed: {(connected ? "Connected" : "Disconnected")}");
                };
                
                HeadphoneMotion.OnHeadRotationQuaternion += HandleHeadRotation;
                HeadphoneMotion.OnHeadRotationRaw += (x, y, z, w) =>
                {
                    Debug.Log($"{LOG_TAG} Raw rotation received: ({x:F2}, {y:F2}, {z:F2}, {w:F2})");
                };
                
                HeadphoneMotion.StartTracking();
                
                // 检查是否已连接
                bool isConnected = HeadphoneMotion.AreHeadphonesConnected();
                Debug.Log($"{LOG_TAG} Initial headphone connection status: {(isConnected ? "Connected" : "Disconnected")}");
                
                Debug.Log($"{LOG_TAG} Headphone motion tracking started successfully");
                Debug.Log($"{LOG_TAG} Thresholds - Tilt: {HEAD_TILT_THRESHOLD}°, Nod: {HEAD_NOD_THRESHOLD}°");
            }
            else
            {
                LogError("Headphone motion is not available! Please check if AirPods are connected.");
            }
        }

        private void HandleHeadRotation(Quaternion rotation)
        {
            if (Time.time - _lastActionTime < MIN_ACTION_INTERVAL)
            {
                return;
            }

            Vector3 eulerAngles = rotation.eulerAngles;
            
            // 标准化角度
            Vector3 normalizedAngles = new Vector3(
                NormalizeAngle(eulerAngles.x),
                NormalizeAngle(eulerAngles.y),
                NormalizeAngle(eulerAngles.z)
            );

            // 应用低通滤波
            _filteredAngles = Vector3.Lerp(_filteredAngles, normalizedAngles, FILTER_FACTOR);

            // 应用移动平均滤波
            UpdateMovingAverage(normalizedAngles);

            // 使用滤波后的角度进行动作检测
            Vector3 smoothedAngles = (_filteredAngles + _movingAverageAngles) * 0.5f;

            // 每秒打印一次当前角度数据
            if (Time.frameCount % 60 == 0)
            {
                LogInfo($"Raw Angles - X: {normalizedAngles.x:F1}°, Y: {normalizedAngles.y:F1}°, Z: {normalizedAngles.z:F1}°");
                LogInfo($"Filtered Angles - X: {smoothedAngles.x:F1}°, Y: {smoothedAngles.y:F1}°, Z: {smoothedAngles.z:F1}°");
            }

            // 使用平滑后的角度检测动作
            DetectActions(smoothedAngles);
        }

        private void UpdateMovingAverage(Vector3 newAngles)
        {
            // 更新采样窗口
            _anglesSampleWindow.Enqueue(newAngles);
            if (_anglesSampleWindow.Count > SAMPLE_WINDOW_SIZE)
            {
                _anglesSampleWindow.Dequeue();
            }

            // 计算移动平均
            Vector3 sum = Vector3.zero;
            foreach (var angles in _anglesSampleWindow)
            {
                sum += angles;
            }
            _movingAverageAngles = sum / _anglesSampleWindow.Count;
        }

        private void DetectActions(Vector3 angles)
        {
            // 检测左右倾斜动作
            if (Mathf.Abs(angles.z) > HEAD_TILT_THRESHOLD)
            {
                string direction = angles.z > 0 ? "左" : "右";
                _playerAction = angles.z > 0 ? PlayerAction.MoveLeft : PlayerAction.MoveRight;
                _lastActionTime = Time.time;
                LogInfo($"检测到向{direction}倾斜 ({angles.z:F1}°) - 触发{_playerAction}动作");
            }

            // 检测抬头动作（变形）
            if (angles.x < -HEAD_NOD_THRESHOLD)
            {
                if (!_isHeadUp)
                {
                    _isHeadUp = true;
                    _headUpStartTime = Time.time;
                    LogInfo($"开始检测抬头动作 ({angles.x:F1}°)");
                }
                else if (Time.time - _headUpStartTime >= HEAD_UP_DURATION_THRESHOLD)
                {
                    _playerAction = PlayerAction.Rotate;
                    _lastActionTime = Time.time;
                    LogInfo($"检测到持续抬头 ({angles.x:F1}°) 超过 {HEAD_UP_DURATION_THRESHOLD}秒 - 触发变形动作");
                    _isHeadUp = false;
                }
            }
            else
            {
                _isHeadUp = false;
            }
            
            // 检测持续低头动作（加速）
            if (angles.x > HEAD_NOD_THRESHOLD)
            {
                if (!_isHeadDown)
                {
                    _isHeadDown = true;
                    _headDownStartTime = Time.time;
                    LogInfo($"开始检测低头动作 ({angles.x:F1}°)");
                }
                else if (Time.time - _headDownStartTime >= HEAD_DOWN_DURATION_THRESHOLD)
                {
                    _playerAction = PlayerAction.MoveDown;
                    _lastActionTime = Time.time;
                    LogInfo($"检测到持续低头 ({angles.x:F1}°) 超过 {HEAD_DOWN_DURATION_THRESHOLD}秒 - 触发加速下落动作");
                }
            }
            else
            {
                _isHeadDown = false;
            }

            // 记录角度变化
            if (_playerAction != null)
            {
                Vector3 angleDelta = angles - _lastRotation;
                LogInfo($"角度变化 - ΔX: {angleDelta.x:F1}°, ΔY: {angleDelta.y:F1}°, ΔZ: {angleDelta.z:F1}°");
            }

            _lastRotation = angles;
        }

        private float NormalizeAngle(float angle)
        {
            // 将角度转换到 -180 到 180 度范围
            if (angle > 180)
            {
                angle -= 360;
            }
            return angle;
        }

        public PlayerAction? GetPlayerAction()
        {
            var action = _playerAction;
            if (action != null)
            {
                Debug.Log($"{LOG_TAG} Returning action: {action}");
            }
            _playerAction = null;
            return action;
        }

        public void Update()
        {
            // HeadphoneMotion 插件会通过回调自动更新
        }

        public void Cancel()
        {
            _playerAction = null;
        }

        ~AirpodsInput()
        {
            if (HeadphoneMotion.IsHeadphoneMotionAvailable())
            {
                HeadphoneMotion.OnHeadRotationQuaternion -= HandleHeadRotation;
                HeadphoneMotion.StopTracking();
                Debug.Log($"{LOG_TAG} Headphone motion tracking stopped");
            }
        }

        private void LogInfo(string message)
        {
            if (_isDebugMode)
            {
                Debug.Log($"{LOG_TAG} {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"{LOG_TAG} {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"{LOG_TAG} {message}");
        }
    }
}
