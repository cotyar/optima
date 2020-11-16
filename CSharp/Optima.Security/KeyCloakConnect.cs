using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Keycloak.Net;
using Keycloak.Net.Models.Users;

namespace Optima.Security
{
    public class KeyCloakConnect
    {
        private readonly KeycloakClient _client;
        private readonly string _realm = "Optima";
        // private readonly string _realm = "master";

        public KeyCloakConnect()
        {
            _client = new KeycloakClient("http://127.0.0.1:9080/", "testUser", "password");
            // _client = new KeycloakClient("http://127.0.0.1:9080/", "keycloak", "password");
        }
        
        public async Task<ImmutableArray<string>> GetUsers() => 
            (await _client.GetUsersAsync(_realm).ConfigureAwait(false)).Select(u => $"{u.Id} - {u.UserName}").ToImmutableArray();

        public async Task AddUser(string userName) => 
            await _client.CreateUserAsync(_realm, new User {UserName = userName}).ConfigureAwait(false);
        
    }
}
