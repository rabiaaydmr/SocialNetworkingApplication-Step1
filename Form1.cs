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

namespace server
{
    public partial class Form1 : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        List<Socket> clientSockets = new List<Socket>();

        bool terminating = false;
        bool listening = false;

        String[] names = new String[300];
        List<String> names_include = new List<string>();

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();


            //  Reading People Name to Check
            int counter = 0;
            string line;

            System.IO.StreamReader file = new System.IO.StreamReader("user_db.txt");
            while ((line = file.ReadLine()) != null)
            {
                names[counter] = line;
                counter++;
            }
            file.Close();
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            if (Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);

                serverSocket.Listen(300);       //300 since we are going to listen 300 items.

                listening = true;
                button_listen.Enabled = false;
                textBox_message.Enabled = true;
                button_send.Enabled = true;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();

                    Byte[] our_name = new Byte[64];     //Names length is 64 Bytes.
                    newClient.Receive(our_name);        //Name is received.
                    string new_name = Encoding.Default.GetString(our_name);
                    new_name = new_name.Substring(0, new_name.IndexOf("\0"));


                    if (names.Contains(new_name) == true && names_include.Contains(new_name) != true)    //Checking if the name is in the database or not.
                    {
                        names_include.Add(new_name);

                        clientSockets.Add(newClient);       //Adding the client to socket.
                        logs.AppendText("Client : " + new_name + " is connected.");
                        logs.AppendText(" \n");
                        Thread our_thread = new Thread(Receive);
                        our_thread.Start();
                        string special_approval = "approved";
                        Byte[] our_buffer = Encoding.Default.GetBytes(special_approval);
                        newClient.Send(our_buffer);
                    }
                    else    //Name is not in the database.
                    {

                        string special_approval = "not_approved";
                        if (names_include.Contains(new_name) == true)
                        {
                            special_approval = "same_name";
                        }
                        Byte[] our_buffer = Encoding.Default.GetBytes(special_approval);
                        newClient.Send(our_buffer);
                        newClient.Close();
                        logs.AppendText("A client tried to connect with invalid name.\n");
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void Receive()
        {
            Socket thisClient = clientSockets[clientSockets.Count() - 1];
            bool connected = true;

            string name_about_to = "";

            while (connected && !terminating)
            {
                try
                {
                    Byte[] buffer = new Byte[64];
                    thisClient.Receive(buffer);

                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

                    int index_of = incomingMessage.IndexOf(":");
                    name_about_to = incomingMessage.Substring(0, index_of);

                    clientSockets.Remove(thisClient);       //We are removing this client so, he/she does not send message to himslef/herself.
                    if (clientSockets.Count() > 0)      //If we have more than 1 client we can send a message.
                    {
                        foreach (Socket client in clientSockets)      //We are sending to message to each client one by one except this sender.
                        {
                            try
                            {
                                logs.AppendText("Client: " + incomingMessage + "\n");
                                client.Send(buffer);
                            }
                            catch
                            {
                                logs.AppendText("Problem Occured with the connection. \n");
                                terminating = true;
                                textBox_message.Enabled = false;
                                button_send.Enabled = false;
                                textBox_port.Enabled = true;
                                button_listen.Enabled = true;
                                serverSocket.Close();

                            }

                        }

                    }
                    else
                    {
                        logs.AppendText("There is only one client to send. To send this message add more clients. \n");
                    }
                    clientSockets.Add(thisClient);
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("A client has disconnected\n");
                    }

                    if (name_about_to != "")
                    {
                        if (names_include.Contains(name_about_to) == true)
                        {
                            names_include.Remove(name_about_to);
                        }
                    }

                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;
                }
            }
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string message = "Server: "+textBox_message.Text;
            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);

                foreach (Socket client in clientSockets)
                {
                    try
                    {
                        client.Send(buffer);
                    }
                    catch
                    {
                        logs.AppendText("There is a problem! Check the connection...\n");
                        terminating = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        textBox_port.Enabled = true;
                        button_listen.Enabled = true;
                        serverSocket.Close();
                    }

                }
            }
        }
    }
}
