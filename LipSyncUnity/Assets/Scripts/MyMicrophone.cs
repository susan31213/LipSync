using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MyMicrophone : MonoBehaviour
{
    public int m_Length = 1;
    public bool m_IsLoop = true;
    Queue<byte> m_wavBuffer;

    UdpSocket socket;

    protected AudioSource m_AudioSource;
    public Dropdown MicrophoneDeviceDropdown;
    public Button ToggleRecBtn;
    bool recording = false;

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
            List<string> micros = new List<string>();
            foreach (string m in Microphone.devices)
            {
                micros.Add(m);
            }
            MicrophoneDeviceDropdown.AddOptions(micros);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    void StartRecording()
    {
        MicrophoneDeviceDropdown.interactable = false;
        string microphoneName = MicrophoneDeviceDropdown.options[MicrophoneDeviceDropdown.value].text;
        m_AudioSource.clip = Microphone.Start(microphoneName, true, m_Length, 16000);
        m_AudioSource.loop = m_IsLoop;
        while (!(Microphone.GetPosition(microphoneName) > 0)) { }
        m_AudioSource.Play();
        StartCoroutine(GetMicrophoneData());
        StartCoroutine(SendWavDataToServer());
    }

    void StopRecording()
    {
        m_AudioSource.Stop();
        StopCoroutine(GetMicrophoneData());
        StopCoroutine(SendWavDataToServer());
        MicrophoneDeviceDropdown.interactable = true;
    }

    public void ToggleRecord()
    {
        if (recording)
        {
            StopRecording();
            ToggleRecBtn.GetComponentInChildren<Text>().text = "Start Record";
        }
        else
        {
            StartRecording();
            ToggleRecBtn.GetComponentInChildren<Text>().text = "Stop Record";
        }
        recording = !recording;
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

