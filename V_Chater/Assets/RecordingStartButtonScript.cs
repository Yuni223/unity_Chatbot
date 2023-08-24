using System;
using System.IO;
using UnityEngine;

public class RecordingStartButtonScript : MonoBehaviour
{
    private AudioSource audioSource;
    private const int samplingFrequency = 44100; // 샘플링 레이트
    private const int maxTime_s = 3599; // 최대 녹으미간 60분 미만
    private string micName = null; // 마이크 이름
    public static bool recordedFlg = false; // 녹음 완료 여부에 대한 플래그

    void Start()
    {
            //마이크 디바이스를 찾다
        foreach (string device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
            micName = device;
        }
             // 미리 태그 설정해 둔 Audio source를 취득해 두다
        audioSource = GameObject.FindGameObjectWithTag("AudioSource").GetComponent<AudioSource>();
    }

    void Update()
    {
        // AudioClip 존재 and 녹음 미완성 and 마이크 녹음 중이 아님 → 자동 종료됨
        if (audioSource.clip != null && !recordedFlg && !Microphone.IsRecording(deviceName: micName))
        {
            Debug.Log("녹음 자동 종료！");
            recordedFlg = true;

                    // Wav 파일에 저장
                    // ※ 여기에 경보 표시 처리를 해 주시는 것도 좋다
            DateTime dt = DateTime.Now;
            //string dtStr = dt.ToString("yyyyMMddHHmmss");
            string fileFullPath = Path.Combine(Application.persistentDataPath, "voice.wav");//"audiofile_" + dtStr + ".wav");
            if (!SaveAudioSourceWav.Save(fileFullPath, audioSource.clip))
            {
                Debug.Log("녹음 파일을 저장할 수 없었습니다.");
            }
        }
    }

    /// <summary>
    /// 녹음시작
    /// </summary>
    public void StartRecording()
    {
            // 마이크존재확인 
        if (Microphone.devices.Length == 0)
        {
            Debug.Log("마이크를 찾을수 없습니다");
            return;
        }

         // AudioClip 초기화
        audioSource.clip = null;
        recordedFlg = false;

        // 녹음 시작
        Debug.Log("녹음을 시작합니다"+micName);
        audioSource.clip = Microphone.Start(deviceName: micName, loop: false, lengthSec: maxTime_s, frequency: samplingFrequency);
    }
}
