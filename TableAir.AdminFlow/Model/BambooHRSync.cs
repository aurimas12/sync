using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TableAir.AdminFlow.Model
{
    public class BambooHREmployeeRecord
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("preferredName")]
        public string PreferredName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("jobTitle")]
        public string JobTitle { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("workPhone")]
        public string WorkPhone { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("workEmail")]
        public string WorkEmail { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("department")]
        public string Department { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("location")]
        public string Location { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("division")]
        public string Division { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("linkedIn")]
        public string LinkedIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("workPhoneExtension")]
        public string WorkPhoneExtension { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("supervisor")]
        public string Supervisor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("photoUploaded")]
        public bool? PhotoUploaded { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("photoUrl")]
        public string PhotoUrl { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("canUploadPhoto")]
        public int? CanUploadPhoto { get; set; }

        private List<(DateTime, string)> _history = new List<(DateTime, string)>();
        public List<(DateTime, string)> History => _history;

        public static BambooHREmployeeRecord FromUser(User user)
        {
            var _ = new BambooHREmployeeRecord
            {
                Id = user.Id.ToString(),
                WorkEmail = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
            if (!string.IsNullOrWhiteSpace(user.CustomAttributes))
            {
                var customAttr = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(user.CustomAttributes);

                if (customAttr.ContainsKey("location"))
                {
                    _.Location = customAttr["location"];
                }

                if (customAttr.ContainsKey("department"))
                {
                    _.Department = customAttr["department"];
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

    public class BambooHREmployeesContainer
    {
        [System.Text.Json.Serialization.JsonPropertyName("employees")]
        public IEnumerable<BambooHREmployeeRecord> Employees { get; set; }
    }

    public class BambooHRSync
    {
        public static bool IsSyncing = false;
        public static List<Action> _actions = new List<Action>();
        
        public static readonly int TeamId = 45;
        
        private static CancellationTokenSource _ctsSleep = new CancellationTokenSource();

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
                IsSyncing = true;
                try
                {
                    var client = new HttpClient();
                    client.BaseAddress = new Uri("https://api.bamboohr.com");
                    client.DefaultRequestHeaders
                          .Accept
                          .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "thiscodeisoutdatedusemicrosoftone", "x"))));

                    var response = client.SendAsync(
                        new HttpRequestMessage(
                            HttpMethod.Get,
                            "/api/gateway.php/aircall/v1/employees/directory")).Result;
                    var employees = System.Text.Json.JsonSerializer.Deserialize<BambooHREmployeesContainer>(response.Content.ReadAsStringAsync().Result);

                    var repo = new UsersRepository();
                    var transaction = repo.Database.BeginTransaction();
                    foreach (var employee in employees.Employees)
                    {
                        if (string.IsNullOrWhiteSpace(employee.WorkEmail))
                        {
                            continue;
                        }

                        //if (employee.FirstName.Length + employee.LastName.Length < 5) continue;

                        //var newFirstName = "";
                        //for (var i = 0; i < employee.FirstName.Length; i++)
                        //{
                        //    if (employee.FirstName[i] >= 'a' && employee.FirstName[i] <= 'x' || employee.FirstName[i] >= 'A' && employee.FirstName[i] <= 'X')
                        //    {
                        //        newFirstName += (char)(employee.FirstName[i] + 1);
                        //    }
                        //    else
                        //    {
                        //        newFirstName += (char)(employee.FirstName[i]);
                        //    }
                        //}
                        //var oldFirstName = employee.FirstName;
                        //employee.FirstName = newFirstName;

                        //var newLastName = "";
                        //for (var i = 0; i < employee.LastName.Length; i++)
                        //{
                        //    if (employee.LastName[i] >= 'a' && employee.LastName[i] <= 'x' || employee.LastName[i] >= 'A' && employee.LastName[i] <= 'X')
                        //    {
                        //        newLastName += (char)(employee.LastName[i] + 1);
                        //    }
                        //    else
                        //    {
                        //        newLastName += (char)(employee.LastName[i]);
                        //    }
                        //}
                        //var oldLastName = employee.LastName;
                        //employee.LastName = newLastName;

                        //var newWorkEmail = "";
                        //for (var i = 0; i < employee.WorkEmail.Length; i++)
                        //{
                        //    if (employee.WorkEmail[i] >= 'a' && employee.WorkEmail[i] <= 'x' || employee.WorkEmail[i] >= 'A' && employee.WorkEmail[i] <= 'X')
                        //    {
                        //        newWorkEmail += (char)(employee.WorkEmail[i] + 1);
                        //    }
                        //    else
                        //    {
                        //        newWorkEmail += (char)(employee.WorkEmail[i]);
                        //    }
                        //}
                        //var oldWorkEmail = employee.WorkEmail;
                        //employee.WorkEmail = newWorkEmail;

                        Console.WriteLine($"Processing user {employee.WorkEmail.ToLower()}");
                        var user = repo.Users.Where(u => u.Email.ToLower() == employee.WorkEmail.ToLower()).FirstOrDefault();
                        Participation participation = null;
                        if (user != null)
                        {
                            // TODO skip admins
                            // Silly measure to eliminate duplicates
                            var users = repo.Users.Where(u => u.Email.ToLower() == employee.WorkEmail.ToLower()).OrderBy(u => u.Id).ToList();
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

                            var customAttributes = System.Text.Json.JsonSerializer.Serialize(new { location = employee.Location, department = employee.Department });
                            if (user.Email != employee.WorkEmail.ToLowerInvariant()
                                || user.FirstName != employee.FirstName
                                || user.LastName != employee.LastName
                                || !user.IsActive
                                || user.CustomAttributes != customAttributes)
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
                                .Where(p => p.User.Id == user.Id && p.Team.Id == BambooHRSync.TeamId).FirstOrDefault();
                            if (participation == null)
                            {
                                // Do syncronization and undelete only on team assigment
                                user.IsExternallySynchronized = false;
                                user.WasDeleted = false;
                            }
                            repo.Update<User>(user);
                        }
                        else
                        {
                            user = new User
                            {
                                Email = employee.WorkEmail.ToLowerInvariant(),

                                Password = "Not set",
                                IsSuperUser = false,
                                IsStaff = false,
                                FirstSeen = DateTime.UtcNow,
                                WasDeleted = true, // Make user invisible at start
                                IsActive = true,
                                FirstUsage = true, // No idea whats that
                                IsKiosk = false,

                                FirstName = employee.FirstName,
                                LastName = employee.LastName,
                                IsExternallySynchronized = false,
                                CustomAttributes = System.Text.Json.JsonSerializer.Serialize(new { location = employee.Location, department = employee.Department })
                            };
                            repo.Users.Add(user);
                            repo.SaveChanges(); // Bit overkill, but need to save when new, to get id and select later
                        }

                        if (participation == null)
                        {
                            participation = new Participation
                            {
                                User = user,
                                Team = repo.Teams.Single(t => t.Id == BambooHRSync.TeamId),
                                Role = 1,
                                IsAssistant = false
                            };
                            repo.Participations.Add(participation);
                            repo.SaveChanges(); // Bit overkill, but need to save when new, to get id and select later
                        }

                        // TODO remove from old groups
                        if (!string.IsNullOrWhiteSpace(employee.Department))
                        {
                            var authGroup = repo.AuthGroups
                                .Where(ag => ag.Name.ToLower() == $"{participation.Team.Id}:::{employee.Department}".ToLower())
                                .FirstOrDefault();
                            if (authGroup == null)
                            {
                                authGroup = new AuthGroup
                                {
                                    Name = $"{participation.Team.Id}:::{employee.Department}"
                                };
                                repo.AuthGroups.Add(authGroup);
                                repo.SaveChanges(); // Bit overkill, but need to save when new id and select later

                                var teamGroupPermissions = new TeamGroupPermissions
                                {
                                    GroupId = authGroup.Id,
                                    TeamId = BambooHRSync.TeamId,
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

                    var notSynced = repo.Participations
                        .Include(p => p.User)
                        .Include(p => p.Team)
                        .Where(p => p.Team.Id == TeamId && !p.User.IsExternallySynchronized)
                        .Select(p => new { bambooUser = BambooHREmployeeRecord.FromUser(p.User), user = p.User }).ToList();
                    notSynced.ForEach(u =>
                    {
                        if (!string.IsNullOrWhiteSpace(u.bambooUser.Location))
                        {
                            u.user.IsExternallySynchronized = true;

                            u.user.DoNotSync = string.Compare(u.bambooUser.Location, "paris", StringComparison.InvariantCultureIgnoreCase) != 0;
                            u.user.WasDeleted = u.user.DoNotSync; // TODO if multiteam user this will not work
                            u.user.LastSynced = DateTime.UtcNow;

                            repo.Update<User>(u.user);
                        }
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

                CancellationToken token;
                lock (_actions)
                {
                    token = _ctsSleep.Token;
                }
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1800));

                Task.Factory.StartNew(taskRunner);
            };
            Task.Factory.StartNew(taskRunner);
        }
    }
}
