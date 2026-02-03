using Microsoft.EntityFrameworkCore;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empresa> Empresas => Set<Empresa>();
        public DbSet<Persona> Personas => Set<Persona>();
        public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar Empresa
            modelBuilder.Entity<Empresa>(entity =>
            {
                entity.ToTable("empresas");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.FechaCreacion)
                    .HasColumnName("fechacreacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Activo)
                    .HasColumnName("activo")
                    .HasDefaultValue(true);

                // Relaciones
                entity.HasMany(e => e.Personas)
                    .WithOne(p => p.Empresa)
                    .HasForeignKey(p => p.EmpresaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurar Persona
            modelBuilder.Entity<Persona>(entity =>
            {
                entity.ToTable("personas");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.Nombre)
                    .HasColumnName("nombre")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Identificacion)
                    .HasColumnName("identificacion")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Telefono)
                    .HasColumnName("telefono")
                    .HasMaxLength(50);

                entity.Property(p => p.Email)  
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(p => p.FechaCreacion)  
                    .HasColumnName("fechacreacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(p => p.Activo) 
                    .HasColumnName("activo")
                    .HasDefaultValue(true);

                entity.Property(p => p.EmpresaId)
                    .HasColumnName("empresaid")
                    .IsRequired();

                // Índice único en identificación
                entity.HasIndex(p => p.Identificacion)
                    .IsUnique()
                    .HasDatabaseName("ix_personas_identificacion");

                // Índice en empresa_id para mejor performance
                entity.HasIndex(p => p.EmpresaId)
                    .HasDatabaseName("ix_personas_empresaid");

                // Relación con Empresa
                entity.HasOne(p => p.Empresa)
                    .WithMany(e => e.Personas)
                    .HasForeignKey(p => p.EmpresaId)
                    .HasConstraintName("fk_personas_empresa")
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Dispositivos
                entity.HasMany(p => p.Dispositivos)
                    .WithOne(d => d.Persona)
                    .HasForeignKey(d => d.PersonaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar Dispositivo
            modelBuilder.Entity<Dispositivo>(entity =>
            {
                entity.ToTable("dispositivos");
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(d => d.IMEI)
                    .HasColumnName("imei")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(d => d.FechaRegistro)
                    .HasColumnName("fecharegistro")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(d => d.Activo)
                    .HasColumnName("activo")
                    .HasDefaultValue(true);

                entity.Property(d => d.PersonaId)
                    .HasColumnName("personaid")
                    .IsRequired();

                // Índice único en IMEI
                entity.HasIndex(d => d.IMEI)
                    .IsUnique()
                    .HasDatabaseName("ix_dispositivos_imei");

                // Índice en persona_id para mejor performance
                entity.HasIndex(d => d.PersonaId)
                    .HasDatabaseName("ix_dispositivos_personaid");

                // Índice en activo para búsquedas frecuentes
                entity.HasIndex(d => d.Activo)
                    .HasDatabaseName("ix_dispositivos_activo")
                    .HasFilter("activo = true");

                // Relación con Persona
                entity.HasOne(d => d.Persona)
                    .WithMany(p => p.Dispositivos)
                    .HasForeignKey(d => d.PersonaId)
                    .HasConstraintName("fk_dispositivos_persona")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(u => u.Username)
                    .HasColumnName("username")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired();

                entity.Property(u => u.Rol)
                    .HasColumnName("rol")
                    .HasMaxLength(50)
                    .HasDefaultValue("Usuario");

                entity.Property(u => u.EmpresaId)
                    .HasColumnName("empresa_id")
                    .IsRequired(false);

                entity.Property(u => u.Activo)
                    .HasColumnName("activo")
                    .HasDefaultValue(true);

                entity.Property(u => u.FechaCreacion)
                    .HasColumnName("fecha_creacion")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Índice único en username
                entity.HasIndex(u => u.Username)
                    .IsUnique()
                    .HasDatabaseName("ix_usuarios_username");

                // Índice en empresa_id
                entity.HasIndex(u => u.EmpresaId)
                    .HasDatabaseName("ix_usuarios_empresa_id");

                // Índice en rol para búsquedas por rol
                entity.HasIndex(u => u.Rol)
                    .HasDatabaseName("ix_usuarios_rol");

                // Índice en activo
                entity.HasIndex(u => u.Activo)
                    .HasDatabaseName("ix_usuarios_activo")
                    .HasFilter("activo = true");

                // Relación con Empresa (opcional)
                entity.HasOne(u => u.Empresa)
                    .WithMany()
                    .HasForeignKey(u => u.EmpresaId)
                    .HasConstraintName("fk_usuarios_empresa")
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        // Método para asegurar que el modelo esté correctamente configurado
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Actualizar timestamps antes de guardar
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Usuario &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    ((Usuario)entityEntry.Entity).FechaCreacion = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}