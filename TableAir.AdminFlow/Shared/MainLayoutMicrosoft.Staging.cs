using MatBlazor;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TableAir.AdminFlow.Model;

namespace TableAir.AdminFlow.Shared
{
    public class MainLayoutMicrosoftStaging
    {
        public List<EmployeeRecord> Records { get; set; } = new List<EmployeeRecord>();
        public string Filter { get; set; } = "WorkEmail";
        public string FilterLabel { get; set; } = "Filter by employee email";
        public MatSortDirection SortDirection { get; set; } = MatSortDirection.None;

        public void Load()
        {
            var repo = new UsersRepository();
            Records = repo.Participations
                .Include(p => p.User)
                .Include(p => p.Team)
                .Where(p => p.Team.Id == MicrosoftSync.TeamId && !p.User.IsExternallySynchronized)
                .Select(p => EmployeeRecord.FromUser(p.User)).ToList();
            SortDirection = MatSortDirection.None;
        }

        public void Sort(MatSortChangedEvent sort)
        {
            if (!(sort == null || sort.Direction == MatSortDirection.None || string.IsNullOrEmpty(sort.SortId)))
            {
                Comparison<EmployeeRecord> comparison = null;
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
