namespace SpeedDate.ServerPlugins.Authentication
{
    public interface IPasswordResetData
    {
        string Email { get; set; }
        string Code { get; set; }
    }
}