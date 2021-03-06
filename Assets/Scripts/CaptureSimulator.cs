﻿#define FACE_DETECT

using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System;
using System.Text;

public class CaptureSimulator : MonoBehaviour {
    const int PORT = 8888;
    private Thread mainThread = null;

    public int captureID = 0;
    private WebCamTexture webCamTexture;
    private byte[] imageData;
    private int imageH;
    private int imageW;
    private bool imageDataLock = false;

    void OnApplicationQuit() {
        endServer();
    }

    void Start() {
        webCamTexture = new WebCamTexture();
        webCamTexture.Play();
    }

    void Update() {
        if (mainThread != null) {
            Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height);
            texture.SetPixels(webCamTexture.GetPixels());

            int height = webCamTexture.height;
            int width = webCamTexture.width;
#if FACE_DETECT
            int x = 0;
            int y = 0;
            if (FaceDetection.faceDetect(texture, out x, out y, out height, out width)) {
                Destroy(texture);
                texture = new Texture2D(width, height);
                texture.SetPixels(webCamTexture.GetPixels(x, webCamTexture.height - y - height, width, height));
            }
#endif
            int timeout = 1000;
            while (imageDataLock) {
                Thread.Sleep(1);
                if (--timeout == 0) {
                    imageDataLock = false;
                    break;
                }
            }
            imageData = texture.EncodeToJPG();
            Destroy(texture);
            imageH = height;
            imageW = width;
        }
    }

    void OnGUI() {
        GUI.color = Color.gray;
        GUI.TextArea(new Rect(0, 50, 200, 50), Network.player.ipAddress);
        GUI.color = Color.white;
        if (mainThread == null) {
            if (GUI.Button(new Rect(200, 50, 200, 50), "start capture server")) {
                startServer();
            }
        } else {
            if (GUI.Button(new Rect(200, 50, 200, 50), "end capture server")) {
                endServer();
            }
        }
        GUI.DrawTexture(new Rect(0, 100, 480, 480), webCamTexture);
    }

    private void startServer() {
        imageDataLock = false;
        string ipAddress = Network.player.ipAddress;
        mainThread = new Thread(() => hostServer(ipAddress));
        mainThread.Start();
    }

    private void hostServer(string ipAddress) {
        IPAddress serverIP = IPAddress.Parse(ipAddress);
        TcpListener listener = new TcpListener(serverIP, PORT);

        listener.Start();
        while (mainThread != null) {
            if (listener.Pending()) {
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(() => msgThread(client));
                thread.Start();
            }
            Thread.Sleep(10);
        }
        listener.Stop();
    }

    private void msgThread(TcpClient client) {
        Stream sr = new StreamReader(client.GetStream()).BaseStream;
        Stream sw = new StreamWriter(client.GetStream()).BaseStream;

        while (mainThread != null) {
            if (imageData != null) {
                try {
                    byte[] info = new byte[8];
                    imageDataLock = true;
                    int len = imageData.Length;
                    info[0] = (byte)captureID;
                    info[1] = (byte)((len & 0xff0000) >> 16);
                    info[2] = (byte)((len & 0xff00) >> 8);
                    info[3] = (byte)(len & 0xff);
                    info[4] = (byte)((imageH & 0xff00) >> 8);
                    info[5] = (byte)(imageH & 0xff);
                    info[6] = (byte)((imageW & 0xff00) >> 8);
                    info[7] = (byte)(imageW & 0xff);
                    sw.Write(info, 0, 8);
                    sw.Write(imageData, 0, len);
                    sw.Flush();
                    imageDataLock = false;
                    imageData = null;
                    sr.ReadByte();
                } catch {
                    break;
                }
            }
            Thread.Sleep(50);
        }

        client.Close();
    }

    private void endServer() {
        mainThread = null;
    }
}
