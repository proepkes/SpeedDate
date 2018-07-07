namespace SpeedDate.ServerPlugins.Database.CockroachDb
{
    public class PasswordResetData
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
