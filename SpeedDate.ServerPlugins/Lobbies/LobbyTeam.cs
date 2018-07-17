using System.Collections.Generic;
using SpeedDate.Packets.Lobbies;

namespace SpeedDate.ServerPlugins.Lobbies
{
    public class LobbyTeam
    {
        private readonly Dictionary<string, string> _properties;
        private readonly Dictionary<string, LobbyMember> _members;

        /// <summary>
        /// Min number of players, required in this team
        /// </summary>
        public int MinPlayers { get; set; }

        /// <summary>
        /// How many players can join this team
        /// </summary>
        public int MaxPlayers { get; set; }

        public string Name { get; }

        /// <summary>
        /// Returns a number of members in this team
        /// </summary>
        public int PlayerCount => _members.Count;

        public LobbyTeam(string name)
        {
            _properties = new Dictionary<string, string>();
            _members = new Dictionary<string, LobbyMember>();

            Name = name;
            MinPlayers = 1;
            MaxPlayers = 5;
        }

        /// <summary>
        /// Checks if a specific member can be added to the lobby
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool CanAddPlayer(LobbyMember member)
        {
            return PlayerCount < MaxPlayers;
        }

        /// <summary>
        /// Adds a member to the lobby
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public bool AddMember(LobbyMember member)
        {
            if (_members.ContainsKey(member.Username))
            {
                return false;
            }

            _members.Add(member.Username, member);
            member.Team = this;

            return true;
        }

        /// <summary>
        /// Removes a member from the lobby
        /// </summary>
        /// <param name="member"></param>
        public void RemoveMember(LobbyMember member)
        {
            _members.Remove(member.Username);

            if (member.Team == this)
                member.Team = null;
        }

        /// <summary>
        /// Sets lobby property to a specified value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetProperty(string key, string value)
        {
            _properties[key] = value;
        }

        /// <summary>
        /// Returns a MUTABLE dictionary of members
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, LobbyMember> GetTeamMembers()
        {
            return _members;
        }

        /// <summary>
        /// Returns a MUTABLE dictionary of properties
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetTeamProperties()
        {
            return _properties;
        }

        /// <summary>
        /// Generates a lobby data packet
        /// </summary>
        /// <returns></returns>
        public LobbyTeamData GenerateData()
        {
            return new LobbyTeamData
            {
                MaxPlayers = MaxPlayers,
                MinPlayers = MinPlayers,
                Name = Name,
                Properties = _properties
            };
        }
    }
}