using System;
using System.IO;
using System.Net.Sockets;

namespace net
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Net Package Source Code! Make yourself at home :D");
        }
    }

    //This is the Official ReCT NET Package -- ©2021 RedCube
    public static class net
    {
        public static TCPClient ConnectTCPClient(string ip, int port)
        {
            return new TCPClient(ip, port);
        }

        public static TCPListener ListenOnTCPPort(int port)
        {
            return new TCPListener(port);
        }

        public static TCPSocket OpenSocket(TCPListener listener)
        {
            return listener.OpenSocket();
        }

        public class TCPClient
        {
            private TcpClient client;
            private StreamWriter sw;
            private StreamReader sr;
            private NetworkStream ns;

            public TCPClient(string ip, int port)
            {
                client = new TcpClient(ip, port);
                ns = client.GetStream();
                sw = new StreamWriter(ns);
                sr = new StreamReader(ns);
            }

            public string Read()
            {
                return ((Char)sr.Read()).ToString();
            }

            public string ReadLine()
            {
                return sr.ReadLine();
            }

            public void Write(string s)
            {
                sw.Write(s);
            }

            public void WriteLine(string s)
            {
                sw.WriteLine(s);
            }

            public void WriteFlush(string s)
            {
                sw.WriteLine(s);
                sw.Flush();
            }

            public void Flush()
            {
                sw.Flush();
            }

			public void setReadTimeout(int timeout)
			{
				ns.ReadTimeout = timeout;
			}
        }

        public class TCPListener
        {
            private TcpListener listener;

            public TCPListener(int port)
            {
                listener = new TcpListener(port);
                listener.Start();
            }

            public TCPSocket OpenSocket()
            {
                return new TCPSocket(listener.AcceptSocket());
            }
        }

        public class TCPSocket
        {
            private Socket socket;
            private StreamWriter sw;
            private StreamReader sr;
            private NetworkStream ns;

            public TCPSocket(object internalTCPData)
            {
                socket = (Socket)internalTCPData;
                ns = new NetworkStream(socket);
                sw = new StreamWriter(ns);
                sr = new StreamReader(ns);
            }

            public string Read()
            {
                return ((Char)sr.Read()).ToString();
            }

            public string ReadLine()
            {
                return sr.ReadLine();
            }

            public void Write(string s)
            {
                sw.Write(s);
            }

            public void WriteLine(string s)
            {
                sw.WriteLine(s);
            }

            public void WriteFlush(string s)
            {
                sw.WriteLine(s);
                sw.Flush();
            }

            public void Flush()
            {
                sw.Flush();
            }

			public void setReadTimeout(int timeout)
			{
				ns.ReadTimeout = timeout;
			}
        }
    }
}
