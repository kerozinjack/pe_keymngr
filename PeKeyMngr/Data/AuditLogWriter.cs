using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    /// olyan esemenyekhez amelyeknél nincs crud muvelet pl login/logout
    public sealed class AuditLogWriter(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        public async Task WriteAsync(
            string subsystem,            
            string message,              
            Severity severity = Severity.Info,
            CancellationToken cancellationToken = default)
        {
       
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync(cancellationToken);

            db.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Severity = severity,
                Subsystem = subsystem,
                Text = message.Replace('\r', ' ').Replace('\n', ' ')
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
