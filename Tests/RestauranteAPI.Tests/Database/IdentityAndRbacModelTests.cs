using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RestauranteAPI.Data;
using RestauranteAPI.Models;

namespace RestauranteAPI.Tests.Database;

public sealed class IdentityAndRbacModelTests
{
    private readonly IModel _model;

    public IdentityAndRbacModelTests()
    {
        var options = new DbContextOptionsBuilder<MyAppDbContext>()
            .UseSqlServer("Server=model.invalid;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var context = new MyAppDbContext(options);
        _model = context.Model;
    }

    [Fact]
    public void Identity_and_rbac_model_builds_with_all_expand_entities()
    {
        Assert.NotNull(_model.FindEntityType(typeof(Person)));
        Assert.NotNull(_model.FindEntityType(typeof(ClientProfile)));
        Assert.NotNull(_model.FindEntityType(typeof(UserAccount)));
        Assert.NotNull(_model.FindEntityType(typeof(Role)));
        Assert.NotNull(_model.FindEntityType(typeof(Permission)));
        Assert.NotNull(_model.FindEntityType(typeof(UserRole)));
        Assert.NotNull(_model.FindEntityType(typeof(RolePermission)));
        Assert.NotNull(_model.FindEntityType(typeof(AuditLog)));
    }

    [Fact]
    public void Person_identification_is_filtered_unique_and_email_is_not_unique()
    {
        var person = Entity<Person>();
        var identificationIndex = Index<Person>(nameof(Person.IdentificationNumber));

        Assert.NotNull(identificationIndex);
        Assert.True(identificationIndex.IsUnique);
        Assert.Equal("[IdentificationNumber] IS NOT NULL", identificationIndex.GetFilter());
        Assert.Null(Index<Person>(nameof(Person.Email)));
        Assert.True(person.FindProperty(nameof(Person.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void Profile_and_account_person_relationships_are_unique()
    {
        Assert.True(Index<ClientProfile>(nameof(ClientProfile.PersonId))!.IsUnique);
        Assert.True(Index<UserAccount>(nameof(UserAccount.PersonId))!.IsUnique);
        Assert.True(Index<UserAccount>(nameof(UserAccount.Username))!.IsUnique);
    }

    [Fact]
    public void Authorization_bridges_use_documented_composite_keys()
    {
        Assert.Equal(
            [nameof(UserRole.UserId), nameof(UserRole.RoleId)],
            Entity<UserRole>().FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId)],
            Entity<RolePermission>().FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Audit_log_uses_bigint_identity_actor_fk_and_polymorphic_target()
    {
        var auditLog = Entity<AuditLog>();

        Assert.Equal("bigint", auditLog.FindProperty(nameof(AuditLog.AuditLogId))!.GetColumnType());
        Assert.True(auditLog.FindProperty(nameof(AuditLog.AuditLogId))!.ValueGenerated == ValueGenerated.OnAdd);
        Assert.Equal("nvarchar(120)", auditLog.FindProperty(nameof(AuditLog.ActionCode))!.GetColumnType());
        Assert.Equal("nvarchar(100)", auditLog.FindProperty(nameof(AuditLog.EntityName))!.GetColumnType());
        Assert.Equal("nvarchar(100)", auditLog.FindProperty(nameof(AuditLog.EntityId))!.GetColumnType());
        Assert.Equal("varchar(45)", auditLog.FindProperty(nameof(AuditLog.IpAddress))!.GetColumnType());
        Assert.Single(auditLog.GetForeignKeys());
        Assert.Equal(nameof(AuditLog.UserId), auditLog.GetForeignKeys().Single().Properties.Single().Name);
        Assert.Contains(auditLog.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AuditLog.UserId), nameof(AuditLog.OccurredAtUtc)]));
        Assert.Contains(auditLog.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AuditLog.EntityName), nameof(AuditLog.EntityId), nameof(AuditLog.OccurredAtUtc)]));
    }

    private IEntityType Entity<TEntity>() where TEntity : class =>
        _model.FindEntityType(typeof(TEntity))!;

    private IIndex? Index<TEntity>(params string[] propertyNames) where TEntity : class =>
        Entity<TEntity>().GetIndexes().SingleOrDefault(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
}
