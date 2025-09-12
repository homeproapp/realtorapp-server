using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RealtorApp.Contracts.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientsConversation> ClientsConversations { get; set; }

    public virtual DbSet<ClientsProperty> ClientsProperties { get; set; }

    public virtual DbSet<ContactAttachment> ContactAttachments { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<FileType> FileTypes { get; set; }

    public virtual DbSet<FilesTask> FilesTasks { get; set; }

    public virtual DbSet<Link> Links { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskAttachment> TaskAttachments { get; set; }

    public virtual DbSet<ThirdPartyContact> ThirdPartyContacts { get; set; }

    public virtual DbSet<User> Users { get; set; }

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
            entity.Property(e => e.Brokerage).HasColumnName("brokerage");
            entity.Property(e => e.BrokerageTeam).HasColumnName("brokerage_team");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.EmailValidated)
                .HasDefaultValue(false)
                .HasColumnName("email_validated");
            entity.Property(e => e.TeamLead).HasColumnName("team_lead");
            entity.Property(e => e.TeamWebsite).HasColumnName("team_website");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne(p => p.Agent)
                .HasForeignKey<Agent>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("agents_user_id_fkey");
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
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
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

        modelBuilder.Entity<ClientsConversation>(entity =>
        {
            entity.HasKey(e => e.ClientConversationId).HasName("clients_conversations_pkey");

            entity.ToTable("clients_conversations");

            entity.HasIndex(e => e.ClientId, "ix_clients_conversations_client_id");

            entity.Property(e => e.ClientConversationId).HasColumnName("client_conversation_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientsConversations)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_conversations_client_id_fkey");

            entity.HasOne(d => d.Conversation).WithMany(p => p.ClientsConversations)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_conversations_conversation_id_fkey");
        });

        modelBuilder.Entity<ClientsProperty>(entity =>
        {
            entity.HasKey(e => e.ClientPropertyId).HasName("clients_properties_pkey");

            entity.ToTable("clients_properties");

            entity.HasIndex(e => e.AgentId, "ix_cp_agent_active").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.ClientId, "ix_cp_client_active").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => e.PropertyId, "ix_cp_property_active").HasFilter("(deleted_at IS NULL)");

            entity.HasIndex(e => new { e.PropertyId, e.ClientId }, "ux_cp_active_property_client")
                .IsUnique()
                .HasFilter("(deleted_at IS NULL)");

            entity.Property(e => e.ClientPropertyId).HasColumnName("client_property_id");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Agent).WithMany(p => p.ClientsProperties)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_properties_agent_id_fkey");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientsProperties)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_properties_client_id_fkey");

            entity.HasOne(d => d.Property).WithMany(p => p.ClientsProperties)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("clients_properties_property_id_fkey");
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
            entity.HasKey(e => e.ConversationId).HasName("conversations_pkey");

            entity.ToTable("conversations");

            entity.HasIndex(e => e.AgentId, "ix_conversations_agent_id");

            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.AgentId).HasColumnName("agent_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Agent).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.AgentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("conversations_agent_id_fkey");
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
            entity.HasKey(e => e.Id).HasName("file_types_pkey");

            entity.ToTable("file_types");

            entity.HasIndex(e => e.Name, "file_types_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
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
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Url).HasColumnName("url");

            entity.HasOne(d => d.Task).WithMany(p => p.Links)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("links_task_id_fkey");
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

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropertyId).HasName("properties_pkey");

            entity.ToTable("properties");

            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2");
            entity.Property(e => e.Bathrooms).HasColumnName("bathrooms");
            entity.Property(e => e.Bedrooms).HasColumnName("bedrooms");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.ClosingAt).HasColumnName("closing_at");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("country_code");
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
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.ListPrice)
                .HasPrecision(12, 2)
                .HasColumnName("list_price");
            entity.Property(e => e.ListedAt).HasColumnName("listed_at");
            entity.Property(e => e.Longitude)
                .HasPrecision(9, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.PropertyType).HasColumnName("property_type");
            entity.Property(e => e.Region).HasColumnName("region");
            entity.Property(e => e.SalePrice)
                .HasPrecision(12, 2)
                .HasColumnName("sale_price");
            entity.Property(e => e.SquareFeet).HasColumnName("square_feet");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.YearBuilt).HasColumnName("year_built");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("tasks_pkey");

            entity.ToTable("tasks");

            entity.HasIndex(e => e.PropertyId, "ix_tasks_property_id");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.Room).HasColumnName("room");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Property).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.PropertyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("tasks_property_id_fkey");
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
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Uuid).HasColumnName("uuid");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
