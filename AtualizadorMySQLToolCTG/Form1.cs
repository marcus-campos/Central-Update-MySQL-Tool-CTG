using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Net;
using System.Collections.Specialized;


namespace AtualizadorMySQLToolCTG
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Thread downloadThread = null;
        private int totalFiles = 0;
        private int filesRead = 0;
        private static int percentProgress;
        private string knownToken = " NAME=\"";
        private delegate void UpdateProgessCallback(
            int filesRead, int totalFiles);
        private delegate void SetTextCallback(string text);

        private delegate void EnableBtnDownloadCallback();

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void GetHTTPContent(StringBuilder webPageString)
        {
            // used on each read operation
            byte[] buffer = new byte[8192];
            string tempString = null;
            int count = 0;

            // prepare the web page we will be asking for
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create("www.mysqltoolctg.freeoda.com");
            // execute the request
            HttpWebResponse response =
                (HttpWebResponse)request.GetResponse();
            // we will read data via the response stream
            Stream responseStream = response.GetResponseStream();

            do
            {
                // Fills the buffer with data
                count = responseStream.Read(buffer, 0, buffer.Length);

                if (count != 0)
                {
                    // Translates from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buffer, 0, count);

                    // Continues building the string
                    webPageString.Append(tempString);
                }
            }
            while (count > 0);
        }

        private void ParseFileNamesFromWebPage(
            StringCollection fileNames, string webPageContent)
        {
            // Get the string that ends with the first token.
            int tokenIndex = webPageContent.IndexOf(knownToken);
            webPageContent = webPageContent.Remove(0, tokenIndex + knownToken.Length);

            // Parse the file to get all the file names from the file.
            while (webPageContent.Length > 0 && tokenIndex > 0)
            {
                String fileName = webPageContent.Substring(0, webPageContent.IndexOf("\""));
                fileNames.Add(fileName);
                //labelStatusText.Text = "Adding file " + fileName + " to download...";

                tokenIndex = webPageContent.IndexOf(knownToken);
                webPageContent = webPageContent.Remove(0, tokenIndex + knownToken.Length);
            }

            // Update the total number of files in the HTTP file.
            totalFiles = fileNames.Count;
        }

        private void Download()
        {
            try
            {
                StringBuilder webPageString = new StringBuilder();
                StringCollection fileNames = new StringCollection();
                
                rtb_statusAtualiza.Text = ("Iniciando o download...");
                // Get the HTTP content.
                GetHTTPContent(webPageString);
                string webPageContent = webPageString.ToString();

                // Parse out all the file names.
                ParseFileNamesFromWebPage(fileNames, webPageContent);

                foreach (String fileName in fileNames)
                {

                    if (this.textExclude.Text.Length > 0 ||
                        this.textOnlyInclude.Text.Length > 0)
                    {
                        if (fileName.Contains(this.textExclude.Text) ||
                            !fileName.Contains(this.textOnlyInclude.Text))
                        {

                            UpdateProgessCallback delegateCB =
                                new UpdateProgessCallback(UpdateProgress);
                            this.Invoke(delegateCB,
                                new object[] { filesRead++, totalFiles });


                            continue;
                        }
                    }


                    DownloadFile(
                        textBoxRemoteUrl.Text + "/" + fileName,
                        textLocalPath.Text + fileName);
                }

                UpdateStatusBox("Download Completed...");
            }
            catch (NotSupportedException exception)
            {
                UpdateStatusBox("Exception! " + exception.Message);
            }
            catch (WebException exception)
            {
                UpdateStatusBox("Exception! " + exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                UpdateStatusBox("Exception! " + exception.Message);
            }
            finally
            {
                // Check if this method is running on a different thread
                // than the thread that created the control.
                if (this.textStatus.InvokeRequired)
                {
                    // It's on a different thread, so use Invoke.
                    EnableBtnDownloadCallback delegateCB =
                        new EnableBtnDownloadCallback(EnableBtnDownload);
                    this.Invoke(delegateCB);
                }
                else
                {
                    btnStartDownload.Enabled = true;
                }
            }


        }
    }
}
