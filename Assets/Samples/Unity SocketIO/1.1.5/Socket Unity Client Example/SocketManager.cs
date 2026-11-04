using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

using Debug = System.Diagnostics.Debug;

public class SocketManager : MonoBehaviour
{
    public SocketIOUnity socket;

    public InputField EventNameTxt;
    public InputField DataTxt;
    public Text ReceivedText;

    public GameObject objectToSpin;

    public string host = "localhost";
    public string port = "3000";

    // Variable para controlar la distancia de movimiento por cada clic
    public float moveDistance = 1f;

    void Start()
    {
        var uri = new Uri($"http://{host}:{port}");
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new Dictionary<string, string>
                {
                    {"token", "UNITY" }
                }
            ,
            EIO = EngineIO.V4
            ,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        socket.OnConnected += (sender, e) =>
        {
            Debug.Print("socket.OnConnected");
        };
        socket.OnPing += (sender, e) =>
        {
            Debug.Print("Ping");
        };
        socket.OnPong += (sender, e) =>
        {
            Debug.Print("Pong: " + e.TotalMilliseconds);
        };
        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Print("disconnect: " + e);
        };
        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.Print($"{DateTime.Now} Reconnecting: attempt = {e}");
        };

        Debug.Print("Connecting...");
        socket.Connect();

        socket.OnUnityThread("spin", (data) =>
        {
            rotateAngle = 0;
        });

        // Escucha confirmaciones de movimiento del servidor
        socket.OnUnityThread("move", (data) =>
        {
            var x = data.GetValue<JToken>()["x"];
            var y = data.GetValue<JToken>()["y"];
            UnityEngine.Debug.Log($"Confirmación del servidor - X: {x}, Y: {y}");
        });

        ReceivedText.text = "";
        socket.OnAnyInUnityThread((name, response) =>
        {
            ReceivedText.text += "Received On " + name + " : " + response.GetValue().GetRawText() + "\n";
        });
    }

    async void OnApplicationQuit()
    {
        if (socket != null && socket.Connected)
        {
            await socket.DisconnectAsync();
        }
        socket?.Dispose();
    }

    public void MoveUp()
    {
        ApplyMovement(new Vector3(0, moveDistance, 0));
    }

    public void MoveDown()
    {
        ApplyMovement(new Vector3(0, -moveDistance, 0));
    }

    public void MoveLeft()
    {
        ApplyMovement(new Vector3(-moveDistance, 0, 0));
    }

    public void MoveRight()
    {
        ApplyMovement(new Vector3(moveDistance, 0, 0));
    }

    private void ApplyMovement(Vector3 direction)
    {
        if (objectToSpin != null)
        {
            objectToSpin.transform.position += direction;
            Vector3 pos = objectToSpin.transform.position;

            // Impresión en la consola de Unity
            UnityEngine.Debug.Log($"Movimiento - X: {pos.x}, Y: {pos.y}");

            // Envío de coordenadas al servidor
            var data = new { x = pos.x, y = pos.y };
            socket.Emit("move", data);
        }
    }

    public void EmitTest()
    {
        string eventName = EventNameTxt.text.Trim().Length < 1 ? "hello" : EventNameTxt.text;
        string txt = DataTxt.text;
        if (!IsJSON(txt))
        {
            socket.Emit(eventName, txt);
        }
        else
        {
            socket.EmitStringAsJSON(eventName, txt);
        }
    }

    public static bool IsJSON(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) { return false; }
        str = str.Trim();
        if ((str.StartsWith("{") && str.EndsWith("}")) ||
            (str.StartsWith("[") && str.EndsWith("]")))
        {
            try
            {
                var obj = JToken.Parse(str);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void EmitSpin()
    {
        socket.Emit("spin");
    }

    public void EmitClass()
    {
        TestClass testClass = new TestClass(new string[] { "foo", "bar", "baz", "qux" });
        TestClass2 testClass2 = new TestClass2("lorem ipsum");
        socket.Emit("class", testClass2);
    }

    [System.Serializable]
    class TestClass
    {
        public string[] arr;

        public TestClass(string[] arr)
        {
            this.arr = arr;
        }
    }

    [System.Serializable]
    class TestClass2
    {
        public string text;

        public TestClass2(string text)
        {
            this.text = text;
        }
    }

    float rotateAngle = 45;
    readonly float MaxRotateAngle = 45;

    void Update()
    {
        if (rotateAngle < MaxRotateAngle)
        {
            rotateAngle++;
            objectToSpin.transform.Rotate(0, 1, 0);
        }
    }
}