﻿using Newtonsoft.Json;

namespace SystemMessage
{
    public class Message
    {
        public string MessageSender { get; set; }
        public string MessageType { get; set; }
        public string MessageDestination { get; set; }
        public string MessageContent { get; set; }

        public Message(string messageSender, string messageType = "NO_TYPE", string messageDestination = "NO_DESTINATION", string messageContent = "NO_CONTENT")
        {
            MessageSender = messageSender;
            MessageType = messageType;
            MessageDestination = messageDestination;
            MessageContent = messageContent;
        }

        public Message() { }

        public void loadJson(string jsonString)
        {
            try
            {
                Message tmp = JsonConvert.DeserializeObject<Message>(jsonString);
                this.MessageSender = tmp.MessageSender;
                this.MessageType = tmp.MessageType;
                this.MessageDestination = tmp.MessageDestination;
                this.MessageContent = tmp.MessageContent;
            }
            catch
            {

            }
        }

        public string getJsonString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
