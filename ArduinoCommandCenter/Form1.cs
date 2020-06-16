using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArduinoCommandCenter
{
    delegate void message_safe(string str);
    public partial class Form1 : Form
    {
        public static Queue<string> newCommands = new Queue<string>();
        int clientnum = 0;
        List<Queue<string>> executableCommands;
        void Log_Append_Safe(string str)
        {
            if (richTextBox_Log.InvokeRequired)
            {
                this.Invoke(new message_safe(Log_Append_Safe), new object[] { str });
            }
            else
            {
                TBot.newMessages.Enqueue(str);
                richTextBox_Log.AppendText(str + "\r\n");
            }
        }
        public Form1()
        {
            InitializeComponent();
        }
        TcpListener listener;

        private void Form1_Load(object sender, EventArgs e)
        {
            executableCommands = new List<Queue<string>>();
            for (int i = 0; i < 100; i++)
            {
                executableCommands.Add(null);
            }
            Task.Run(() => { StartListening(); });
        }
        private void StartListening()
        {
            listener = new TcpListener(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 8008));
            listener.Start();
            while (true)
            {
                try
                {
                    if (Form1.newCommands.Count > 0)
                    {
                        var command = Form1.newCommands.Dequeue();
                        for (int i = 0; i < executableCommands.Count; i++)
                        {
                            if (executableCommands[i] != null)
                                executableCommands[i].Enqueue(command);
                        }
                    }
                    if (listener.Pending())
                    {
                        new Thread(ClientThread).Start(listener.AcceptTcpClient());
                    }
                    Thread.Sleep(100);
                }
                catch
                {
                    Log_Append_Safe("something happend at listener loop!");
                }
            }
        }
        private void ClientThread(object client)
        {
            int mynum = -1;
            for (int i = 0; i < executableCommands.Count; i++)
            {
                if (executableCommands[i] == null)
                {
                    mynum = i;
                }
            }
            executableCommands[mynum] = new Queue<string>();

            var tcpclient = client as TcpClient;
            var ip = ((IPEndPoint)tcpclient.Client.RemoteEndPoint).Address.ToString();
            Log_Append_Safe(ip);
            var stream = tcpclient.GetStream();
            var currentcommand = "";
            byte[] buffer = new byte[1024];
            while (tcpclient.Connected)
            {
                try
                {
                    while (stream.DataAvailable)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        currentcommand += Encoding.ASCII.GetString(buffer, 0, read);
                    }
                    var splitted = currentcommand.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    currentcommand = "";
                    foreach (var command in splitted)
                    {
                        var res = ProcessCommand(ip, command);
                        if (!res)
                        {
                            currentcommand += command;
                        }
                    }
                    if (executableCommands[mynum].Count > 0)
                    {
                        var tosend = executableCommands[mynum].Dequeue();
                        tosend += "\r\n";
                        var bytestosend = Encoding.ASCII.GetBytes(tosend);
                        stream.Write(bytestosend, 0, bytestosend.Length);
                    }
                    Thread.Sleep(100);
                }
                catch
                {

                }
            }
            executableCommands[mynum] = null;
        }
        private bool ProcessCommand(string ip, string command)
        {
            if (command.Last() == '\r')
            {
                if (command == "keepalive\r")
                {
                    //ignore
                    //Log_Append_Safe(ip + ":" + command);
                }
                else if (command.Contains(":OK\r"))
                {
                    //send back to telegram
                    Log_Append_Safe(ip + ":" + command);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1.newCommands.Enqueue(textBox_CustomCommand.Text);
            textBox_CustomCommand.Text = "";
        }
    }
}
