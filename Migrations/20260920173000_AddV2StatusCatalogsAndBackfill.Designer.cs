using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RestauranteAPI.Data;

#nullable disable

namespace RestauranteAPI.Migrations
{
    [DbContext(typeof(MyAppDbContext))]
    [Migration("20260920173000_AddV2StatusCatalogsAndBackfill")]
    partial class AddV2StatusCatalogsAndBackfill
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .UseIdentityColumns();
#pragma warning restore 612, 618
        }
    }
}
