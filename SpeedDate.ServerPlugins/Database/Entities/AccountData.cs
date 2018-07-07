using System;
using System.Collections.Generic;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    public class AccountData
    {
        public long AccountId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsGuest { get; set; }

        public bool IsEmailConfirmed { get; set; }

        public Dictionary<string, string> Properties { get; set; }

        public event Action<AccountData> OnChange;

        public void MarkAsDirty()
        {
            OnChange?.Invoke(this);
        }
    }
}
