using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MyMicrophone : MonoBehaviour
{
    public int m_Length = 1;
    public bool m_IsLoop = true;
    Queue<byte> m_wavBuffer;


    UdpSocket socket;

    protected AudioSource m_AudioSource;
    [StringInList(typeof(PropertyDrawersHelper), "AllMicrophones")] public string MicrophoneDevice;

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
            m_AudioSource.clip = Microphone.Start(MicrophoneDevice, true, m_Length, 16000);
            m_AudioSource.loop = m_IsLoop;
        }
        while (!(Microphone.GetPosition(MicrophoneDevice) > 0)) { }
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

public class StringInList : PropertyAttribute
{
    public delegate string[] GetStringList();

    public StringInList(params string[] list)
    {
        List = list;
    }

    public StringInList(Type type, string methodName)
    {
        var method = type.GetMethod(methodName);
        if (method != null)
        {
            List = method.Invoke(null, null) as string[];
        }
        else
        {
            Debug.LogError("NO SUCH METHOD " + methodName + " FOR " + type);
        }
    }

    public string[] List
    {
        get;
        private set;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StringInList))]
public class StringInListDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var stringInList = attribute as StringInList;
        var list = stringInList.List;
        if (property.propertyType == SerializedPropertyType.String)
        {
            int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
            index = EditorGUI.Popup(position, property.displayName, index, list);

            property.stringValue = list[index];
        }
        else if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = EditorGUI.Popup(position, property.displayName, property.intValue, list);
        }
        else
        {
            base.OnGUI(position, property, label);
        }
    }
}
public static class PropertyDrawersHelper
{
    public static string[] AllMicrophones()
    {
        var temp = new List<string>();
        foreach (string s in Microphone.devices)
            temp.Add(s);
        return temp.ToArray();
    }
}
#endif

