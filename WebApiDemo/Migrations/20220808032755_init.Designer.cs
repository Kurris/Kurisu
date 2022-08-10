﻿// <auto-generated />
using System;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WebApiDemo.Migrations
{
    [DbContext(typeof(DefaultAppDbContext))]
    [Migration("20220808032755_init")]
    partial class init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.17");

            modelBuilder.Entity("WebApiDemo.Entities.Good", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime(3)");

                    b.Property<int>("Creator")
                        .HasColumnType("int");

                    b.Property<string>("GoodsDesc")
                        .HasColumnType("longtext");

                    b.Property<string>("GoodsName")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("UpdateTime")
                        .HasColumnType("datetime(3)");

                    b.Property<int?>("Updater")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("goods");
                });

            modelBuilder.Entity("WebApiDemo.Entities.Menu", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Code")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("datetime(3)");

                    b.Property<int>("Creator")
                        .HasColumnType("int");

                    b.Property<string>("DisplayName")
                        .HasColumnType("longtext");

                    b.Property<string>("Icon")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("PCode")
                        .HasColumnType("longtext");

                    b.Property<string>("Route")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("UpdateTime")
                        .HasColumnType("datetime(3)");

                    b.Property<int?>("Updater")
                        .HasColumnType("int");

                    b.Property<bool>("Visible")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("menus");
                });
#pragma warning restore 612, 618
        }
    }
}
