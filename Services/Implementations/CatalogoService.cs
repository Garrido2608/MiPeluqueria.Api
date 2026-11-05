using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class CatalogoService : ICatalogoService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;
        private readonly ITenantProvider _tenantProvider;

        public CatalogoService(
            IUnitOfWork uow,
            IMemoryCache cache,
            ITenantProvider tenantProvider)
        {
            _uow = uow;
            _cache = cache;
            _tenantProvider = tenantProvider;
        }

        private string GetCacheKey()
        {
            int tenantId = _tenantProvider.CurrentTenantId;
            int sucursalId = _tenantProvider.CurrentSucursalId ?? 1;
            return $"SaaS_Catalogos_T{tenantId}_S{sucursalId}";
        }

        private void InvalidarCache()
        {
            _cache.Remove(GetCacheKey());
        }

        public async Task<ApiResponse<CatalogosMaestrosResponseDto>> ObtenerTodosLosCatalogosAsync()
        {
            var cacheKey = GetCacheKey();
            if (_cache.TryGetValue(cacheKey, out CatalogosMaestrosResponseDto? cached) && cached != null)
            {
                return ApiResponse<CatalogosMaestrosResponseDto>.Exito(cached, "Catálogos maestros obtenidos desde caché.");
            }

            var dto = new CatalogosMaestrosResponseDto
            {
                TiposCabello = (await ObtenerTiposCabelloAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                TiposRostro = (await ObtenerTiposRostroAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                CategoriasServicio = (await ObtenerCategoriasServicioAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                Especialidades = (await ObtenerEspecialidadesAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                EstadosTurno = (await ObtenerEstadosTurnoAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                MotivosCancelacion = (await ObtenerMotivosCancelacionAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                CategoriasProducto = (await ObtenerCategoriasProductoAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                MediosPago = (await ObtenerMediosPagoAsync()).Data?.ToList() ?? new List<CatalogoItemDto>(),
                Roles = (await ObtenerRolesAsync()).Data?.ToList() ?? new List<CatalogoItemDto>()
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(30));
            return ApiResponse<CatalogosMaestrosResponseDto>.Exito(dto, "Catálogos maestros obtenidos correctamente.");
        }

        // =========================================================================
        // 1. TIPOS DE CABELLO
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposCabelloAsync()
        {
            var list = await _uow.Catalogos.GetTiposCabelloAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearTipoCabelloAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre del tipo de cabello es obligatorio.");

            var entity = new TipoCabello
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Tipo de cabello creado exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarTipoCabelloAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetTipoCabelloByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró el tipo de cabello con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Tipo de cabello actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarTipoCabelloAsync(int id)
        {
            var entity = await _uow.Catalogos.GetTipoCabelloByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró el tipo de cabello con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Tipo de cabello eliminado correctamente.");
        }

        // =========================================================================
        // 2. TIPOS DE ROSTRO
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposRostroAsync()
        {
            var list = await _uow.Catalogos.GetTiposRostroAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearTipoRostroAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre del tipo de rostro es obligatorio.");

            var entity = new TipoRostro
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Tipo de rostro creado exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarTipoRostroAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetTipoRostroByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró el tipo de rostro con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Tipo de rostro actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarTipoRostroAsync(int id)
        {
            var entity = await _uow.Catalogos.GetTipoRostroByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró el tipo de rostro con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Tipo de rostro eliminado correctamente.");
        }

        // =========================================================================
        // 3. CATEGORÍAS DE SERVICIO
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasServicioAsync()
        {
            var list = await _uow.Catalogos.GetCategoriasServicioAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearCategoriaServicioAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre de la categoría de servicio es obligatorio.");

            var entity = new CategoriaServicio
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Categoría de servicio creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarCategoriaServicioAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetCategoriaServicioByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró la categoría de servicio con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Categoría de servicio actualizada correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarCategoriaServicioAsync(int id)
        {
            var entity = await _uow.Catalogos.GetCategoriaServicioByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró la categoría de servicio con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Categoría de servicio eliminada correctamente.");
        }

        // =========================================================================
        // 4. ESPECIALIDADES
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEspecialidadesAsync()
        {
            var list = await _uow.Catalogos.GetEspecialidadesAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearEspecialidadAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre de la especialidad es obligatorio.");

            var entity = new Especialidad
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Especialidad creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarEspecialidadAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetEspecialidadByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró la especialidad con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Especialidad actualizada correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarEspecialidadAsync(int id)
        {
            var entity = await _uow.Catalogos.GetEspecialidadByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró la especialidad con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Especialidad eliminada correctamente.");
        }

        // =========================================================================
        // 5. ESTADOS DE TURNO (CATÁLOGO DE PLATAFORMA)
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEstadosTurnoAsync()
        {
            var list = await _uow.Catalogos.GetEstadosTurnoAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        // =========================================================================
        // 6. MOTIVOS DE CANCELACIÓN
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMotivosCancelacionAsync()
        {
            var list = await _uow.Catalogos.GetMotivosCancelacionAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Descripcion,
                Descripcion = x.Descripcion
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearMotivoCancelacionAsync(CrearCatalogoItemDto dto)
        {
            var texto = !string.IsNullOrWhiteSpace(dto.Descripcion) ? dto.Descripcion.Trim() : dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(texto))
                return ApiResponse<CatalogoItemDto>.Falla("La descripción del motivo de cancelación es obligatoria.");

            var entity = new MotivoCancelacion
            {
                Descripcion = texto
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Descripcion,
                Descripcion = entity.Descripcion
            }, "Motivo de cancelación creado exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarMotivoCancelacionAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetMotivoCancelacionByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró el motivo de cancelación con ID {id}.");

            var texto = !string.IsNullOrWhiteSpace(dto.Descripcion) ? dto.Descripcion.Trim() : dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(texto))
                return ApiResponse<CatalogoItemDto>.Falla("La descripción del motivo de cancelación es obligatoria.");

            entity.Descripcion = texto;
            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Descripcion,
                Descripcion = entity.Descripcion
            }, "Motivo de cancelación actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarMotivoCancelacionAsync(int id)
        {
            var entity = await _uow.Catalogos.GetMotivoCancelacionByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró el motivo de cancelación con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Motivo de cancelación eliminado correctamente.");
        }

        // =========================================================================
        // 7. CATEGORÍAS DE PRODUCTO
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasProductoAsync()
        {
            var list = await _uow.Catalogos.GetCategoriasProductoAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearCategoriaProductoAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre de la categoría de producto es obligatorio.");

            var entity = new CategoriaProducto
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Categoría de producto creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarCategoriaProductoAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetCategoriaProductoByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró la categoría de producto con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre
            }, "Categoría de producto actualizada correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarCategoriaProductoAsync(int id)
        {
            var entity = await _uow.Catalogos.GetCategoriaProductoByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró la categoría de producto con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Categoría de producto eliminada correctamente.");
        }

        // =========================================================================
        // 8. MEDIOS DE PAGO (CON SOPORTE DE EsEfectivoFisico)
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMediosPagoAsync()
        {
            var list = await _uow.Catalogos.GetMediosPagoAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion ?? x.Nombre,
                EsEfectivoFisico = x.EsEfectivoFisico
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearMedioPagoAsync(CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<CatalogoItemDto>.Falla("El nombre del medio de pago es obligatorio.");

            var entity = new MedioPago
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                EsEfectivoFisico = dto.EsEfectivoFisico ?? false
            };

            await _uow.Catalogos.AddEntityAsync(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre,
                EsEfectivoFisico = entity.EsEfectivoFisico
            }, "Medio de pago creado exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> ActualizarMedioPagoAsync(int id, ActualizarCatalogoItemDto dto)
        {
            var entity = await _uow.Catalogos.GetMedioPagoByIdAsync(id);
            if (entity == null)
                return ApiResponse<CatalogoItemDto>.Falla($"No se encontró el medio de pago con ID {id}.");

            entity.Nombre = dto.Nombre.Trim();
            entity.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
            if (dto.EsEfectivoFisico.HasValue)
            {
                entity.EsEfectivoFisico = dto.EsEfectivoFisico.Value;
            }

            _uow.Catalogos.UpdateEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<CatalogoItemDto>.Exito(new CatalogoItemDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion ?? entity.Nombre,
                EsEfectivoFisico = entity.EsEfectivoFisico
            }, "Medio de pago actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarMedioPagoAsync(int id)
        {
            var entity = await _uow.Catalogos.GetMedioPagoByIdAsync(id);
            if (entity == null)
                return ApiResponse<bool>.Falla($"No se encontró el medio de pago con ID {id}.");

            _uow.Catalogos.DeleteEntity(entity);
            await _uow.SaveChangesAsync();
            InvalidarCache();

            return ApiResponse<bool>.Exito(true, "Medio de pago eliminado correctamente.");
        }

        // =========================================================================
        // 9. ROLES DEL SISTEMA (CATÁLOGO DE PLATAFORMA)
        // =========================================================================
        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerRolesAsync()
        {
            var list = await _uow.Catalogos.GetRolesAsync();
            var dtos = list.Select(x => new CatalogoItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Descripcion = x.Nombre
            }).ToList();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Exito(dtos);
        }
    }
}
