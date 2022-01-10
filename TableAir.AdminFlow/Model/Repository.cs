using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TableAir.AdminFlow.Model
{
    [Table("ta_team_external_link")]
    public class ExternalLink
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("provider")]
        public int Provider { get; set; }

        [Column("external_id")]
        public string ExternalId { get; set; }

        [Column("properties", TypeName = "jsonb")]
        public string Properties { get; set; } // {"sync_client": "41eb7b93-fdaf-44db-9819-30003a13d433", "initial_sync_params": {"import_users": true}}

        [ForeignKey("team_id")]
        public Team Team { get; set; }

        [Column("use_cron")]
        public bool UseCron { get; set; }
    }

    [Table("ta_team")]
    public class Team
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("url")]
        public string Subdomain { get; set; }

        [Column("integrations_admin_email")]
        public string IntegrationsAdminEmail { get; set; }

        [Column("invitation_email_subject")]
        public string InvitationEmailSubject { get; set; }

        [Column("invitation_email_template")]
        public string InvitationEmailTemplate { get; set; }
    }


    [Table("ta_account")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("is_superuser")]
        public bool IsSuperUser { get; set; }

        [Column("first_usage")]
        public bool FirstUsage { get; set; }

        [Column("is_kiosk")]
        public bool IsKiosk { get; set; }

        [Column("is_staff")]
        public bool IsStaff { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("was_deleted")]
        public bool WasDeleted { get; set; }

        [Column("is_externally_synchronized")]
        public bool IsExternallySynchronized { get; set; }

        [Column("do_not_sync")]
        public bool DoNotSync { get; set; }

        [Column("date_joined")]
        public DateTime? FirstSeen { get; set; }

        [Column("last_synced")]
        public DateTime? LastSynced { get; set; }

        [Column("custom_attributes", TypeName = "jsonb")]
        public string CustomAttributes { get; set; }

        public IEnumerable<Participation> participations { get; set; }
    }

    [Table("ta_participation")]
    public class Participation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("role")]
        public int Role { get; set; }

        [Column("is_assistant")]
        public bool IsAssistant { get; set; }

        [ForeignKey("team_id")]
        public Team Team { get; set; }

        [ForeignKey("account_id")]
        public User User { get; set; }
    }

    [Table("auth_group")]
    public class AuthGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }

    [Table("ta_account_groups")]
    public class AccountGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [ForeignKey("account_id")]
        public User User { get; set; }

        [ForeignKey("group_id")]
        public AuthGroup Group { get; set; }
    }

    [Table("ta_permissions_teamgroup")]
    public class TeamGroupPermissions
    {
        [Key]
        [Column("group_ptr_id")]
        public int GroupId { get; set; }

        [Column("team_id")]
        public int TeamId { get; set; }

        [Column("closed")]
        public bool Closed { get; set; }
    }


    public class UsersRepository : DbContext
    {
        public UsersRepository() : base()
        {
        }

        public DbSet<AuthGroup> AuthGroups { get; set; }
        public DbSet<ExternalLink> ExternalLinks { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Participation> Participations { get; set; }
        public DbSet<AccountGroup> AccountGroups { get; set; }
        public DbSet<TeamGroupPermissions> TeamGroupsPermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
         => optionsBuilder.UseNpgsql("Host=localhost;Database=postgres;Port=5432;Username=postgres;Password=jv06kgus");
         //=> optionsBuilder.UseNpgsql("Host=localhost;Database=ebdb;Username=postgres;Password=Vakare_234");
    }
}
