// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;

#nullable disable

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Migrations
{
    [DbContext(typeof(CoreDbContext))]
    [Migration("00010101000000_DatabaseCollation")]
    partial class DatabaseCollation
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseCollation(CoreDbContext.DefaultCollation)
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);
#pragma warning restore 612, 618
        }
    }
}