using System.Collections.Generic;
using Npgsql;
using SpeedDate.Logging;
using SpeedDate.Packets;
using SpeedDate.ServerPlugins.Profiles;

namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    public class ProfilesDbCockroachDb : IProfilesDatabase
    {
        private readonly string _connectionString;

        public ProfilesDbCockroachDb(string connectionString)
        {
            _connectionString = connectionString;
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
    }
}