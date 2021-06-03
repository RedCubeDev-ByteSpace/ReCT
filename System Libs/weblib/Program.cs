using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace web
{
    class Program
    {
        static void Main(string[] args)
        {
            // emptyness
        }
    }

    //This is the Official ReCT WEB Package -- ©2021 RedCube
    public static class web
    {
        public static void DownloadFile(string url, string path)
        {
            WebClient myWebClient = new WebClient();
            myWebClient.DownloadFile(url, path);
        }

        public static string UploadFile(string url, string path)
        {
            WebClient myWebClient = new WebClient();
            byte[] responseArray = myWebClient.UploadFile(url, path);
            return System.Text.Encoding.ASCII.GetString(responseArray);
        }

        public class WebRequest
        {
            private HttpClient client;
            private MultipartFormDataContent fields;
            private bool requestType = false;

            public WebRequest(string mode)
            {
                if (mode != "POST" && mode != "GET")
                    throw new Exception("Only 'POST' and 'GET' are Valid request types!");
                
                client = new HttpClient();
                requestType = mode == "POST";
                fields = new MultipartFormDataContent();
            }

            public void AddField(string key, string value)
            {
                fields.Add(new StringContent(value), key);
            }
            
            public HttpResponse SendRequest(string URI)
            {
                return sendRequest(URI).Result;
            }

            private async Task<HttpResponse> sendRequest(string URI)
            {
                if (requestType)
                {
                    var resp = await client.PostAsync(URI, fields);
                    
                    List<string> headers = new List<string>();
                    
                    foreach (var h in resp.Headers)
                        headers.Add(h.Key + ":" + h.Value);

                    return new HttpResponse(headers.ToArray(), (int)resp.StatusCode, resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
                }
                else
                {
                    var resp = await client.GetAsync(URI);
                    
                    List<string> headers = new List<string>();
                    
                    foreach (var h in resp.Headers)
                        headers.Add(h.Key + ":" + h.Value);

                    return new HttpResponse(headers.ToArray(), (int)resp.StatusCode, resp.IsSuccessStatusCode, await resp.Content.ReadAsStringAsync());
                }
            }
        }

        public class HttpResponse
        {
            public string[] Headers;
            public int StatusCode;
            public bool wasSuccessful;
            public string Content;

            public HttpResponse(string[] headers, int statusCode, bool successful, string content)
            {
                Headers = headers;
                StatusCode = statusCode;
                wasSuccessful = successful;
                Content = content;
            }
        }

        public class WebSocketClient
        {
            private ClientWebSocket client;
            private string URL;
            private CancellationTokenSource cancelToken;

            public WebSocketClient(string url)
            {
                client = new ClientWebSocket();
                URL = url;
                cancelToken = new CancellationTokenSource();
            }

            public void Open()
            {
                var task = client.ConnectAsync(new Uri(URL), cancelToken.Token);
                task.Wait();
            }

            public string Receive()
            {
                var task = ReceiveAsync();
                task.Wait();
                return task.Result;
            }

            private async Task<string> ReceiveAsync()
            {
                byte[] buffer = new byte[1024];
                WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return "abort";
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Write(buffer, 0, result.Count);
                    while (!result.EndOfMessage)
                    {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken.Token);
                        stream.Write(buffer, 0, result.Count);
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string message = reader.ReadToEnd();
                        return message;
                    }
                }
            }

            public void Send(string message)
            {
                byte[] byteContentBuffer = Encoding.UTF8.GetBytes(message);
                var task = client.SendAsync(new ArraySegment<byte>(byteContentBuffer), WebSocketMessageType.Text, true, cancelToken.Token);
                task.Wait();
            }

            public void Cancel()
            {
                cancelToken.Cancel(false);
            }
        }

    }
}