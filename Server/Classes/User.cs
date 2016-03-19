﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiffieHellman;

namespace Server
{
    public enum UserStatus
    {
        Offline,
        AFK,
        Online
    }

    class User
    {
        public string ID { get; }
        public string Name { get; }
        string Address { get; set; }
        DateTime LastConnectionCheck { get; set; }
        UserStatus Status { get; set; }

        // DIFFIE HELLMAN PART
        public DiffieHellmanTunnel Tunnel { get; }
        //

        public User(string ID, string name)
        {
            this.ID = ID;
            Name = name;
            Tunnel = new DiffieHellmanTunnel();
        }

        public void UpdateAddress(string address)
        {
            Address = address;
        }

        public void UpdateLastConnectionCheck(DateTime time)
        {
            LastConnectionCheck = time;
        }

        public void UpdateStatus(UserStatus status)
        {
            Status = status;
        }

        public DateTime LastConCheck()
        {
            return LastConnectionCheck;
        }
    }
}
