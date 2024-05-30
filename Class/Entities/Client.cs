namespace Entities
{
    public class Client
    {
        public string Id { get; set; }
        public List<Entities.User> Users { get; set; }
        public List<Entities.Group> Groups { get; set; }

        public bool IsEmpty()
        {
            return (this.Groups.Count == 0 && this.Users.Count == 0);
        }

        #region USER_CRUD

        public void AddUser(Entities.User user)
        {
            try
            {
                this.Users.Add(user);
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public Entities.User GetUser(string identifier, bool byConnectionId)
        {
            if (this.Users.Count == 0)
            {
                throw new Exception(
                    string.Format("The current Client {0} not has Users", this.Id.ToString())
                );
            }

            if (byConnectionId)
            {
                return this.Users.Where(x => x.ConnectionId.Equals(identifier)).FirstOrDefault();
            }
            else
            {
                return this.Users.Where(x => x.Id.Equals(identifier)).FirstOrDefault();
            }
        }

        /// <summary>
        /// Get a list of distinct users from each group
        /// </summary>
        /// <returns></returns>
        public List<User> GetUserDistinctFromGroup()
        {
            return this.Groups
                .SelectMany(x => x.Members)
                .Select(member => member)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Get a list of distinct users from all groups
        /// </summary>
        /// <returns></returns>
        public List<User> GetUserDistinctAll(User? me, bool includeMe = false)
        {
            List<User> result = new List<User>();

            List<User> usersFromGroups = this.Groups
                .SelectMany(x => x.Members)
                .Select(member => member)
                .Distinct()
                .ToList();

            List<string> connectionIds = usersFromGroups
                .Select(u => u.ConnectionId)
                .Distinct()
                .ToList();

            foreach (string connectionId in connectionIds)
            {
                try
                {
                    User u = this.GetUser(connectionId, true);

                    if (
                        !includeMe
                        && !object.Equals(me, null)
                        && u.ConnectionId.Equals(me.ConnectionId)
                    )
                    {
                        continue;
                    }

                    result.Add(u);
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            return result;
        }

        public void UpdateUser(User newUser)
        {
            int index = Users.FindIndex(u => u.Id.Equals(newUser.Id));
            this.Users[index] = newUser;
        }

        public bool DeleteUser(string userGuid)
        {
            Entities.User user = this.GetUser(userGuid, false);

            GroupRemoveUser(user);

            this.Users.Remove(user);

            return true;
        }

        #endregion

        #region GROUP_CRUD

        public bool GroupRemoveUser(User user)
        {
            try
            {
                Group g = GetUserGroup(user.ConnectionId);

                this.Groups.Where(w => w.Id.Equals(g.Id)).FirstOrDefault().Members = this.Groups
                    .Where(w => w.Id.Equals(g.Id))
                    .FirstOrDefault()
                    .Members.Where(m => !m.ConnectionId.Equals(user.ConnectionId))
                    .ToList();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Error eliminando usuario del grupo: " + ex.Message);
                throw;
            }

            return true;
        }

        public Entities.Group GetUserGroup(string connectionId)
        {
            try
            {
                return this.Groups.FirstOrDefault(
                    g => g.Members.Any(u => u.ConnectionId.Equals(connectionId))
                );
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public List<Entities.Group> GetUserGroups(string connectionId)
        {
            try
            {
                return this.Groups
                    .Where(g => g.Members.Any(u => u.ConnectionId.Equals(connectionId)))
                    .ToList();
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public List<Group> GetGroups()
        {
            try
            {
                return this.Groups;
            }
            catch (System.Exception ex)
            {
                throw new Exception("ERROR GetGroups: " + ex.Message);
            }
        }

        public void RemoveGroups()
        {
            this.Groups = new List<Group>();
        }

        #endregion
    }
}
