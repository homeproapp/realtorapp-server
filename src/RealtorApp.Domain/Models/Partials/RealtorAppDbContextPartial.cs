using Microsoft.EntityFrameworkCore;

namespace RealtorApp.Domain.Models;

public partial class RealtorAppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Global query filters for soft deletes - automatically filter out deleted records
        modelBuilder.Entity<Agent>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Attachment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Client>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ClientInvitation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ClientInvitationsProperty>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ClientsProperty>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ContactAttachment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ConversationsProperty>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<File>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<FileType>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<FilesTask>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Link>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Message>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Property>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<PropertyInvitation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Task>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<TaskAttachment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ThirdPartyContact>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
    }
}