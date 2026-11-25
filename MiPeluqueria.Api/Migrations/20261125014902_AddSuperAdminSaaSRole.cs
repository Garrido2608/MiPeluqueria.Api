using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminSaaSRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 6)
                BEGIN
                    SET IDENTITY_INSERT [Roles] ON;
                    INSERT INTO [Roles] ([Id], [CreatedAt], [CreatedByUserId], [DeletedAt], [DeletedByUserId], [IsDeleted], [Nombre], [UpdatedAt], [UpdatedByUserId])
                    VALUES (6, '2026-01-01T00:00:00.0000000', NULL, NULL, NULL, CAST(0 AS bit), N'SuperAdminSaaS', NULL, NULL);
                    SET IDENTITY_INSERT [Roles] OFF;
                END
                ELSE
                BEGIN
                    UPDATE [Roles]
                    SET [Nombre] = N'SuperAdminSaaS',
                        [IsDeleted] = CAST(0 AS bit),
                        [DeletedAt] = NULL
                    WHERE [Id] = 6;
                END

                IF EXISTS (SELECT 1 FROM [Usuarios] WHERE [Email] = N'superadmin@mipeluqueria.com' OR [Username] = N'superadmin')
                BEGIN
                    UPDATE [Usuarios]
                    SET [RolId] = 6,
                        [PasswordHash] = N'$2a$11$RUjOXUvzS0DfSxGuKhXROOQUTgq1oYzrZ2hbQLYDlSgKiV65v8Tbi',
                        [EmailVerificado] = CAST(1 AS bit),
                        [AceptaTerminos] = CAST(1 AS bit),
                        [IntentosFallidos] = 0,
                        [BloqueadoHasta] = NULL,
                        [Activo] = CAST(1 AS bit),
                        [IsDeleted] = CAST(0 AS bit),
                        [DeletedAt] = NULL
                    WHERE [Email] = N'superadmin@mipeluqueria.com' OR [Username] = N'superadmin';
                END
                ELSE IF NOT EXISTS (SELECT 1 FROM [Usuarios] WHERE [Id] = 2)
                BEGIN
                    SET IDENTITY_INSERT [Usuarios] ON;
                    INSERT INTO [Usuarios] (
                        [Id], [AceptaTerminos], [Activo], [BloqueadoHasta], [ClienteId], [CodigoVerificacion],
                        [CreatedAt], [CreatedByUserId], [DeletedAt], [DeletedByUserId], [Email], [EmailVerificado],
                        [FechaAceptacionTerminos], [FechaExpiracionCodigo], [IntentosFallidos], [IsDeleted],
                        [PasswordHash], [PeluqueroId], [RolId], [SucursalId], [TenantId], [UpdatedAt], [UpdatedByUserId], [Username]
                    )
                    VALUES (
                        2, CAST(1 AS bit), CAST(1 AS bit), NULL, NULL, NULL,
                        '2026-01-01T00:00:00.0000000', NULL, NULL, NULL, N'superadmin@mipeluqueria.com', CAST(1 AS bit),
                        '2026-01-01T00:00:00.0000000', NULL, 0, CAST(0 AS bit),
                        N'$2a$11$RUjOXUvzS0DfSxGuKhXROOQUTgq1oYzrZ2hbQLYDlSgKiV65v8Tbi', NULL, 6, 1, 1, NULL, NULL, N'superadmin'
                    );
                    SET IDENTITY_INSERT [Usuarios] OFF;
                END
                ELSE
                BEGIN
                    INSERT INTO [Usuarios] (
                        [AceptaTerminos], [Activo], [BloqueadoHasta], [ClienteId], [CodigoVerificacion],
                        [CreatedAt], [CreatedByUserId], [DeletedAt], [DeletedByUserId], [Email], [EmailVerificado],
                        [FechaAceptacionTerminos], [FechaExpiracionCodigo], [IntentosFallidos], [IsDeleted],
                        [PasswordHash], [PeluqueroId], [RolId], [SucursalId], [TenantId], [UpdatedAt], [UpdatedByUserId], [Username]
                    )
                    VALUES (
                        CAST(1 AS bit), CAST(1 AS bit), NULL, NULL, NULL,
                        '2026-01-01T00:00:00.0000000', NULL, NULL, NULL, N'superadmin@mipeluqueria.com', CAST(1 AS bit),
                        '2026-01-01T00:00:00.0000000', NULL, 0, CAST(0 AS bit),
                        N'$2a$11$RUjOXUvzS0DfSxGuKhXROOQUTgq1oYzrZ2hbQLYDlSgKiV65v8Tbi', NULL, 6, 1, 1, NULL, NULL, N'superadmin'
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [RefreshTokens] WHERE [UsuarioId] IN (SELECT [Id] FROM [Usuarios] WHERE [Email] = N'superadmin@mipeluqueria.com');
                DELETE FROM [HistorialSesiones] WHERE [UsuarioId] IN (SELECT [Id] FROM [Usuarios] WHERE [Email] = N'superadmin@mipeluqueria.com');
                DELETE FROM [Usuarios] WHERE [Email] = N'superadmin@mipeluqueria.com';
                IF NOT EXISTS (SELECT 1 FROM [Usuarios] WHERE [RolId] = 6)
                BEGIN
                    DELETE FROM [Roles] WHERE [Id] = 6;
                END
            ");
        }
    }
}
