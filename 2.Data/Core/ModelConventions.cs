using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Data.Core
{
    /// <summary>اتفاقيات النموذج العامّة</summary>
    public static class ModelConventions
    {
        /// <summary>المحذوف منطقياً خارج كل استعلام</summary>
        public static void HideDeleted(ModelBuilder model)
        {
            foreach (var entity in model.Model.GetEntityTypes()
                         .Where(e => e.FindProperty("IsDeleted")?.ClrType == typeof(bool)))
            {
                var row = Expression.Parameter(entity.ClrType, "row");
                var deleted = Expression.Call(typeof(EF), nameof(EF.Property), new[] { typeof(bool) },
                                              row, Expression.Constant("IsDeleted"));
                entity.SetQueryFilter(Expression.Lambda(Expression.Not(deleted), row));
            }
        }

        /// <summary>أختام الإنشاء والتعديل والحذف</summary>
        public static void Stamp(ChangeTracker tracker)
        {
            var now = DateTime.Now;
            var user = AppSession.Username ?? "Admin";

            foreach (var entry in tracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    Fill(entry, "CreatedAt", now);
                    Fill(entry, "UpdatedAt", now);
                    Fill(entry, "CreatedBy", user);
                }
                else if (entry.State == EntityState.Modified)
                {
                    Put(entry, "UpdatedAt", now);
                    Put(entry, "UpdatedBy", user);

                    if (entry.Metadata.FindProperty("IsDeleted") != null
                        && entry.Property("IsDeleted").CurrentValue is true
                        && entry.Property("IsDeleted").OriginalValue is not true)
                    {
                        Put(entry, "DeletedAt", now);
                        Fill(entry, "DeletedBy", user);
                    }
                }
            }
        }

        private static void Fill(EntityEntry entry, string property, object value)
        {
            if (entry.Metadata.FindProperty(property) == null) return;

            var current = entry.Property(property).CurrentValue;
            if (current == null || Equals(current, default(DateTime)) || current is "") entry.Property(property).CurrentValue = value;
        }

        private static void Put(EntityEntry entry, string property, object value)
        {
            if (entry.Metadata.FindProperty(property) != null) entry.Property(property).CurrentValue = value;
        }
    }

    public partial class PrimeDbContext
    {
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ModelConventions.Stamp(ChangeTracker);
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
    }
}
