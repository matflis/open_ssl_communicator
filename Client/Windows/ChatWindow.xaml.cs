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
        private readonly Uri _messageUri = new Uri("http://" + ConnectionInfo.Address + ":" + ConnectionInfo.Port + "/" + ConfigurationHandler.GetValueFromKey("SEND_CHAT_MESSAGE_API") + "/");
        private readonly Uri _incomingMessageUri = new Uri("Media/incoming.wav", UriKind.Relative);
        private readonly Uri _outcomingMessageUri = new Uri("Media/outcoming.wav", UriKind.Relative);
        private readonly FlashWindow _flashWindow = new FlashWindow(Application.Current);

        private readonly string _token;
        private readonly SymmetricCipher _cipher;
        public string TargetId { get; }

        private readonly NameValueCollection _headers = new NameValueCollection();
        private readonly NameValueCollection _data = new NameValueCollection();
        private readonly MediaPlayer _player = new MediaPlayer();
        private readonly List<DisplayMessage> _chatWindowMessages = new List<DisplayMessage>();

        private readonly CryptoRSA _cryptoService;

        public ChatWindow(string target) : this(target, target)
        {
        }

        public ChatWindow(string target, string windowName)
        {
            InitializeComponent();
            TargetId = target;
            Title = Title + " - " + windowName;
            
            ChatController.AddNewWindow(this);

            _cryptoService = new CryptoRSA();
            _cryptoService.LoadRsaFromPublicKey("SERVER_Public.pem");
            _token = ConfigurationHandler.GetValueFromKey("TOKEN_VALUE");
            _cipher = new SymmetricCipher();

            ChatText.IsEnabled = false;
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
            if (string.IsNullOrWhiteSpace(ChatText.Text))
                return;

            try
            {
                string UID = Guid.NewGuid().ToString();
                AddMessageToChatWindow(UID, ConnectionInfo.Sender, ChatText.Text, true);
                PrepareMessage(UID, ChatText.Text);
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
                _player.Open(_outcomingMessageUri);
                _player.Play();
            }
            else if (!outcoming && IsPropertyTrue("INCOMING_SOUND") )
            {
                _player.Open(_incomingMessageUri);
                _player.Play();
            }
        }

        private bool IsPropertyTrue(string propertyName)
        {
            return ConfigurationHandler.GetValueFromKey(propertyName) == "True";
        }

        void AddMessageToChatWindow(string UID, string userName, string messageContent, bool isFromSelf = false, int pos = 0)
        {
            DisplayMessage message = new DisplayMessage(UID, userName, messageContent, isFromSelf);


            /* This Probaly should be somewhere else    */
            message.UpdateMessageStatus( userName == "TUNNEL CREATOR"? "DELIVERED" : "SENDED");
            /*                                          */


            _chatWindowMessages.Add(message);
            ListBox.Items.Insert(pos, message);
     
            PlaySound(isFromSelf);
            if (!isFromSelf && IsPropertyTrue("BLINK_CHAT") )
                _flashWindow.FlashApplicationWindow();
        }

        void SendInitMessage()
        {
            ControlMessage message = new ControlMessage();
            try
            {
                Message chatMessage = new Message(Guid.NewGuid().ToString(), ConnectionInfo.Sender, TargetId, "INIT");
                string encryptedChatMessage = _cryptoService.PublicEncrypt(chatMessage.GetJsonString(), _cryptoService.PublicRSA);
                message = new ControlMessage(ConnectionInfo.Sender, "CHAT_INIT", encryptedChatMessage);

                string responseString = string.Empty;
                using (var wb = new WebClient())
                {
                    wb.Proxy = null;
                    _headers["messageContent"] = message.GetJsonString();
                    wb.Headers.Add(_headers);
                    _data["DateTime"] = DateTime.Now.ToShortDateString();
                    byte[] responseByte = wb.UploadValues(_messageUri, "POST", _data);
                    responseString = Encoding.UTF8.GetString(responseByte);
                }

                if (responseString == "OK")
                {
                    ListBox.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        AddMessageToChatWindow(Guid.NewGuid().ToString(), "TUNNEL CREATOR", "Encrypted channel has been established.", false, _chatWindowMessages.Count);
                    }));
                    ChatText.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        ChatText.IsEnabled = true;
                    }));
                }
                else if (responseString == "OFFLINE")
                {
                    ListBox.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        AddMessageToChatWindow(Guid.NewGuid().ToString(), "TUNNEL CREATOR", "Secound user is offline.", false, _chatWindowMessages.Count);
                    }));
                    ChatText.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        ChatText.IsEnabled = false;
                    }));
                }
                else
                {
                    ListBox.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        AddMessageToChatWindow(Guid.NewGuid().ToString(), "TUNNEL CREATOR", "Encrypted channel is not established.");
                    }));
                    ChatText.Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        ChatText.IsEnabled = false;
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
            try
            {
                string cipheredContent = _cipher.Encode(textBoxContent, _token, string.Empty);
                Message chatMessage = new Message(UID, ConnectionInfo.Sender, TargetId, textBoxContent, cipheredContent);
                string encryptedChatMessage = _cryptoService.PublicEncrypt(chatMessage.GetJsonString(), _cryptoService.PublicRSA);
                ControlMessage message = new ControlMessage(ConnectionInfo.Sender, "CHAT_MESSAGE", encryptedChatMessage);

                Thread sendReceiveMessageThread = StartThreadWithParam(UID, message);
                ChatText.Text = string.Empty;
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

                    _headers["messageContent"] = message.GetJsonString();
                    wb.Headers.Add(_headers);

                    _data["DateTime"] = DateTime.Now.ToShortDateString();

                    byte[] responseByte = wb.UploadValues(_messageUri, "POST", _data);
                    string responseString = Encoding.UTF8.GetString(responseByte);
                    if (responseString == "RECEIVED")
                    {
                        _chatWindowMessages.Find(x => x.UID == UID).UpdateMessageStatus("SEND_ACK");
                        RefreshMessages();
                    }
                    else if(responseString == "OFFLINE")
                    {
                        ChatText.IsEnabled = false;
                    }
                }
            }
            catch
            {

            }
        }

        // Maybe method name change?
        // M: ??
        public void UpdateMessageStatus(Message message)
        {
            Application.Current.Dispatcher.Invoke(() => updateMessageStatus(message));
        }

        // Maybe method name change?
        void updateMessageStatus(Message message)
        {
            string messageUID = message.MessageUID;
            string messageContent = message.MessageContent;

            DisplayMessage dspMsg = _chatWindowMessages.Find(x => x.UID == messageUID);
            if(dspMsg != null && messageContent == "DELIVERED")
            {
                dspMsg.UpdateMessageStatus("DELIVERED");
            }
            else
            {
                dspMsg = new DisplayMessage(messageUID, TargetId, messageContent, false);

                dspMsg.UpdateMessageStatus("DELIVERED");
                _chatWindowMessages.Add(dspMsg);
                ListBox.Items.Insert(0, dspMsg);
                PlaySound();
            }

            RefreshMessages();
        }

        public void RefreshMessages()
        {
            Application.Current.Dispatcher.Invoke(() => Refresh());
        }

        void Refresh()
        {
            ListBox.Items.Refresh();
        }

        private void ChatText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendMsg();
        }
    }
}