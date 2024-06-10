using Newtonsoft.Json;

namespace Entities
{
    public class Client
    {
        public required string Id { get; set; }
        public required List<Entities.User> Users { get; set; }
        public required List<Entities.Group> Groups { get; set; }

        /// <summary>
        /// Indicate if Groups and Users it's empty
        /// </summary>
        /// <returns>Description of the return value.</returns>
        public bool IsEmpty()
        {
            return this.Groups.Count == 0 && this.Users.Count == 0;
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

        public Entities.User? GetUser(string identifier, bool byConnectionId)
        {
            if (this.Users.Count == 0)
            {
                throw new Exception(
                    string.Format("The current Client {0} not has Users", this.Id.ToString())
                );
            }

            if (byConnectionId)
            {
                return Users.FirstOrDefault(x => x.ConnectionId.Equals(identifier));
            }
            else
            {
                return Users.Where(x => x.Id.Equals(identifier)).FirstOrDefault();
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
                    User? u = this.GetUser(connectionId, true);

                    if (object.Equals(u, null))
                    {
                        throw new Exception($"User with connection id ${connectionId} not found.");
                    }

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
            User? user = this.GetUser(userGuid, false);

            if (object.Equals(user, null))
            {
                throw new Exception($"User with GUID ${userGuid} not found.");
            }

            GroupRemoveUser(user);

            this.Users.Remove(user);

            return true;
        }

        #endregion

        #region GROUP_CRUD

        /// <summary>
        /// Remove a user from all groups.
        /// </summary>
        /// <param name="user">The user to be removed from all groups</param>
        /// <returns>True if the user was removed</returns>
        public bool GroupRemoveUser(User user)
        {
            try
            {
                Group? group = GetUserGroup(user.ConnectionId);

                if (object.Equals(group, null)) { return false; }
                if (Groups.Count.Equals(0)) { return false; }

                List<User> groupMembers = group.Members.Where(m => !m.ConnectionId.Equals(user.ConnectionId)).ToList() ?? new List<User>();

                Group? groupFromUser = Groups.FirstOrDefault(w => w.Id.Equals(group.Id));

                if (!object.Equals(groupFromUser, null))
                {
                    groupFromUser.Members = groupMembers;
                }
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get any group where the client connectionId exists on it.
        /// </summary>
        /// <param name="connectionId">Client's connection id</param>
        /// <returns> Iterable list of groups</returns>
        public Group? GetUserGroup(string connectionId)
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

        /// <summary>
        /// Get all groups where the client connectionId exists on it.
        /// </summary>
        /// <param name="connectionId">Client's connection id</param>
        /// <returns> Iterable list of groups</returns>
        public List<Group> GetUserGroups(string connectionId)
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

        /// <summary>
        /// Get all client groups.
        /// </summary>
        /// <returns>A Iterable list of groups</returns>
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

        /// <summary>
        /// Get Client group from unique identifier
        /// </summary>
        /// <param name="groupGuid">Group unique identifier</param>
        /// <returns>A Group object if it exists</returns>
        public Group? GetGroup(string groupGuid)
        {
            if (string.IsNullOrEmpty(groupGuid)) { return null; }

            try
            {
                return Groups.FirstOrDefault(x => x.Id.Equals(groupGuid));
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Remove all client Groups
        /// </summary>
        public void RemoveGroups()
        {
            this.Groups = new List<Group>();
        }

        #endregion
    }
}
