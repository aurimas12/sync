using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System.Threading;

namespace TableAir.AdminFlow.Model
{
    public class EmployeeRecord
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("workEmail")]
        public string WorkEmail { get; set; }

        public List<string> Groups { get; set; } = new List<string>();

        public List<(DateTime, string)> History { get; } = new List<(DateTime, string)>();

        public string GroupsList
        {
            get
            {
                return Groups.Count > 0 ? Groups.Aggregate((current, next) => current + ", " + next) : string.Empty;
            }
        }

        public static EmployeeRecord FromUser(User user)
        {
            var _ = new EmployeeRecord
            {
                Id = user.Id.ToString(),
                WorkEmail = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
            if (!string.IsNullOrWhiteSpace(user.CustomAttributes))
            {
                var customAttr = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(user.CustomAttributes);

                if (customAttr.ContainsKey("groups"))
                {
                    _.Groups = System.Text.Json.JsonSerializer.Deserialize<List<string>>(customAttr["groups"]);
                }
            }

            if (user.FirstSeen != null)
            {
                _.History.Add((user.FirstSeen.Value, "First time seen"));
            }
            if (user.LastSynced != null)
            {
                _.History.Add((user.LastSynced.Value, "Last time updated"));
            }

            return _;
        }
    }

    public class MicrosoftSync
    {
        public static bool IsSyncing = false;
        public static List<Action> _actions = new List<Action>();

        //public static readonly int TeamId = 2;
        //public static readonly string TeamUrl = "tableair";

        // // // dev team
        public static readonly int TeamId = 1; //dev team id 
        public static readonly string TeamUrl = "company"; //prod team name


        // prod team
        // public static readonly int TeamId = 3; //prod team id
        // public static readonly string TeamUrl = "devsync"; //prod team name


        private static CancellationTokenSource _ctsSleep = new CancellationTokenSource();
        public static bool TheresNoTeamLink = true;

        public static void ForceRefresh()
        {
            lock (_actions)
            {
                _ctsSleep.Cancel();
                _ctsSleep = new CancellationTokenSource();
            }
        }

        public static void AddAction(Action action)
        {
            lock (_actions)
            {
                _actions.Add(action);
            }
        }

        public static void RemoveAction(Action action)
        {
            lock (_actions)
            {
                _actions.Remove(action);
            }
        }

        public static void Start()
        {
            Action taskRunner = null;
            taskRunner = async () =>
            {
                CancellationToken token;
                IsSyncing = true;
                try
                {
                    var repo = new UsersRepository();
                    var teamLink = repo.ExternalLinks.Include(el => el.Team).Where(el => el.Team.Id == TeamId && el.Provider == 4).FirstOrDefault();

                    if (teamLink == null)
                    {
                        TheresNoTeamLink = true;

                        IsSyncing = false;
                        lock (_actions)
                        {
                            foreach (var action in _actions)
                            {
                                action.Invoke();
                            }
                        }

                        lock (_actions)
                        {
                            token = _ctsSleep.Token;
                        }
                        token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1800));

                        await Task.Factory.StartNew(taskRunner);
                        return;
                    }
                    TheresNoTeamLink = false;
                    // dev app
                    string clientDevId="a2920437-2703-4699-92a4-f6e5a6429df9";
                    string clientDevSecretValue="zWc7Q~k30i5ZTJqMIWNR6fGEPZbiHS1pjx9FI";
                    // prod app
                    string clientProdId="c2c3392c-9323-4f02-b831-dd22d285901d";
                    string clientProdSecretValue="hYo7Q~S0WuJK_.IVJEqzo7KW1xagi6ZvObCC0";
                    
                    var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(clientDevId) //client id
                                                              .WithClientSecret(clientDevSecretValue) //client secret value
                                                            
                                                              .WithAuthority(new Uri("https://login.microsoftonline.com/" + teamLink.ExternalId))
                                                              .Build();
                    var graphHttpClient = GraphClientFactory.Create(new ClientCredentialProvider(confidentialClientApplication));
                    var graphClient = new GraphServiceClient(graphHttpClient);

                    var allGroups = new List<(string Id, string DisplayName)>();
                    var fetchedGroups = await graphClient.Groups.Request().GetAsync(); 
                    while (true)
                    {
                        allGroups.AddRange(fetchedGroups.Select(g => new ValueTuple<string, string>(g.Id, g.DisplayName)));

                        if (fetchedGroups.NextPageRequest != null)
                        {
                            fetchedGroups = await fetchedGroups.NextPageRequest.GetAsync();
                        }
                        else
                        {
                            break;
                        }
                    }
                    var fetchedUsers = await graphClient.Users.Request().GetAsync();

                    var employees = new List<EmployeeRecord>();

                    var transaction = repo.Database.BeginTransaction();
                    while (true)
                    {
                        foreach (var fetchedUser in fetchedUsers)
                        {
                            if (string.IsNullOrWhiteSpace(fetchedUser.Mail))
                            {
                                continue;
                            }

                            var displayNameSplit = fetchedUser.DisplayName.Split(' ');
                            var employee = new EmployeeRecord
                            {
                                WorkEmail = fetchedUser.Mail,
                                FirstName = string.IsNullOrWhiteSpace(fetchedUser.GivenName) ? displayNameSplit[0] : fetchedUser.GivenName,
                                LastName = string.IsNullOrWhiteSpace(fetchedUser.Surname) ? (displayNameSplit.Length > 1 ? displayNameSplit.Skip(1).Aggregate((current, next) => current + " " + next) : string.Empty) : fetchedUser.Surname,
                            };

                            IUserMemberOfCollectionWithReferencesPage memberOfGroups = await graphClient.Users[fetchedUser.Id].MemberOf.Request().GetAsync();
                            if (memberOfGroups?.Count > 0)
                            {
                                foreach (var directoryObject in memberOfGroups)
                                {
                                    // We only want groups, so ignore DirectoryRole objects.
                                    if (directoryObject is Group)
                                    {
                                        var group = allGroups.Where(g => g.Id == directoryObject.Id).FirstOrDefault();
                                        if (group.DisplayName != null && group.DisplayName.ToLowerInvariant().StartsWith("gg_tableair"))
                                        {
                                            employee.Groups.Add(group.DisplayName);
                                        }
                                    }
                                }
                            }

                            Console.WriteLine($"Processing user {employee.WorkEmail.ToLower()}");
                            var user = repo.Users.Where(u => u.Email.ToLower() == employee.WorkEmail.ToLower()).FirstOrDefault();
                            Participation participation = null;
                            if (user != null)
                            {
                                // TODO skip admins

                                // Silly measure to eliminate duplicates, which happens in old db for whatever reason
                                var users = repo.Users.Where(u => u.Email.ToLower() == employee.WorkEmail.ToLower())
                                    .OrderByDescending(u => u.IsActive).OrderByDescending(u => u.Id).ToList();
                                if (users.Count > 1)
                                {
                                    for (var i = 1; i < users.Count; i++)
                                    {
                                        user.Email = user.Id + user.Email;
                                        user.IsActive = false;
                                        user.WasDeleted = true;
                                        repo.Update<User>(user);
                                    }

                                    user = users[0];
                                }
                                //

                                var customAttributes = System.Text.Json.JsonSerializer.Serialize(new { groups = System.Text.Json.JsonSerializer.Serialize(employee.Groups) });
                                dynamic userCustomAttributes = null;
                                if (user.CustomAttributes != null)
                                {
                                    System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonSerializer.Deserialize<dynamic>(user.CustomAttributes));
                                }
                                if (user.Email != employee.WorkEmail.ToLowerInvariant()
                                    || user.FirstName != employee.FirstName
                                    || user.LastName != employee.LastName
                                    || !user.IsActive
                                    || userCustomAttributes != customAttributes) // TODO compare groups only, leave other fields intact
                                {
                                    user.Email = employee.WorkEmail.ToLowerInvariant();
                                    user.FirstName = employee.FirstName;
                                    user.LastName = employee.LastName;
                                    user.IsActive = true; // TODO Delete other participations when changing from inactive to active
                                    user.CustomAttributes = customAttributes;

                                    user.LastSynced = DateTime.UtcNow;
                                }

                                participation = repo.Participations
                                    .Include(p => p.Team)
                                    .Where(p => p.User.Id == user.Id && p.Team.Id == TeamId).FirstOrDefault();
                                if (participation == null)
                                {
                                    // Marking it 
                                    user.IsExternallySynchronized = false; // TODO move this to participation, because its incorrect for multiteam logic
                                    user.DoNotSync = false; // This on for logical marking to not sync AFTER it becomes IsExternallySynchronized
                                    user.WasDeleted = false; // TODO move wasDeleted into participation to make it work correct for multiteam logic
                                }
                                repo.Update(user);
                            }
                            else
                            {
                                user = new User
                                {
                                    Email = employee.WorkEmail.ToLowerInvariant(),
                                    LastSynced = DateTime.UtcNow,

                                    Password = "Not set",
                                    IsSuperUser = false,
                                    IsStaff = false,
                                    FirstSeen = DateTime.UtcNow,
                                    WasDeleted = false,
                                    IsActive = true,
                                    FirstUsage = true, // No idea whats that
                                    IsKiosk = false,

                                    FirstName = employee.FirstName,
                                    LastName = employee.LastName,
                                    IsExternallySynchronized = false,
                                    DoNotSync = false, // This on for logical marking to not sync AFTER it becomes IsExternallySynchronized
                                    CustomAttributes = System.Text.Json.JsonSerializer.Serialize(new { groups = System.Text.Json.JsonSerializer.Serialize(employee.Groups) })
                                };
                                repo.Users.Add(user);
                                repo.SaveChanges(); // Bit overkill, but need to save when new, to get its id
                            }

                            if (participation == null)
                            {
                                participation = new Participation
                                {
                                    User = user,
                                    Team = repo.Teams.Single(t => t.Id == TeamId),
                                    Role = 1,
                                    IsAssistant = false
                                };
                                repo.Participations.Add(participation);
                                repo.SaveChanges(); // Bit overkill, but need to save when new, to get its id
                            }

                            // TODO remove from old groups, make group name a setting
                            foreach (var group in employee.Groups)
                            {
                                if (group.ToLowerInvariant() != "gg_tableair_syncgroup")
                                {
                                    var authGroup = repo.AuthGroups
                                        .Where(ag => ag.Name.ToLower() == $"{participation.Team.Id}:::{group}".ToLower())
                                        .FirstOrDefault();
                                    if (authGroup == null)
                                    {
                                        authGroup = new AuthGroup
                                        {
                                            Name = $"{participation.Team.Id}:::{group}"
                                        };
                                        repo.AuthGroups.Add(authGroup);
                                        repo.SaveChanges(); // Bit overkill, but need to save when new id and select later

                                        var teamGroupPermissions = new TeamGroupPermissions
                                        {
                                            GroupId = authGroup.Id,
                                            TeamId = MicrosoftSync.TeamId,
                                            Closed = true
                                        };
                                        repo.TeamGroupsPermissions.Add(teamGroupPermissions);
                                    }

                                    var accountGroup = repo.AccountGroups.Where(ag => ag.User.Id == user.Id && ag.Group.Id == authGroup.Id).FirstOrDefault();
                                    if (accountGroup == null)
                                    {
                                        accountGroup = new AccountGroup
                                        {
                                            User = user,
                                            Group = authGroup
                                        };
                                        repo.AccountGroups.Add(accountGroup);
                                    }
                                }
                            }
                        }

                        if (fetchedUsers.NextPageRequest != null)
                        {
                            fetchedUsers = await fetchedUsers.NextPageRequest.GetAsync();
                        }
                        else
                        {
                            break;
                        }
                    }

                    var notSynced = repo.Participations
                        .Include(p => p.User)
                        .Include(p => p.Team)
                        .Where(p => p.Team.Id == TeamId && !p.User.IsExternallySynchronized)
                        .Select(p => new { microsoftUser = EmployeeRecord.FromUser(p.User), user = p.User }).ToList();
                    notSynced.ForEach(u =>
                    {
                        u.user.IsExternallySynchronized = u.microsoftUser.Groups.Any(g => g.ToLowerInvariant() == "gg_tableair_syncgroup");

                        if (u.user.IsExternallySynchronized)
                        {
                            u.user.DoNotSync = false;
                            u.user.WasDeleted = u.user.DoNotSync; // TODO if multiteam user this will not work
                            u.user.LastSynced = DateTime.UtcNow;
                        }

                        repo.Update<User>(u.user);
                    });
                    repo.SaveChanges();
                    transaction.Commit();
                    // TODO Delete
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    IsSyncing = false;
                    lock (_actions)
                    {
                        foreach (var action in _actions)
                        {
                            action.Invoke();
                        }
                    }
                }


                lock (_actions)
                {
                    token = _ctsSleep.Token;
                }
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1800));

                await Task.Factory.StartNew(taskRunner);
            };
            Task.Factory.StartNew(taskRunner);
        }
    }
}
