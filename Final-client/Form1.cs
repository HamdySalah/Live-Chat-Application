using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Final_client
{
    public partial class Form1 : Form
    {
        TcpClient client = null;
        NetworkStream stream = null;
        Thread listenerThread = null;
        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            textBox4.Multiline = true;
            textBox4.ScrollBars = ScrollBars.Vertical;
            textBox4.ReadOnly = true;
        }

        

        private async void Connect_Click(object sender, EventArgs e)
        {
            client = new TcpClient("127.0.0.1", 5000);
            stream = client.GetStream();
            UpdateTextBox("Welcome to my server");

            await ListenForMessages();
            //listenerThread = new Thread(new ThreadStart(ListenForMessages));
            //listenerThread.Start();
        }

        private async Task ListenForMessages()
        {
            byte[] buffer = new byte[1024];

            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (message.StartsWith("FILE:"))
                    {
                        string fileContent = message.Substring(5);
                        SaveFile(fileContent);
                    }
                    else if (message.StartsWith("DIR:"))
                    {
                        int zipSize = int.Parse(message.Substring(4));
                        byte[] zipBytes = new byte[zipSize];
                        int totalBytesRead = 0;
                        while (totalBytesRead < zipSize)
                        {
                            int read = await stream.ReadAsync(zipBytes, totalBytesRead, zipSize - totalBytesRead);
                            totalBytesRead += read;
                        }
                        SaveCompressedDirectory(zipBytes);
                        //else if (message.StartsWith("DIR:"))
                        //{
                        //    int zipSize = int.Parse(message.Substring(4));
                        //    byte[] zipBytes = new byte[zipSize];
                        //    int totalBytesRead = 0;
                        //    while (totalBytesRead < zipSize)
                        //    {
                        //        int read = await stream.ReadAsync(zipBytes, totalBytesRead, zipSize - totalBytesRead);
                        //        totalBytesRead += read;
                        //    }
                        //    string zipFilePath = SaveCompressedDirectory(zipBytes);
                        //    string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "decompressed_directory");
                        //    DecompressAndPrintDirectory(zipFilePath, outputDirectory);
                        //}

                    }
                    else if (message.StartsWith("IMAGE:"))
                    {
                        int imageSize = int.Parse(message.Substring(6));
                        byte[] imageBytes = new byte[imageSize];
                        int totalBytesRead = 0;
                        while (totalBytesRead < imageSize)
                        {
                            int read = await stream.ReadAsync(imageBytes, totalBytesRead, imageSize - totalBytesRead);
                            totalBytesRead += read;
                        }
                        DisplayImage(imageBytes);
                        SaveImage(imageBytes);

                    }
                    else
                    {
                        UpdateTextBox($"Server Msg: {message}");
                    }
                }
                catch (Exception ex)
                {
                    UpdateTextBox("Error: " + ex.Message);
                }
            }
        }

        private void SaveFile(string fileContent)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "received_file.txt");

                // Write the content to the file
                File.WriteAllText(filePath, fileContent);

                UpdateTextBox("File Content :" + Environment.NewLine + fileContent + Environment.NewLine + "File saved successfully at " + filePath + Environment.NewLine);
            }
            catch (Exception ex)
            {
                UpdateTextBox("Error saving file: " + ex.Message);
            }
        }
        // save Image
        private void SaveImage(byte[] fileContent)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "received_image.jpg");

                using (MemoryStream ms = new MemoryStream(fileContent))
                {
                    using (Image img = Image.FromStream(ms))
                    {
                        img.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }

                UpdateTextBox("Image saved successfully at " + filePath + Environment.NewLine);
            }
            catch (Exception ex)
            {
                UpdateTextBox("Error saving image: " + ex.Message);
            }
        }

        private void DisplayImage(byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Image img = Image.FromStream(ms);
                pictureBox1.Invoke((MethodInvoker)delegate {
                    pictureBox1.Image = img;
                });
            }
        }
        private void DecompressAndPrintDirectory(string zipFilePath, string outputDirectory)
        {

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                UpdateTextBox("please specify a valid output directory");
                return;
            }

            try
            {
                if (File.Exists(zipFilePath))
                {
                    if (Directory.Exists(outputDirectory))
                    {
                        Directory.Delete(outputDirectory, true);
                    }

                    ZipFile.ExtractToDirectory(zipFilePath, outputDirectory);
                    UpdateTextBox("File decompressed successfully to " + outputDirectory);
                    PrintDirectoryContents(outputDirectory);
                }
                else
                {
                    UpdateTextBox("Compressed file not found: " + zipFilePath);
                }
            }
            catch (Exception ex)
            {
                UpdateTextBox("Error decompressing file: " + ex.Message);
            }
        }
        private void PrintDirectoryContents(string directoryPath)
        {
            try
            {
                var directories = Directory.GetDirectories(directoryPath);
                var files = Directory.GetFiles(directoryPath);

                UpdateTextBox("Directories:");
                foreach (var dir in directories)
                {
                    UpdateTextBox(dir);
                }

                UpdateTextBox("Files:");
                foreach (var file in files)
                {
                    UpdateTextBox(file);
                }
            }
            catch (Exception ex)
            {
                UpdateTextBox("Error reading directory contents: " + ex.Message);
            }
        }

        private void SaveCompressedDirectory(byte[] zipBytes)
        {
            try
            {
                string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "received_directory.zip");
                File.WriteAllBytes(zipPath, zipBytes);

                UpdateTextBox("Directory compression successfully received and saved at " + zipPath);
            }
            catch (Exception ex)
            {
                UpdateTextBox("Error saving compressed directory: " + ex.Message);
            }
        }

        private void UpdateTextBox(string message)
        {
            textBox4.Invoke((MethodInvoker)delegate {
                textBox4.AppendText(message + Environment.NewLine);
            });
        }

        private async void SendMsg_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;
            await SendMessageAsync(message);
        }

        private async void SendFilePath_Click(object sender, EventArgs e)
        {
            string filepath = "FILEPATH:" + textBox2.Text;
            await SendMessageAsync(filepath);
        }

        private async void SendDirPath_Click(object sender, EventArgs e)
        {
            string dirpath = "DIRPATH:" + textBox3.Text;
            await SendMessageAsync(dirpath);
        }
        private async Task SendMessageAsync(string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }
        private async void SendImgpath_Click(object sender, EventArgs e)
        {
            string imagePath = "IMAGEPATH:" + textBox5.Text;
            await SendMessageAsync(imagePath);
        }
        private void Close_Click(object sender, EventArgs e)
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
            if (listenerThread != null)
                listenerThread.Abort();
            UpdateTextBox("Connection close from client");
        }
        private void textBox4_TextChanged(object sender, EventArgs e){ }
        private void label1_Click(object sender, EventArgs e){ }
    }
}
