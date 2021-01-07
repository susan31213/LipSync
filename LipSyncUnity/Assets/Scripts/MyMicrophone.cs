using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyMicrophone : MonoBehaviour
{
    public int m_Length = 1;
    public bool m_IsLoop = true;
    Queue<byte> m_wavBuffer;


    UdpSocket socket;

    protected AudioSource m_AudioSource;

    void Start()
    {
        socket = GetComponentInChildren<UdpSocket>();
        m_AudioSource = GetComponent<AudioSource>();
        m_wavBuffer = new Queue<byte>(64000);


        if (Microphone.devices.Length == 0)
        {
            Debug.Log("No Microphone");
        }
        else
        {
            m_AudioSource.clip = Microphone.Start(Microphone.devices[0], true, m_Length, 16000);
            m_AudioSource.loop = m_IsLoop;
        }
        while (!(Microphone.GetPosition(Microphone.devices[0]) > 0)) { }
        m_AudioSource.Play();
        StartCoroutine(GetMicrophoneData());
        StartCoroutine(SendWavDataToServer());

    }

    IEnumerator GetMicrophoneData()
    {
        float fps = 30;
        int bytePreSecond = 32000;
        int sentBytePreSecond = (int)(bytePreSecond / fps);

        while (true)
        {
            /*uint len;
            byte[] clipData = SavWav.GetWavWithoutHeader(m_AudioSource.clip, out len);
            for(int i=44; i<clipData.Length; i++)
                m_wavBuffer.Enqueue(clipData[i]);
            yield return new WaitForSeconds(1.0f);*/
            
            uint len;
            byte[] clipData = SavWav.GetWavWithoutHeader(m_AudioSource.clip, out len);
            for (int i = 44; i < sentBytePreSecond; i++)
            {
                m_wavBuffer.Enqueue(clipData[i]);
            }
            
            yield return new WaitForSeconds(1/fps);
            
        }
    }

    IEnumerator SendWavDataToServer()
    {
        float fps = 5f;
        int bytePreSecond = 32000;
        int sentBytePreSecond = (int)(bytePreSecond/fps);
        int headerSize = 44;

        while(true)
        {
            while (m_wavBuffer.Count > sentBytePreSecond)
            {
                byte[] data = new byte[sentBytePreSecond];
                int len = headerSize;
                while (len < sentBytePreSecond && m_wavBuffer.Count > 0)
                {
                    data[len] = m_wavBuffer.Dequeue();
                    len++;
                }

                SavWav.WriteHeader(data, m_AudioSource.clip, (uint)(len + headerSize), (uint)(len + headerSize));
                socket.SendByteData(data);
            }
            yield return new WaitForSeconds(1 / fps);
        }
    }
}
