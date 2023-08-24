using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;

public class NewBehaviourScript : MonoBehaviour
{
    // TTS 부분을 Google Cloud에서 지원해주는 Google Cloud TTS 서비스를 통해서 TTS를 구현함
    // google cloud에 접속을 하기위한 주소 변수
    private string apiURL = "https://texttospeech.googleapis.com/your_googleapi_token";

    // Google Cloud에서 텍스트 형식과 음성 종류, 속도 등을 알아보기 위해 json 형식으로 바꾸어서 전송해야됨
    // google cloud 에 text를 보내기위해 json 형식으로 바꿔줄 class 선언
    SetTextToSpeech textts = new SetTextToSpeech();

    // 소켓통신을 하기위한 Soket 선언
    Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    // 파이썬에서 받아올 문자열
    private string msg;

    // 유니티에서 음성을 인식하는 음성 소스 선언
    private AudioSource audioSource;

    // 마이크 디바이스 이름
    private string micName = null; 
    
    void Start() // 프로그램이 실행되면 바로 동작하는 함수
    {
        //서버에 연결하기 위한 주소와 포트 번호
        mySocket.Connect("ip number", port_number);
        // 컴퓨터에 연결된 마이크 디바이스를 찾는 방식
        // foreach를 통해 검색된 마이크 디바이스 수만큼 반복을 할수 있게 해준다.
        foreach (string device in Microphone.devices)
        {
            // 마이크 디바이스 이름 저장
            micName = device;
        }
        // 미리 태그 설정해 둔 마이크 디바이스를 Audio source로 설정하여 사용한다. 
        audioSource = GameObject.FindGameObjectWithTag("AudioSource").GetComponent<AudioSource>();
    }

    public void OnClick() // Start Voice 버튼을 클릭 했을 시 실행되는 이벤트
    {
        try
        {
            // 검색할 파일의 이름
            string filename = "voice.wav";
            // 저장된 음성 파일을 소켓 통신을 통해 서버에 전송
            mySocket.SendFile(filename);
            // 음성 파일을 4byte씩 전송
            var data = new Byte[4];
            // 소켓 통신에서 받을 파일 크기를 미리 확인
            mySocket.Receive(data, 4, SocketFlags.None);
            // 배열에 크기만큼 저장
            Array.Reverse(data);
            // 저장된 배열을 Int 형식으로 변경
            data = new byte[BitConverter.ToInt32(data, 0)];
            // 받아야될 파일을 본격적으로 받기 시작
            mySocket.Receive(data, data.Length, SocketFlags.None);
            // UTF8 형식으로 데이터 전환
            msg = System.Text.Encoding.UTF8.GetString(data);
            Init();
            CreateAudio();
        }
        catch (Exception e) // 오류 발생시 오류 메세지 출력
        {
            Debug.Log(e);
            mySocket.Close();
        }
    }

    public void EndRecording()
    {
        if (!Microphone.IsRecording(deviceName: micName))
        {
            Debug.Log("녹음이 시작되지 않았습니다");
            return;
        }

        Debug.Log("녹음을 정지합니다");

        // 마이크 녹음 위치 취득
        int position = Microphone.GetPosition(micName);

        // 녹음을 정지
        Microphone.End(deviceName: micName);
        RecordingStartButtonScript.recordedFlg = true;

        // 음성 데이터 일시 대피용 영역을 확보하고, audioClip으로부터의 데이터를 저장
        float[] soundData = new float[audioSource.clip.samples * audioSource.clip.channels];
        audioSource.clip.GetData(soundData, 0);

        // 새로운 음성 데이터 영역을 확보하고, positon 만큼 저장할 수 있는 크기로 한다.
        float[] newData = new float[position * audioSource.clip.channels];

        // position 만큼 데이터 복사
        for (int i = 0; i < newData.Length; i++)
        {
            newData[i] = soundData[i];
        }

        // 새로운 AudioClip 인스턴스를 생성하고 음성 데이터 셋팅
        AudioClip newClip = AudioClip.Create(audioSource.clip.name, position, audioSource.clip.channels, audioSource.clip.frequency, false);
        newClip.SetData(newData, 0);
        AudioClip.Destroy(audioSource.clip);
        audioSource.clip = newClip;

        // Wav 파일에 저장
        DateTime dt = DateTime.Now;
        string fileFullPath = Path.Combine("C:/Users/yun/Unity/V_Chater", "voice.wav");//Application.persistentDataPath
        if (!SaveAudioSourceWav.Save(fileFullPath, audioSource.clip))
        {
            Debug.Log("녹음 파일을 저장할 수 없었습니다.");
        }
    }

