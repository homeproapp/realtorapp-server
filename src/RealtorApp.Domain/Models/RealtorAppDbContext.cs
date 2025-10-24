using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RealtorApp.Domain.Models;

public partial class RealtorAppDbContext : DbContext
{
    public RealtorAppDbContext()
    {
    }

    public RealtorAppDbContext(DbContextOptions<RealtorAppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<AgentsListing> AgentsListings { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientInvitation> ClientInvitations { get; set; }

    public virtual DbSet<ClientInvitationsProperty> ClientInvitationsProperties { get; set; }

    public virtual DbSet<ClientsListing> ClientsListings { get; set; }

    public virtual DbSet<ContactAttachment> ContactAttachments { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<FileType> FileTypes { get; set; }

    public virtual DbSet<FilesTask> FilesTasks { get; set; }

    public virtual DbSet<Link> Links { get; set; }

    public virtual DbSet<Listing> Listings { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageRead> MessageReads { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<PropertyInvitation> PropertyInvitations { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Reminder> Reminders { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskAttachment> TaskAttachments { get; set; }

    public virtual DbSet<TaskTitle> TaskTitles { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<ThirdPartyContact> ThirdPartyContacts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=test-run49;Username=test;Password=test");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("citext")
            .HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("agents_pkey");

            entity.ToTable("agents");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EmailValidated)
                .HasDefaultValue(false)
                .HasColumnName("email_validated");
            entity.Property(e => e.IsTeamLead)
                .HasDefaultValue(false)
                .HasColumnName("is_team_lead");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Team).WithMany(p => p.Agents)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("agents_team_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.Agent)
                .HasForeignKey<Agent>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("agents_user_id_fkey");
        });

        modelBuilder.Entity<AgentsListing>(entity =>
        {
            entity.HasKey(e => e.AgentListingId).HasName("agents_listings_pkey");

            entity.ToTable("agents_listings");

            entity.Property(e => e.AgentListingId).HasColumnName("agent_listing_id");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Agent).WithMany(p => p.AgentsListings)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("agents_listings_agent_id_fkey");

            entity.HasOne(d => d.Listing).WithMany(p => p.AgentsListings)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("agents_listings_listing_id_fkey");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("attachments_pkey");

            entity.ToTable("attachments");

            entity.HasIndex(e => e.MessageId, "ix_attachments_message_id");

            entity.Property(e => e.AttachmentId).HasColumnName("attachment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Message).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.MessageId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attachments_message_id_fkey");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("clients_pkey");

            entity.ToTable("clients");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.MaritalStatus).HasColumnName("marital_status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.YearlyIncome).HasColumnName("yearly_income");

            entity.HasOne(d => d.User).WithOne(p => p.Client)
                .HasForeignKey<Client>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_user_id_fkey");
        });

        modelBuilder.Entity<ClientInvitation>(entity =>
        {
            entity.HasKey(e => e.ClientInvitationId).HasName("client_invitations_pkey");

            entity.ToTable("client_invitations");

            entity.HasIndex(e => e.InvitationToken, "client_invitations_invitation_token_key").IsUnique();

            entity.HasIndex(e => e.ExpiresAt, "ix_client_invitations_expires_at").HasFilter("((deleted_at IS NULL) AND (accepted_at IS NULL))");

            entity.HasIndex(e => e.InvitationToken, "ix_client_invitations_token_active").HasFilter("((deleted_at IS NULL) AND (accepted_at IS NULL))");

            entity.HasIndex(e => e.ClientEmail, "ux_client_invitations_email_active")
                .IsUnique()
                .HasFilter("((deleted_at IS NULL) AND (accepted_at IS NULL))");

            entity.Property(e => e.ClientInvitationId).HasColumnName("client_invitation_id");
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(e => e.ClientEmail)
                .HasColumnType("citext")
                .HasColumnName("client_email");
            entity.Property(e => e.ClientFirstName).HasColumnName("client_first_name");
            entity.Property(e => e.ClientLastName).HasColumnName("client_last_name");
            entity.Property(e => e.ClientPhone).HasColumnName("client_phone");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedUserId).HasColumnName("created_user_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.InvitationToken)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("invitation_token");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedUser).WithMany(p => p.ClientInvitations)
                .HasForeignKey(d => d.CreatedUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("client_invitations_created_user_id_fkey");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.ClientInvitations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("client_invitations_invited_by_fkey");
        });

        modelBuilder.Entity<ClientInvitationsProperty>(entity =>
        {
            entity.HasKey(e => e.ClientInvitationPropertyId).HasName("client_invitations_properties_pkey");

            entity.ToTable("client_invitations_properties");

            entity.HasIndex(e => new { e.ClientInvitationId, e.PropertyInvitationId }, "ux_client_invitations_properties_active")
                .IsUnique()
                .HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.ClientInvitationPropertyId).HasColumnName("client_invitation_property_id");
            entity.Property(e => e.ClientInvitationId).HasColumnName("client_invitation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.PropertyInvitationId).HasColumnName("property_invitation_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ClientInvitation).WithMany(p => p.ClientInvitationsProperties)
                .HasForeignKey(d => d.ClientInvitationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("client_invitations_properties_client_invitation_id_fkey");

            entity.HasOne(d => d.PropertyInvitation).WithMany(p => p.ClientInvitationsProperties)
                .HasForeignKey(d => d.PropertyInvitationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("client_invitations_properties_property_invitation_id_fkey");
        });

        modelBuilder.Entity<ClientsListing>(entity =>
        {
            entity.HasKey(e => e.ClientListingId).HasName("clients_listings_pkey");

            entity.ToTable("clients_listings");

            entity.Property(e => e.ClientListingId).HasColumnName("client_listing_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientsListings)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_listings_client_id_fkey");

            entity.HasOne(d => d.Listing).WithMany(p => p.ClientsListings)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_listings_listing_id_fkey");
        });

        modelBuilder.Entity<ContactAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("contact_attachments_pkey");

            entity.ToTable("contact_attachments");

            entity.Property(e => e.AttachmentId)
                .ValueGeneratedNever()
                .HasColumnName("attachment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ThirdPartyContactId).HasColumnName("third_party_contact_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Attachment).WithOne(p => p.ContactAttachment)
                .HasForeignKey<ContactAttachment>(d => d.AttachmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("contact_attachments_attachment_id_fkey");

            entity.HasOne(d => d.ThirdPartyContact).WithMany(p => p.ContactAttachments)
                .HasForeignKey(d => d.ThirdPartyContactId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("contact_attachments_third_party_contact_id_fkey");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ListingId).HasName("conversations_pkey");

            entity.ToTable("conversations");

            entity.Property(e => e.ListingId)
                .ValueGeneratedNever()
                .HasColumnName("listing_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.Nickname).HasColumnName("nickname");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Image).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("conversations_image_id_fkey");

            entity.HasOne(d => d.Listing).WithOne(p => p.Conversation)
                .HasForeignKey<Conversation>(d => d.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("conversations_listing_id_fkey");
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("files_pkey");

            entity.ToTable("files");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.FileExtension).HasColumnName("file_extension");
            entity.Property(e => e.FileTypeId).HasColumnName("file_type_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Uuid).HasColumnName("uuid");

            entity.HasOne(d => d.FileType).WithMany(p => p.Files)
                .HasForeignKey(d => d.FileTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("files_file_type_id_fkey");
        });

        modelBuilder.Entity<FileType>(entity =>
        {
            entity.HasKey(e => e.FileTypeId).HasName("file_types_pkey");

            entity.ToTable("file_types");

            entity.HasIndex(e => e.Name, "file_types_name_key").IsUnique();

            entity.Property(e => e.FileTypeId).HasColumnName("file_type_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<FilesTask>(entity =>
        {
            entity.HasKey(e => e.FileTaskId).HasName("files_tasks_pkey");

            entity.ToTable("files_tasks");

            entity.HasIndex(e => new { e.FileId, e.TaskId }, "ux_files_tasks_pair").IsUnique();

            entity.Property(e => e.FileTaskId).HasColumnName("file_task_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.File).WithMany(p => p.FilesTasks)
                .HasForeignKey(d => d.FileId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("files_tasks_file_id_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.FilesTasks)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("files_tasks_task_id_fkey");
        });

        modelBuilder.Entity<Link>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("links_pkey");

            entity.ToTable("links");

            entity.HasIndex(e => e.TaskId, "ix_links_task_id");

            entity.Property(e => e.LinkId).HasColumnName("link_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsReferral)
                .HasDefaultValue(false)
                .HasColumnName("is_referral");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.TimesUsed).HasColumnName("times_used");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.Task).WithMany(p => p.Links)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("links_task_id_fkey");
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.HasKey(e => e.ListingId).HasName("listings_pkey");

            entity.ToTable("listings");

            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.Bathrooms).HasColumnName("bathrooms");
            entity.Property(e => e.Bedrooms).HasColumnName("bedrooms");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("currency_code");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.ExternalSource).HasColumnName("external_source");
            entity.Property(e => e.ListPrice)
                .HasPrecision(12, 2)
                .HasColumnName("list_price");
            entity.Property(e => e.ListedAt).HasColumnName("listed_at");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.PropertyType).HasColumnName("property_type");
            entity.Property(e => e.SalePrice)
                .HasPrecision(12, 2)
                .HasColumnName("sale_price");
            entity.Property(e => e.SquareFeet).HasColumnName("square_feet");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.YearBuilt).HasColumnName("year_built");

            entity.HasOne(d => d.Property).WithMany(p => p.Listings)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("listings_property_id_fkey");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("messages_pkey");

            entity.ToTable("messages");

            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "ix_messages_conversation_created");

            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("messages_conversation_id_fkey");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("messages_sender_id_fkey");
        });

        modelBuilder.Entity<MessageRead>(entity =>
        {
            entity.HasKey(e => e.MessageReadId).HasName("message_reads_pkey");

            entity.ToTable("message_reads");

            entity.Property(e => e.MessageReadId).HasColumnName("message_read_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MessageId).HasColumnName("message_id");
            entity.Property(e => e.ReaderId).HasColumnName("reader_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Message).WithMany(p => p.MessageReads)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("message_reads_message_id_fkey");

            entity.HasOne(d => d.Reader).WithMany(p => p.MessageReads)
                .HasForeignKey(d => d.ReaderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("message_reads_reader_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.NotificationText).HasColumnName("notification_text");
            entity.Property(e => e.NotificationType).HasColumnName("notification_type");
            entity.Property(e => e.ReferencingObjectId).HasColumnName("referencing_object_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("properties_pkey");

            entity.ToTable("properties");

            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PropertyInvitation>(entity =>
        {
            entity.HasKey(e => e.PropertyInvitationId).HasName("property_invitations_pkey");

            entity.ToTable("property_invitations");

            entity.Property(e => e.PropertyInvitationId).HasColumnName("property_invitation_id");
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("country_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedListingId).HasColumnName("created_listing_id");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedListing).WithMany(p => p.PropertyInvitations)
                .HasForeignKey(d => d.CreatedListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("property_invitations_created_listing_id_fkey");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.PropertyInvitations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("property_invitations_invited_by_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.ExpiresAt, "ix_refresh_tokens_expires_at");

            entity.HasIndex(e => e.TokenHash, "ix_refresh_tokens_token_hash").HasFilter("(revoked_at IS NULL)");

            entity.HasIndex(e => e.UserId, "ix_refresh_tokens_user_id");

            entity.Property(e => e.RefreshTokenId).HasColumnName("refresh_token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.ReminderId).HasName("reminders_pkey");

            entity.ToTable("reminders");

            entity.HasIndex(e => e.RemindAt, "ix_reminders_remind_at");

            entity.Property(e => e.ReminderId).HasColumnName("reminder_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.ReferencingObjectId).HasColumnName("referencing_object_id");
            entity.Property(e => e.RemindAt).HasColumnName("remind_at");
            entity.Property(e => e.ReminderText).HasColumnName("reminder_text");
            entity.Property(e => e.ReminderType).HasColumnName("reminder_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Reminders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("reminders_user_id_fkey");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("tasks_pkey");

            entity.ToTable("tasks");

            entity.HasIndex(e => e.FollowUpDate, "ix_tasks_follow_up_date");

            entity.HasIndex(e => e.ListingId, "ix_tasks_listing_id");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EstimatedCost).HasColumnName("estimated_cost");
            entity.Property(e => e.FollowUpDate).HasColumnName("follow_up_date");
            entity.Property(e => e.ListingId).HasColumnName("listing_id");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Room).HasColumnName("room");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Listing).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ListingId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("tasks_listing_id_fkey");
        });

        modelBuilder.Entity<TaskAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("task_attachments_pkey");

            entity.ToTable("task_attachments");

            entity.Property(e => e.AttachmentId)
                .ValueGeneratedNever()
                .HasColumnName("attachment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Attachment).WithOne(p => p.TaskAttachment)
                .HasForeignKey<TaskAttachment>(d => d.AttachmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("task_attachments_attachment_id_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAttachments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("task_attachments_task_id_fkey");
        });

        modelBuilder.Entity<TaskTitle>(entity =>
        {
            entity.HasKey(e => e.TaskTitleId).HasName("task_titles_pkey");

            entity.ToTable("task_titles");

            entity.HasIndex(e => e.TaskTitle1, "task_titles_task_title_key").IsUnique();

            entity.Property(e => e.TaskTitleId).HasColumnName("task_title_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskTitle1)
                .HasColumnType("citext")
                .HasColumnName("task_title");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("teams_pkey");

            entity.ToTable("teams");

            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Realty).HasColumnName("realty");
            entity.Property(e => e.TeamName).HasColumnName("team_name");
            entity.Property(e => e.TeamSite).HasColumnName("team_site");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ThirdPartyContact>(entity =>
        {
            entity.HasKey(e => e.ThirdPartyContactId).HasName("third_party_contacts_pkey");

            entity.ToTable("third_party_contacts");

            entity.HasIndex(e => e.AgentId, "ix_third_party_contacts_agent_id");

            entity.Property(e => e.ThirdPartyContactId).HasColumnName("third_party_contact_id");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Trade).HasColumnName("trade");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Agent).WithMany(p => p.ThirdPartyContacts)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("third_party_contacts_agent_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Uuid, "users_uuid_key").IsUnique();

            entity.HasIndex(e => e.Email, "ux_users_email_active")
                .IsUnique()
                .HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasColumnType("citext")
                .HasColumnName("email");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.ProfileImageId).HasColumnName("profile_image_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Uuid).HasColumnName("uuid");

            entity.HasOne(d => d.ProfileImage).WithMany(p => p.Users)
                .HasForeignKey(d => d.ProfileImageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("users_profile_image_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
