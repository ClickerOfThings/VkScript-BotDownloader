using System;
using System.Collections.Generic;
using System.Text;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNetExtend.MessageLongPoll;
using System.Runtime.Serialization.Formatters.Soap;
using System.IO;
using System.Windows.Forms;
using System.Net;

namespace test_b
{
    class Program
    {
        static VkApi api;
        static Random rand = new Random();
        static string configPath = ".\\config.dat";
        static Config config;
        [STAThread]
        static void Main(string[] args)
        {
            if (!LoadConfig())
            {
                SetConfig();
            }
            var serverLP = api.Messages.GetLongPollServer(true);
            VkNetExtMessageLongPollWatcher sss = new VkNetExtMessageLongPollWatcher(new MessageLongPollWatcherOptions()
            {
                Wait = 25
            }, api);
            sss.Ts = ulong.Parse(serverLP.Ts);
            sss.Pts = serverLP.Pts;
            sss.NewMessages += newMessage;
            sss.StartWatchAsync();
            Console.ReadLine();
        }

        static bool LoadConfig()
        {
            if (new FileInfo(configPath).Exists)
            {
                try
                {
                    StreamReader reader = new StreamReader(configPath);
                    config = (Config)LoadFromSoapFile(configPath, reader);
                    api = new VkApi();
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = config.accessToken
                    });
                    Console.WriteLine("loaded config file and authed");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        static void SetConfig()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            do
            {
                Console.WriteLine("Choose where to save");
            } while (fbd.ShowDialog() != DialogResult.OK);
            Console.WriteLine("type the command, on which bot will save pictures");
            string command = Console.ReadLine();
            string accessToken = "";
            while (true)
            {
                try
                {
                    Console.WriteLine("Type in access token");
                    accessToken = Console.ReadLine();
                    api = new VkApi();
                    api.Authorize(new ApiAuthParams()
                    {
                        AccessToken = accessToken
                    });
                    break;
                }
                catch
                {
                    Console.WriteLine("Wrong token");
                    continue;
                }
            }
            config = new Config(fbd.SelectedPath, command, accessToken);
            SaveAsSoapFormat(config, configPath);
            Console.WriteLine("created and saved config file in " + configPath);
        }

        static object LoadFromSoapFile(string fileName, StreamReader stream)
        {
            object desrlz = null;
            try
            {
                SoapFormatter soapFormat = new SoapFormatter();
                Stream _stream = stream.BaseStream;
                using (stream)
                {
                    desrlz = soapFormat.Deserialize(_stream);
                }
            }
            catch
            {
                // ¯\_(ツ)_/¯
            }
            return desrlz;
        }

        static void SaveAsSoapFormat(object graph, string path)
        {
            SoapFormatter soapFormat = new SoapFormatter();
            using (Stream fStream = new FileStream(path,
                FileMode.Create, FileAccess.Write, FileShare.None))
            {
                soapFormat.Serialize(fStream, graph);
            }
        }

        static void newMessage(IMessageLongPollWatcher watcher, IEnumerable<VkNet.Model.Message> messages)
        {
            foreach (VkNet.Model.Message message in messages)
            {
                if (message.Text == config.Command)
                {
                    if (message.Attachments.Count > 0)
                    {
                        //string finalUrl = "";
                        List<Uri> urls = new List<Uri>();
                        StringBuilder sb = new StringBuilder();
                        foreach (Attachment attachment in message.Attachments)
                        {
                            var photo = attachment;
                            sb.AppendFormat(photo.Instance.OwnerId.ToString() + "_");
                            sb.AppendFormat(photo.Instance.Id.ToString() + "_");
                            sb.AppendFormat(photo.Instance.AccessKey.ToString());
                            var _photos = api.Photo.GetById(new string[] { sb.ToString() }, photoSizes: true);
                            sb.Clear();
                            //finalUrl+=_photos[0].Sizes[_photos[0].Sizes.Count - 1].Url.ToString()+"\n";
                            //finalUrl += _photos[0];
                            ulong maxHW = 0;
                            PhotoSize maxSize = null;
                            foreach (PhotoSize ps in _photos[0].Sizes)
                            {
                                if (ps.Height + ps.Width > maxHW)
                                {
                                    maxHW = ps.Height + ps.Width;
                                    maxSize = ps;
                                }
                            }
                            //finalUrl += maxSize.Url.ToString() + "\n";
                            urls.Add(maxSize.Url);
                        }
                        int fileNumber = rand.Next(int.MaxValue);
                        foreach (Uri url in urls)
                        {
                            try
                            {
                                WebRequest wr = WebRequest.Create(url.ToString());
                                using (Stream stream = wr.GetResponse().GetResponseStream())
                                {
                                    System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
                                    DirectoryInfo di = new DirectoryInfo(config.Path);
                                    while (di.GetFiles(fileNumber.ToString() + ".png").Length != 0)
                                    {
                                        fileNumber = rand.Next(int.MaxValue);
                                    }
                                    img.Save(config.Path + "\\" + fileNumber + ".png");
                                    Console.WriteLine(fileNumber + ".png saved");
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Image " + url + " is broken, proceed...");
                            }
                        }
                        /*Console.WriteLine("send to " + message.PeerId + " message\n" + finalUrl);
                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = message.PeerId,
                            Message = finalUrl,
                            RandomId = rand.Next(int.MaxValue)
                        });*/
                    }
                }
            }
        }
    }
    [Serializable]
    public class Config
    {
        public string Path;
        public string Command;
        public string accessToken;
        public Config(string path, string command, string accessToken)
        {
            Path = path;
            Command = command;
            this.accessToken = accessToken;
        }
    }
}