    void Init() // 문자열을 JSON 형식 나열하여 저장
    {
        SetInput si = new SetInput();
        si.text = msg;
        textts.input = si;

        SetVoice sv = new SetVoice();
        sv.languageCode = "ko-KR";
        sv.name = "ko-KR-Standard-B";
        sv.ssmlGender = "FEMALE";
        textts.voice = sv;

        SetAudioConfig sa = new SetAudioConfig();
        sa.audioEncoding = "LINEAR16";
        sa.speakingRate = 1;
        sa.pitch = 0;
        sa.volumeGainDb = 0;
        textts.audioConfig = sa;
    }

    private void CreateAudio()
    {
        // google cloud에 요청
        var str = TextToSpeechPost(textts);
        // Google Cloud에 Json 형식으로 파일 데이터를 전송함
        GetContent info = JsonUtility.FromJson<GetContent>(str);
        // Google Cloud에서 보낸 문자열을 받음
        var bytes = Convert.FromBase64String(info.audioContent);
        //google 에서 받은 문자열을 음성 출력을 위해 float[] 형식으로 변환
        var f = ConverByteToFloat(bytes);
        //오디오 클립 생성
        AudioClip audioClip = AudioClip.Create("audioContent", f.Length, 1, 44100, false);
        audioClip.SetData(f, 0);
        //오디오 재생
        AudioSource audioSource = FindObjectOfType<AudioSource>();
        audioSource.PlayOneShot(audioClip);
    }

    // google cloud에서 보내준 문자열을 음성 출력을 하기위해 float[] 형식으로 전환하는 과정
    private static float[] ConverByteToFloat(byte[] array)
    {
        float[] floatArr = new float[array.Length / 2];

        for (int i = 0; i < floatArr.Length; i++)
        {
            floatArr[i] = BitConverter.ToInt16(array, i * 2) / 32768f;
        }
        return floatArr;
    }
    public string TextToSpeechPost(object data) // google cloud에 요청
    {
        // 이때까지 선언한 텍스트를 Json 파일 형식으로 바꾸어줌
        string str = JsonUtility.ToJson(data);
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);

        // 미리 선언해둔 주소를 통해 google cloud에 접속
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiURL);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.ContentLength = bytes.Length;

        //google cloud에 전송
        try
        {
            using (var stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
                stream.Close();
            }
            //google cloud에 보낸 데이터를 받음
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            string json = reader.ReadToEnd();

            return json;
        }
        catch (WebException e)
        {
            Debug.Log(e);
            return null;
        }
    }
}
[System.Serializable]
public class SetTextToSpeech
{
    // Google Cloud에 보낼 클래스 변수들을 각각 따로 선언하여 준다.
    public SetInput input;
    public SetVoice voice;
    public SetAudioConfig audioConfig;
}

[System.Serializable]
public class SetInput
{
    // Google Cloud에 보낼 text 변수 선언
    public string text;
}

[System.Serializable]
public class SetVoice
{
    //Google Cloud에 보낼 언어 종류, 목소리 종류(어른, 아이 등등), 목소리의 성별 변수 선언
    public string languageCode;
    public string name;
    public string ssmlGender;
}

[System.Serializable]
public class SetAudioConfig
{
    // Google Cloud에 보낼 오디오 형식, 말하는 속도, 음성 높낮이, 목소리 크기 변수 선언
    public string audioEncoding;
    public float speakingRate;
    public int pitch;
    public int volumeGainDb;
}

[System.Serializable]
public class GetContent
{
    // 유니티에서 오디오를 사용하기 위한 변수 선언
    public string audioContent;
}