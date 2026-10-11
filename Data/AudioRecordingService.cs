using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudioLib;
using Common;
namespace Data {
    /// <summary>
    /// Handles audio recording operations.
    /// EXTRACTED FROM LogProcessing to reduce complexity.
    /// </summary>
    public class AudioRecordingService {
        private readonly Record1 _recorder;
        public event Action<string, string> OnAudioStatus;

        public AudioRecordingService() {
            _recorder = new Record1(inputDeviceNumber_: 0, bitRate_: 8000);
            _recorder.SetCallBack(new Record1.CallBackDelegate(HandleAudioCallback));
        }

        private void HandleAudioCallback(string item, string value) {
            value = value.Replace('\0', ' ').Trim();
            OnAudioStatus?.Invoke(item, value);
        }

        public void StartRecording(string callSign, string band, string mode) {
            string fileName = $"{callSign}_Rec_{band}_{mode}_{Util.GetUTC_YYYYMMDD_HHMM()}Z.MP3";
            _recorder.RecordStart(@"C:\temp", fileName);
        }

        public void StopRecording() {
            _recorder.StopRecord();
        }
    }
}
