using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using Gtk;
using RestSharp;

public partial class MainWindow : Gtk.Window
{
    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();

       

        //CellRendererText textRenderer = new CellRendererText();
        //cboVoice.PackStart(textRenderer, true);
        //cboVoice.AddAttribute(textRenderer, "text", 0);
        cboVoice.Active = 0;
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnButtonListentClicked(object sender, EventArgs e)
    {
        try
        {
            var objBuffer = this.textviewText.Buffer;

            TextIter textIterStart, textIterEnd;
            objBuffer.GetBounds(out textIterStart, out textIterEnd);

            string text = objBuffer.GetText(textIterStart, textIterEnd, false);


            this.textviewKey.Buffer.GetBounds(out textIterStart, out textIterEnd);


            string key = this.textviewKey.Buffer.GetText(textIterStart, textIterEnd, false);

            if (string.IsNullOrEmpty(key))
            {
                AddLog("Invalid zalo key", "ERROR");
                return;

            }
            //tree_iter = combo.get_active_iter()
            //if tree_iter is not None:
            //    model = combo.get_model()
            //    row_id, name = model[tree_iter][:2]
            //    print("Selected: ID=%d, name=%s" % (row_id, name))
            //else:
            //    entry = combo.get_child()
            //    print("Entered: %s" % entry.get_text())

            int voiceid = this.cboVoice.Active;

            double voiceSpeed = this.hscaleSpeed.Value/10;

            //TreeIter treeIter;

            //  if(  this.cboVoice.GetActiveIter(out treeIter))
            //{
            //    IList row = (IList)cboVoice.Model.GetValue(treeIter, 0); //ILIST 0 por que es el unico elemento aunque dentro vallan las columnas
            //    Console.WriteLine(row);
            //}row


            ZaloResultBO zaloResultBO = null;

            var client = new RestClient("https://api.zalo.ai/v1/tts/synthesize");
            {
                var request = new RestRequest();
                request.AddHeader("apikey", "MpYbVey2LV38x0EuSIHmfMjPw4sNPqRw");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("input", text);
                request.AddParameter("speaker_id", voiceid + 1);
                request.AddParameter("speed", "1");
                var response = client.ExecutePostAsync(request).Result;

                zaloResultBO = Newtonsoft.Json.JsonConvert.DeserializeObject<ZaloResultBO>(response.Content);
                AddLog(response.Content);

                client.Dispose();
            }

                
            if (zaloResultBO != null && zaloResultBO.error_code == 0)
            {

                string url = zaloResultBO.data["url"];
               
                PlayAudio(url);
                //  var url = "http://media.ch9.ms/ch9/2876/fd36ef30-cfd2-4558-8412-3cf7a0852876/////AzureWebJobs103.mp3";
                //using (var mf = new MediaFoundationReader(url))
                //using (var wo = new WaveOutEvent())
                //{
                //    wo.Init(mf);
                //    wo.Play();
                //    while (wo.PlaybackState == PlaybackState.Playing)
                //    {
                //        Thread.Sleep(1000);
                //    }
                //}1000
            }
            else
            {
                AddLog(zaloResultBO.error_message, "ERROR")
            }


        }
        catch (Exception objEx)
        {

            AddLog( objEx.ToString(),"ERROR");
        }
    }

    private void PlayAudio(string url)
    {
        this.AddLog("download file "+url );
        var tempFile = System.IO.Path.GetTempFileName();
        // using var writer = File.OpenWrite(tempFile);

        using (var client = new RestClient(url))
        {
            RestRequest request = new RestRequest();
            var response = client.ExecuteGetAsync(request).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // Read bytes
                byte[] fileBytes = response.RawBytes;

                File.WriteAllBytes(tempFile, fileBytes);

            }
        }
           
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = "/opt/homebrew/bin/ffplay",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            Arguments = string.Format("{0}", tempFile)
        };

        using Process exeProcess = Process.Start(startInfo);

        
       
        exeProcess.WaitForExit();
        exeProcess.Close();
    }

    private void AddLog(string msg , string msgtype = "info")
    {
        Gtk.Application.Invoke(delegate {
           
        var iter = this.textviewLog.Buffer.GetIterAtLine(0);
            this.textviewLog.Buffer.Insert(ref iter, $@"{DateTime.Now.ToString("dd-MMMM-yyyy HH:mm:ss")} -  {msgtype} - {msg}");
        });
      
    }

  

   
}


public class ZaloResultBO
{
    public int error_code { get; set; }
    public string error_message { get; set; }
    public Dictionary<string,string> data { get; set; }
}