using MatBlazor;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TableAir.AdminFlow.Model;

namespace TableAir.AdminFlow.Shared
{
    public class MainLayoutStaging
    {
        public List<BambooHREmployeeRecord> Records { get; set; } = new List<BambooHREmployeeRecord>();
        public string Filter { get; set; } = "Location";
        public string FilterLabel { get; set; } = "Filter by employee location";
        public MatSortDirection SortDirection { get; set; } = MatSortDirection.None;

        public void Load()
        {
            var repo = new UsersRepository();
            Records = repo.Participations
                .Include(p => p.User)
                .Include(p => p.Team)
                .Where(p => p.Team.Id == BambooHRSync.TeamId && !p.User.IsExternallySynchronized)
                .Select(p => BambooHREmployeeRecord.FromUser(p.User)).ToList();
            SortDirection = MatSortDirection.None;
        }

        public void Sort(MatSortChangedEvent sort)
        {
            if (!(sort == null || sort.Direction == MatSortDirection.None || string.IsNullOrEmpty(sort.SortId)))
            {
                Comparison<BambooHREmployeeRecord> comparison = null;
                switch (sort.SortId)
                {
                    case "email":
                        Filter = "WorkEmail";
                        FilterLabel = "Filter by employee email";

                        comparison = (s1, s2) => string.Compare(s1.WorkEmail, s2.WorkEmail, StringComparison.InvariantCultureIgnoreCase);
                        break;

                    case "firstName":
                        Filter = "FirstName";
                        FilterLabel = "Filter by employee name";

                        comparison = (s1, s2) => string.Compare(s1.FirstName, s2.FirstName, StringComparison.InvariantCultureIgnoreCase);
                        break;

                    case "lastName":
                        Filter = "LastName";
                        FilterLabel = "Filter by employee surname";

                        comparison = (s1, s2) => string.Compare(s1.LastName, s2.LastName, StringComparison.InvariantCultureIgnoreCase);
                        break;

                    case "location":
                        Filter = "Location";
                        FilterLabel = "Filter by employee location";

                        comparison = (s1, s2) => string.Compare(s1.Location, s2.Location, StringComparison.InvariantCultureIgnoreCase);
                        break;

                    case "department":
                        Filter = "Department";
                        FilterLabel = "Filter by employee department";

                        comparison = (s1, s2) => string.Compare(s1.Department, s2.Department, StringComparison.InvariantCultureIgnoreCase);
                        break;
                }

                if (comparison != null)
                {
                    if (sort.Direction == MatSortDirection.Desc)
                    {
                        Records.Sort((s1, s2) => -1 * comparison(s1, s2));
                    }
                    else
                    {
                        Records.Sort(comparison);
                    }
                }
            }
        }
    }
}
