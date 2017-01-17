﻿using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

public class OptitrackSimulator : MonoBehaviour {
    const int PORT = 8520;
    private Thread mainTread = null;

    private void OnApplicationQuit() {
        endServer();
    }

    private void OnGUI() {
        GUI.color = Color.gray;
        GUI.TextArea(new Rect(0, 0, 200, 50), Network.player.ipAddress);
        GUI.color = Color.white;
        if (mainTread == null) {
            if (GUI.Button(new Rect(200, 0, 200, 50), "start optitrack server")) {
                startServer();
            }
        } else {
            if (GUI.Button(new Rect(200, 0, 200, 50), "end optitrack server")) {
                endServer();
            }
        }
    }

    private void startServer() {
        string ipAddress = Network.player.ipAddress;
        mainTread = new Thread(() => hostServer(ipAddress));
        mainTread.Start();
    }

    private void hostServer(string ipAddress) {
        IPAddress serverIP = IPAddress.Parse(ipAddress);
        TcpListener listener = new TcpListener(serverIP, PORT);

        listener.Start();
        while (mainTread != null) {
            if (listener.Pending()) {
                TcpClient client = listener.AcceptTcpClient();
                Thread thread = new Thread(() => msgThread(client));
                thread.Start();
            }
            Thread.Sleep(10);
        }
        listener.Stop();
    }

    private void endServer() {
        mainTread = null;
    }

    private void msgThread(TcpClient client) {
        StreamWriter sw = new StreamWriter(client.GetStream());

        float ry = 0;
        while (mainTread != null) {
            sw.WriteLine("begin");
            sw.WriteLine("rb 0 0 0 1 0 " + ry.ToString() + " 0");
            sw.WriteLine("end");
            sw.Flush();
            Thread.Sleep(10);
            ry += 1.0f;
        }
    }
}
