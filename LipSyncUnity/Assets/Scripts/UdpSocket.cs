/*
Created by Youssef Elashry to allow two-way communication between Python3 and Unity to send and receive strings

Feel free to use this in your individual or commercial projects BUT make sure to reference me as: Two-way communication between Python 3 and Unity (C#) - Y. T. Elashry
It would be appreciated if you send me how you have used this in your projects (e.g. Machine Learning) at youssef.elashry@gmail.com

Use at your own risk
Use under the Apache License 2.0

Modified by: 
Youssef Elashry 12/2020 (replaced obsolete functions and improved further - works with Python as well)
Based on older work by Sandra Fang 2016 - Unity3D to MATLAB UDP communication - [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
*/

using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

public class UdpSocket : MonoBehaviour
{
    [HideInInspector] public bool isTxStarted = false;

    [SerializeField] string IP = "127.0.0.1"; // local host
    [SerializeField] int rxPort = 6000; // port to receive data from Python on
    [SerializeField] int txPort = 6001; // port to send data to Python on

    // Create necessary UdpClient objects
    UdpClient client;
    IPEndPoint remoteEndPoint;
    Thread receiveThread; // Receiving Thread

    Queue<byte> receivedData;
    int headerSize = 16;
    int dataSize = 16;
    int usingImage = 0;
    public RawImage[] ReceieveImages;
    bool switching = false;
    byte[] coordBuffer;
    byte[] lastCoords;


    public void SendData(string message) // Use to send data to Python
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    public void SendByteData(byte[] bData) // Use to send data to Python
    {
        try
        {
            client.Send(bData, bData.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    void Awake()
    {
        // Create remote endpoint (to Matlab) 
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);

        // Create local client
        client = new UdpClient(rxPort);

        // local endpoint define (where messages are received)
        // Create a new thread for reception of incoming messages
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Initialize (seen in comments window)
        print("UDP Comms Initialised");

        receivedData = new Queue<byte>(100000);
        coordBuffer = new byte[16];
        lastCoords = new byte[16];
    }

    void Start()
    {
        StartCoroutine(UpdateTexture());    
    }

    // Receive data, update packets received
    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                //Debug.Log("data len: " + data.Length);
                //string text = Encoding.UTF8.GetString(data);
                //print(">> " + text);
                //ProcessInput(text);
                
                foreach (byte b in data)
                    receivedData.Enqueue(b);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    private void ProcessInput(string input)
    {
        // PROCESS INPUT RECEIVED STRING HERE

        if (!isTxStarted) // First data arrived so tx started
        {
            isTxStarted = true;
        }
    }

    IEnumerator UpdateTexture()
    {
        while (true)
        {
            if (switching)
            {
                bool diff = false;
                for(int i=0; i<coordBuffer.Length; i++)
                {
                    if (coordBuffer[i] != lastCoords[i])
                    {
                        diff = true;
                        switching = false;
                        ReceieveImages[usingImage].gameObject.SetActive(true);
                        break;
                    }
                }
                if (!diff)
                {
                    SendSwitchSignal(usingImage);
                    for(int i=0; i<coordBuffer.Length; i++)
                    {
                        lastCoords[i] = coordBuffer[i];
                    }
                }
            }
            if (receivedData.Count >= dataSize + headerSize)
            {
                for (int i = 0; i < headerSize; i++)
                {
                    coordBuffer[i] = receivedData.Dequeue();
                }

                int x1 = BitConverter.ToInt32(coordBuffer, 0);
                int x2 = BitConverter.ToInt32(coordBuffer, 4);
                int y1 = BitConverter.ToInt32(coordBuffer, 8);
                int y2 = BitConverter.ToInt32(coordBuffer, 12);
                dataSize = (x2 - x1) * (y2 - y1) * 3;

                
                if (x2 - x1 > 0 && y2-y1 > 0)
                {
                    byte[] data = new byte[(x2 - x1) * (y2 - y1) * 3];
                    for (int i = 0; i < dataSize; i++)
                        data[i] = receivedData.Dequeue();
                    Texture2D t2d = new Texture2D(x2 - x1, y2 - y1, TextureFormat.RGB24, false);
                    //t.GetRawTextureData
                    t2d.LoadRawTextureData(data);
                    t2d.Apply();

                    //t2d.LoadRawTextureData(data);
                    RectTransform rt = ReceieveImages[usingImage].GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2((x2 - x1), (y2 - y1));
                    rt.anchoredPosition = new Vector2(x1, -y1);
                    ReceieveImages[usingImage].texture = t2d;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SwitchPerson(int n)
    {
        usingImage = n;
        switching = true;
        SendSwitchSignal(n);
        ReceieveImages[usingImage].gameObject.SetActive(false);
        foreach (RawImage i in ReceieveImages)
            i.transform.parent.gameObject.SetActive(false);
        for (int i = 0; i < coordBuffer.Length; i++)
        {
            lastCoords[i] = coordBuffer[i];
        }
    }

    public void SendSwitchSignal(int i)
    {
        byte[] data = new byte[1];
        data[0] = (byte)i;
        SendByteData(data);
    }

    //Prevent crashes - close clients and threads properly!
    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();

        client.Close();
    }

}