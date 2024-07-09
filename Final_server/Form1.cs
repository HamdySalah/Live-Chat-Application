using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Final_server
{
    public partial class Form1 : Form
    {
        TcpListener server = null;
        TcpClient client = null;
        NetworkStream stream = null;
        //Thread listenerThread = null;
        public Form1()
        {
            InitializeComponent();
        }

        private async void Connect_Click(object sender, EventArgs e)
        {
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            UpdateTextBox("Server is waiting for client to connect");

            client = await server.AcceptTcpClientAsync();
            UpdateTextBox("Client connected successfully");
            stream = client.GetStream();

            while (true)
            {
                try
                {

                    byte[] buffer = new byte[1024];
                    int byteRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string message  = Encoding.ASCII.GetString(buffer  , 0 , byteRead);

                    if (message.StartsWith("DIRPATH:"))
                    {
                        
                        string dirpath = message.Substring(8);
                        if (Directory.Exists(dirpath))
                        {
                            string zippath = Path.Combine(Path.GetTempPath(), "compressed_directory.zip");
                            CompressDirectory(dirpath, zippath);
                            byte[] zipbyte = File.ReadAllBytes(zippath);
                            SendMessage("DIR: " + zipbyte.Length);

                            await stream.WriteAsync(zipbyte , 0, zipbyte.Length);

                        }
                        else
                        {
                            SendMessage("Directory not found");
                        }

                        //string dirpath = message.Substring(8);
                        //if (Directory.Exists(dirpath))
                        //{
                        //    string[] files = Directory.GetFiles(dirpath);
                        //    string[] directories = Directory.GetDirectories(dirpath);

                        //    string fileNames = "Files:\n" + Environment.NewLine + string.Join(Environment.NewLine, Array.ConvertAll(files, Path.GetFileName)) + Environment.NewLine;
                        //    string directoryNames = $"Directories:{Environment.NewLine}" + string.Join(Environment.NewLine, Array.ConvertAll(directories, Path.GetFileName)) + Environment.NewLine;

                        //    string content = fileNames + "\n" + directoryNames;
                        //    await SendMessage("DIR:" + content);

                        //    textBox2.Invoke((MethodInvoker)delegate {
                        //        textBox2.Text = $"Server Msg : Server is Sending:  {dirpath}  Information to Client";
                        //    });
                        //}
                        //else
                        //{
                        //    await SendMessage("Directory not found");
                        //}
                    }
                    else if (message.StartsWith("FILEPATH:"))
                    {
                        string filepath = message.Substring(9);
                        if (File.Exists(filepath))
                        {
                            string fileContent = File.ReadAllText(filepath);
                            SendMessage("FILE:" + fileContent);
                        }
                        else
                        {
                            SendMessage("File not found");
                        }
                    }
                    else if (message.StartsWith("IMAGEPATH:"))
                    {
                        string imagePath = message.Substring(10);
                        if (File.Exists(imagePath))
                        {
                            byte[] imageBytes = File.ReadAllBytes(imagePath);
                            SendImage(imageBytes);
                        }
                        else
                        {
                            SendMessage("Image not found");
                        }
                    }
                    {
                        UpdateTextBox($"Client Msg: {message}");
                    }
                }
                catch (Exception ex)
                {
                    UpdateTextBox("Error: " + ex.Message);
                }
            }
        }
        private void CompressDirectory(string sourceDir, string zipFilePath)
        {
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            ZipFile.CreateFromDirectory(sourceDir, zipFilePath);
        }

        private void SendMessage(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }
        //private async Task SendMessage(string message)
        //{
        //    byte[] buffer = Encoding.ASCII.GetBytes(message);
        //    await stream.WriteAsync(buffer, 0, buffer.Length);
        //}

        private void UpdateTextBox(string message)
        {
            textBox2.Invoke((MethodInvoker)delegate {
                textBox2.AppendText(message + Environment.NewLine);
            });
        }

        // send Image function
        private void SendImage(byte[] imageBytes)
        {
            string header = "IMAGE:" + imageBytes.Length.ToString();
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(imageBytes, 0, imageBytes.Length);
        }

        private void Send_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;
            SendMessage(message);
        }


        // function to compression dir
        private void Close_Click(object sender, EventArgs e)
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
            if (server != null)
                server.Stop();
            UpdateTextBox("Connection closed from Server");
        }
    }
}
