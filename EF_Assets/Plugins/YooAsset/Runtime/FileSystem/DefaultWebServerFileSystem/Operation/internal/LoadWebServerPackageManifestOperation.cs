﻿
namespace YooAsset
{
    internal class LoadWebServerPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly DefaultWebServerFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly string _packageHash;
        private readonly int _timeout;
        private UnityWebDataRequestOperation _webDataRequestOp;
        private DeserializeManifestOperation _deserializer;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { private set; get; }


        internal LoadWebServerPackageManifestOperation(DefaultWebServerFileSystem fileSystem, string packageVersion, string packageHash, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _packageHash = packageHash;
            _timeout = timeout;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestFileData)
            {
                if (_webDataRequestOp == null)
                {
                    string filePath = _fileSystem.GetWebPackageManifestFilePath(_packageVersion);
                    string url = DownloadSystemHelper.ConvertToWWWPath(filePath);
                    _webDataRequestOp = new UnityWebDataRequestOperation(url, _timeout);
                    _webDataRequestOp.StartOperation();
                    AddChildOperation(_webDataRequestOp);
                }

                _webDataRequestOp.UpdateOperation();
                if (_webDataRequestOp.IsDone == false)
                    return;

                if (_webDataRequestOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.VerifyFileData;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _webDataRequestOp.Error;
                }
            }

            if (_steps == ESteps.VerifyFileData)
            {
                if (ManifestTools.VerifyManifestData(_webDataRequestOp.Result, _packageHash))
                {
                    _steps = ESteps.LoadManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Failed to verify web server package manifest file!";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializer == null)
                {
                    _deserializer = new DeserializeManifestOperation(_fileSystem.ManifestServices, _webDataRequestOp.Result);
                    _deserializer.StartOperation();
                    AddChildOperation(_deserializer);
                }

                _deserializer.UpdateOperation();
                Progress = _deserializer.Progress;
                if (_deserializer.IsDone == false)
                    return;

                if (_deserializer.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    Manifest = _deserializer.Manifest;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _deserializer.Error;
                }
            }
        }
        internal override string InternalGetDesc()
        {
            return $"PackageVersion : {_packageVersion} PackageHash : {_packageHash}";
        }
    }
}