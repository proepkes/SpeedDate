using System;
using System.Collections.Generic;
using System.Diagnostics;
using Npgsql;
using SpeedDate.Logging;
using SpeedDate.Packets;
using SpeedDate.ServerPlugins.Authentication;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    public class AuthDbCockroachDb : IDbAccess
    {
        private readonly string _connectionString;

        public AuthDbCockroachDb(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IAccountData CreateAccountObject()
        {
            return new SqlAccountData();
        }

        public IAccountData GetAccount(string username)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE username = @username;";
                cmd.Parameters.AddWithValue("@username", username);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    return null;

                return ReadAccountData(reader, cmd);
            }
        }

        public IAccountData GetAccountByToken(string token)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE token = @token;";
                cmd.Parameters.AddWithValue("@token", token);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    return null;

                return ReadAccountData(reader, cmd);
            }
        }

        public IAccountData GetAccountByEmail(string email)
        {

            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE email = @email;";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    return null;

                return ReadAccountData(reader, cmd);
            }
        }

        public void SavePasswordResetCode(IAccountData account, string code)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                //cmd.CommandText = "INSERT INTO password_reset_codes (email, code) " +
                //				  "VALUES(@email, @code) " +
                //				  "ON DUPLICATE KEY UPDATE code = @code";
                cmd.CommandText = "INSERT INTO password_reset_codes (email, code) " +
                                  "VALUES(@email, @code) " +
                                  "ON CONFLICT (email) DO UPDATE SET code = @code";

                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.ExecuteNonQuery();
            }
        }

        public IPasswordResetData GetPasswordResetData(string email)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM password_reset_codes " +
                                  "WHERE email = @email";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    return null;

                // Read row
                reader.Read();

                return new PasswordResetData()
                {
                    Code = reader["code"] as string,
                    Email = reader["email"] as string
                };
            }
        }

        public void SaveEmailConfirmationCode(string email, string code)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                //cmd.CommandText = "INSERT INTO email_confirmation_codes (email, code) " +
                //				  "VALUES(@email, @code) " +
                //				  "ON DUPLICATE KEY UPDATE code = @code";
                cmd.CommandText = "INSERT INTO email_confirmation_codes (email, code) " +
                                  "VALUES(@email, @code) " +
                                  "ON CONFLICT (email) DO UPDATE SET code = @code";

                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.ExecuteNonQuery();
            }

            Debug.WriteLine("Should have inserted: " + email + " " + code);
        }

        public string GetEmailConfirmationCode(string email)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM email_confirmation_codes " +
                                  "WHERE email = @email";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    return null;

                // Read row
                reader.Read();

                return reader["code"] as string;
            }
        }

        public void UpdateAccount(IAccountData acc)
        {
            var account = acc as SqlAccountData;
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "UPDATE accounts " +
                                  "SET password = @password, " +
                                  "email = @email, " +
                                  "is_admin = @is_admin, " +
                                  "is_guest = @is_guest, " +
                                  "is_email_confirmed = @is_email_confirmed " +
                                  "WHERE account_id = @account_id";
                cmd.Parameters.AddWithValue("@password", account.Password);
                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@is_admin", account.IsAdmin);
                cmd.Parameters.AddWithValue("@is_guest", account.IsGuest);
                cmd.Parameters.AddWithValue("@is_email_confirmed", account.IsEmailConfirmed);
                cmd.Parameters.AddWithValue("@account_id", account.AccountId);

                cmd.ExecuteNonQuery();
            }
        }

        public void InsertNewAccount(IAccountData acc)
        {
            var account = acc as SqlAccountData;

            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;

                cmd.CommandText =
                    "INSERT INTO accounts (username, password, email, is_admin, is_guest, is_email_confirmed) " +
                    "VALUES (@username, @password, @email, @is_admin, @is_guest, @is_email_confirmed) RETURNING account_id";

                cmd.Parameters.AddWithValue("@username", account.Username);
                cmd.Parameters.AddWithValue("@password", account.Password);
                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@is_admin", account.IsAdmin);
                cmd.Parameters.AddWithValue("@is_guest", account.IsGuest);
                cmd.Parameters.AddWithValue("@is_email_confirmed", account.IsEmailConfirmed);

                Debug.WriteLine("InsertNewAccount - Query:\n" + cmd.CommandText);

                var id = cmd.ExecuteScalar() as int?;
                if (id.HasValue)
                {
                    account.AccountId = id.Value;
                }
            }
        }

        public void InsertToken(IAccountData acc, string token)
        {
            var account = acc as SqlAccountData;

            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "UPDATE accounts " +
                                  "SET token = @token " +
                                  "WHERE account_id = @account_id";
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@account_id", account.AccountId);

                account.Token = token;

                cmd.ExecuteNonQuery();
            }
        }

        private SqlAccountData ReadAccountData(NpgsqlDataReader reader, NpgsqlCommand cmd)
        {
            SqlAccountData account = null;

            // Read primary account data
            while (reader.Read())
            {
                account = new SqlAccountData()
                {
                    Username = reader["username"] as string,
                    AccountId = long.Parse(reader["account_id"].ToString()),
                    Email = reader["email"] as string,
                    Password = reader["password"] as string,
                    IsAdmin = reader["is_admin"] as bool? ?? false,
                    IsGuest = reader["is_guest"] as bool? ?? false,
                    IsEmailConfirmed = reader["is_email_confirmed"] as bool? ?? false,
                    Properties = new Dictionary<string, string>(),
                    Token = reader["token"] as string
                };
            }

            if (account == null)
                return null;

            // Read account values
            reader.Close();

            cmd.CommandText = "SELECT * FROM account_properties WHERE account_id = @account_id";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@account_id", account.AccountId);
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var key = reader["prop_key"] as string ?? "";
                var value = reader["prop_val"] as string ?? "";

                if (string.IsNullOrEmpty(key))
                    continue;

                account.Properties[key] = value;
            }

            return account;
        }

        public void RestoreProfile(ObservableServerProfile profile)
        {
            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT profile_values.* " +
                                  "FROM profile_values " +
                                  "INNER JOIN accounts " +
                                  "ON accounts.account_id = profile_values.account_id " +
                                  "WHERE username = @username;";
                cmd.Parameters.AddWithValue("@username", profile.Username);

                var reader = cmd.ExecuteReader();

                // There's no such data
                if (!reader.HasRows)
                    return;

                var data = new Dictionary<short, string>();

                while (reader.Read())
                {
                    //var key = reader.GetInt16("value_key");
                    var key = short.Parse(reader["value_key"].ToString());

                    var value = reader["value_value"] as string ?? "";
                    data.Add(key, value);
                }

                profile.FromStrings(data);
            }
        }

        public void UpdateProfile(ObservableServerProfile profile)
        {
            if (!profile.ShouldBeSavedToDatabase)
                return;

            using (var con = new NpgsqlConnection(_connectionString))
            using (var cmd = new NpgsqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT account_id FROM accounts " +
                                  "WHERE username = @username";
                cmd.Parameters.AddWithValue("@username", profile.Username);

                var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    Logs.Error("Tried to save a profile of a user who has no account: " + profile.Username);
                    return;
                }

                reader.Read();

                //var accountId = reader.GetInt32("account_id");
                var accountId = int.Parse(reader["account_id"].ToString());

                reader.Close();
                cmd.Parameters.Clear();

                cmd.Connection = con;

                foreach (var unsavedProp in profile.UnsavedProperties)
                {
                    cmd.CommandText = "INSERT INTO profile_values (account_id, value_key, value_value) " +
                                      "VALUES (@account_id, @value_key, @value_value)" +
                                      "ON CONFLICT (account_id, value_key) DO UPDATE SET value_value = @value_value";

                    cmd.Parameters.AddWithValue("@account_id", accountId);
                    cmd.Parameters.AddWithValue("@value_key", unsavedProp.Key);
                    cmd.Parameters.AddWithValue("@value_value", unsavedProp.SerializeToString());

                    cmd.ExecuteNonQuery();

                    cmd.Parameters.Clear();
                }
            }

            profile.UnsavedProperties.Clear();
        }

        public class SqlAccountData : IAccountData
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

            public event Action<IAccountData> OnChange;

            public void MarkAsDirty()
            {
                OnChange?.Invoke(this);
            }
        }

        public class PasswordResetData : IPasswordResetData
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }
    }
}
