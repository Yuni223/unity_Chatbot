using System;
using System.IO;
using UnityEngine;

public static class SaveAudioSourceWav {

    const int HEADER_SIZE = 44;

    /// <summary>
    /// AudioClip을 Wav 파일에 저장하다
    /// </summary>
    /// <param name="fileFullPath">저장 파일 풀 패스</param>
    /// <param name="clip">저장할 AudioClip</param>
      /// <returns>true: 보존 성공 | false: 보존 실패</returns>
    public static bool Save(string fileFullPath, AudioClip clip)
    {
        if (!fileFullPath.ToLower().EndsWith(".wav"))
        {
            fileFullPath += ".wav";
        }

        //Debug.Log("저장 파일 풀 패스: " + fileFullPath);

            // 보존처 디렉토리가 없는 경우 작성한다
        Directory.CreateDirectory(Path.GetDirectoryName(fileFullPath));

        using (var fileStream = CreateEmpty(fileFullPath))
        {
            ConvertAndWrite(fileStream, clip);
            WriteHeader(fileStream, clip);
        }

        return true;
    }

    /// <summary>
    /// 빈 파일을 작성하다
    /// </summary>
    /// <param name="filepath">저장처 풀패스</param>
    /// <returns>저장시 FileStream</returns>
    static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        // Header 기입
        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="fileStream"></param>
    /// <param name="clip"></param>
    static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        // 변환은 이하의 2단계
        // ・float[] → Int16[]
        // ・Int16[] → Byte[]
        Int16[] intData = new Int16[samples.Length];

        // Int16 변환의 float은 2Byte이므로 Byte 배열에는 2배로 한다.
        Byte[] bytesData = new Byte[samples.Length * 2];

        // float → Int16로 변환용
        float rescaleFactor = 32767;
        for (int i = 0; i < samples.Length; i++) {
            intData[i] = (short) (samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    /// <summary>
    /// Header에 기입
    /// </summary>
    /// <param name="fileStream">저장할 FileStream</param>
    /// <param name="clip">저장할 AudioClip</param>
    static void WriteHeader(FileStream fileStream, AudioClip clip) {

        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort) (channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        fileStream.Close();
    }
}