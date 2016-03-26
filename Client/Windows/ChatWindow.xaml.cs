using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunicatorCore.Classes.Model;
using System.IO;
using OpenSSL.Crypto;
using System.Threading;
using System.Media;
using Config;

namespace Client
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly Uri uriString = new Uri("http://" + ConnectionInfo.Address + ":" + ConnectionInfo.Port + "/" + ConfigurationHandler.GetValueFromKey("SEND_CHAT_MESSAGE_API") + "/");
        private readonly Uri incomingMessage = new Uri("Media/incoming.wav", UriKind.Relative);
        private readonly Uri outcomingMessage = new Uri("Media/outcoming.wav", UriKind.Relative);
        private readonly FlashWindow flashWindow = new FlashWindow(Application.Current);
        public string TargetID { get; }

        NameValueCollection headers = new NameValueCollection();
        NameValueCollection data = new NameValueCollection();
        MediaPlayer player = new MediaPlayer();
        List<DisplayMessage> chatWindowMessages = new List<DisplayMessage>();

        private readonly CryptoRSA cryptoService;

        public ChatWindow(string target) : this(target, target)
        {
        }

        public ChatWindow(string target, string windowName)
        {
            InitializeComponent();
            TargetID = target;
            Title = Title + " - " + windowName;
            
            ChatController.AddNewWindow(this);

            cryptoService = new CryptoRSA();
            cryptoService.LoadRsaFromPublicKey("SERVER_Public.pem");
            chatText.IsEnabled = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(SendInitMessage);
            thread.IsBackground = true;
            thread.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ChatController.CloseWindow(this);
        }

        private void SendMsg()
        {
            if (string.IsNullOrWhiteSpace(chatText.Text))
                return;

            try
            {
                string UID = Guid.NewGuid().ToString();
                AddMessageToChatWindow(UID, ConnectionInfo.Sender, chatText.Text, true);
                PrepareMessage(UID, chatText.Text);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void PlaySound(bool outcoming = false)
        {
            if(outcoming && IsPropertyTrue("OUTCOMING_SOUND"))
            {
                player.Open(outcomingMessage);
                player.Play();
            }
            else if (!outcoming && IsPropertyTrue("INCOMING_SOUND") )
            {
                player.Open(incomingMessage);
                player.Play();
            }
        }

        private bool IsPropertyTrue(string propertyName)
        {
            return ConfigurationHandler.GetValueFromKey(propertyName) == "True";
        }

        void AddMessageToChatWindow(string UID, string userName, string messageContent, bool isFromSelf = false)
        {
            DisplayMessage message = new DisplayMessage(UID, userName, messageContent, isFromSelf);


            /* This Probaly should be somewhere else    */
            if (userName == "TUNNEL CREATOR")
                message.TripStatus = "DELIVERED";
            else
                message.TripStatus = "SENDED";
            /*                                          */


            chatWindowMessages.Add(message);
            listBox.Items.Insert(0, message);
     
            PlaySound(isFromSelf);
            if (!isFromSelf && IsPropertyTrue("BLINK_CHAT") )
                flashWindow.FlashApplicationWindow();
        }

        void SendInitMessage()
        {
            ControlMessage message = new ControlMessage();
            try
            {
                Message chatMessage = new Message(Guid.NewGuid().ToString(), ConnectionInfo.Sender, ConnectionInfo.Sender, "INIT");
                string encryptedChatMessage = cryptoService.PublicEncrypt(chatMessage.GetJsonString(), cryptoService.PublicRSA);
                message = new ControlMessage(ConnectionInfo.Sender, "CHAT_INIT", encryptedChatMessage);

                string responseString = string.Empty;
                using (var wb = new WebClient())
                {
                    wb.Proxy = null;
                    headers["messageContent"] = message.GetJsonString();
                    wb.Headers.Add(headers);
                    data["DateTime"] = DateTime.Now.ToShortDateString();
                    byte[] responseByte = wb.UploadValues(uriString, "POST", data);
                    responseString = Encoding.UTF8.GetString(responseByte);
                }

                if (responseString == "OK")
                {
                    listBox.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        AddMessageToChatWindow(Guid.NewGuid().ToString(), "TUNNEL CREATOR", "Encrypted channel has been established.");
                    }));
                    chatText.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        chatText.IsEnabled = true;
                    }));
                }
                else
                {
                    listBox.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        AddMessageToChatWindow(Guid.NewGuid().ToString(), "TUNNEL CREATOR", "Encrypted channel is not established.");
                    }));
                    chatText.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        chatText.IsEnabled = true;
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        void PrepareMessage(string UID, string textBoxContent)
        {
            ControlMessage message = new ControlMessage();
            try
            {
                Message chatMessage = new Message(UID, ConnectionInfo.Sender, ConnectionInfo.Sender, textBoxContent);
                string encryptedChatMessage = cryptoService.PublicEncrypt(chatMessage.GetJsonString(), cryptoService.PublicRSA);
                message = new ControlMessage(ConnectionInfo.Sender, "CHAT_MESSAGE", encryptedChatMessage);

                Thread SendReceiveMessage = StartThreadWithParam(UID, message);
                chatText.Text = string.Empty;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public Thread StartThreadWithParam(string UID, ControlMessage messageToSend)
        {
            var t = new Thread(() => SendMessage(UID, messageToSend));
            t.Start();
            return t;
        }


        void SendMessage(string UID, ControlMessage message)
        {
            try
            {
                using (var wb = new WebClient())
                {
                    wb.Proxy = null;

                    headers["messageContent"] = message.GetJsonString();
                    wb.Headers.Add(headers);

                    data["DateTime"] = DateTime.Now.ToShortDateString();

                    byte[] responseByte = wb.UploadValues(uriString, "POST", data);
                    string responseString = Encoding.UTF8.GetString(responseByte);
                    if (responseString == "RECEIVED")
                    {
                        chatWindowMessages.Find(x => x.UID == UID).TripStatus = "SEND_ACK";
                        RefreshMessages();
                    }
                }
            }
            catch
            {

            }
        }

        // Maybe method name change?
        public void DeliverMessage(Message message)
        {
            Application.Current.Dispatcher.Invoke(() => deliverMessage(message));
        }

        // Maybe method name change?
        void deliverMessage(Message message)
        {
            /* This is code for INCOMING messages without our knowlage */
            string messageUID = message.MessageUID;
            string messageContent = message.MessageContent;

            DisplayMessage dspMsg = new DisplayMessage(messageUID, TargetID, messageContent, false);

            dspMsg.TripStatus = "DELIVERED";
            chatWindowMessages.Add(dspMsg);
            listBox.Items.Insert(0, dspMsg);
        }

        public void RefreshMessages()
        {
            Application.Current.Dispatcher.Invoke(() => Refresh());
        }

        void Refresh()
        {
            listBox.Items.Refresh();
        }

        private void chatText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendMsg();
        }
    }
}