using Microsoft.EntityFrameworkCore;

namespace RealtorApp.Domain.Models;

public partial class RealtorAppDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Global query filters for soft deletes - automatically filter out deleted records
        modelBuilder.Entity<Agent>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Client>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ClientInvitation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ClientsListing>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<AgentsListing>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Listing>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<File>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<FileType>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Link>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Message>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Property>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Task>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<TaskAttachment>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ThirdPartyContact>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Reminder>().HasQueryFilter(e => e.DeletedAt == null && (e.IsCompleted == null || e.IsCompleted == false ));
    }
}
