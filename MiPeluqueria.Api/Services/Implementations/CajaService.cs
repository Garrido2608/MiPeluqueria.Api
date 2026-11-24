using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class CajaService : ICajaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CajaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<SesionCajaResponseDto?>> ObtenerEstadoCajaActualAsync(CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (caja == null)
                return ApiResponse<SesionCajaResponseDto?>.Ok(null, "No hay ninguna caja abierta actualmente.");

            var usuariosDict = await CargarUsuariosSesionAsync(new[] { caja }, ct);
            var dto = MapToResponseDto(caja, usuariosDict, incluirMovimientos: true, recalcularDesdeColecciones: true);
            return ApiResponse<SesionCajaResponseDto?>.Ok(dto, "Caja abierta encontrada.");
        }

        public async Task<ApiResponse<SesionCajaResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            var caja = await _unitOfWork.SesionesCaja.GetByIdWithMovimientosYVentasAsync(id, ct);
            if (caja == null)
                return ApiResponse<SesionCajaResponseDto>.Fail($"No se encontró la sesión de caja con ID {id}.");

            var usuariosDict = await CargarUsuariosSesionAsync(new[] { caja }, ct);
            var dto = MapToResponseDto(caja, usuariosDict, incluirMovimientos: true, recalcularDesdeColecciones: true);
            return ApiResponse<SesionCajaResponseDto>.Ok(dto, "Detalle de sesión de caja obtenido.");
        }

        public async Task<ApiResponse<PagedResult<SesionCajaResponseDto>>> ObtenerHistorialCajasAsync(
            DateTime? desde = null,
            DateTime? hasta = null,
            CancellationToken ct = default)
        {
            var filter = new PaginationFilterDto
            {
                PageNumber = 1,
                PageSize = 15,
                FechaDesde = desde,
                FechaHasta = hasta
            };

            return await ObtenerHistorialCajasAsync(filter, ct);
        }

        public async Task<ApiResponse<PagedResult<SesionCajaResponseDto>>> ObtenerHistorialCajasAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            filter ??= new PaginationFilterDto();

            var (items, totalRecords) = await _unitOfWork.SesionesCaja.GetHistorialCajasPagedAsync(filter, ct);
            var usuariosDict = await CargarUsuariosSesionAsync(items, ct);

            var data = items
                .Select(c => MapToResponseDto(c, usuariosDict, incluirMovimientos: false, recalcularDesdeColecciones: false))
                .ToList();

            var pagedResult = new PagedResult<SesionCajaResponseDto>
            {
                Data = data,
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<SesionCajaResponseDto>>.Ok(pagedResult, "Historial de sesiones de caja obtenido.");
        }

        public async Task<ApiResponse<SesionCajaResponseDto>> AbrirCajaAsync(
            AperturaCajaDto dto,
            int? usuarioEjecutorId = null,
            CancellationToken ct = default)
        {
            if (dto.MontoInicial < 0m)
                return ApiResponse<SesionCajaResponseDto>.Fail("El fondo inicial de apertura no puede ser negativo.");

            var cajaAbierta = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(ct);
            if (cajaAbierta != null)
                return ApiResponse<SesionCajaResponseDto>.Fail($"Ya existe una sesión de caja abierta (ID #{cajaAbierta.Id}). Debe cerrarla antes de abrir una nueva.");

            int usuarioId = usuarioEjecutorId ?? (dto.UsuarioId > 0 ? dto.UsuarioId : 1);

            var nuevaSesion = new SesionCaja
            {
                UsuarioAperturaId = usuarioId,
                FechaApertura = DateTime.UtcNow,
                MontoInicial = dto.MontoInicial,
                TotalVentasEfectivo = 0m,
                TotalVentasDigital = 0m,
                TotalIngresosManuales = 0m,
                TotalEgresosManuales = 0m,
                MontoFinalEsperado = dto.MontoInicial,
                EstadoAbierta = true,
                CreatedByUserId = usuarioId
            };

            await _unitOfWork.SesionesCaja.AbrirSesionCajaAsync(nuevaSesion, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var usuariosDict = await CargarUsuariosSesionAsync(new[] { nuevaSesion }, ct);
            var responseDto = MapToResponseDto(nuevaSesion, usuariosDict, incluirMovimientos: true, recalcularDesdeColecciones: false);

            return ApiResponse<SesionCajaResponseDto>.Ok(responseDto, "Caja abierta exitosamente.");
        }

        public async Task<ApiResponse<MovimientoCajaResponseDto>> RegistrarIngresoCajaAsync(
            int sesionCajaId,
            decimal monto,
            string concepto,
            string tipo = "IngresoManual",
            int? usuarioEjecutorId = null,
            CancellationToken ct = default)
        {
            if (monto <= 0m)
                return ApiResponse<MovimientoCajaResponseDto>.Fail("El monto del ingreso debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(concepto))
                return ApiResponse<MovimientoCajaResponseDto>.Fail("Debe especificar el concepto del ingreso de caja.");

            string tipoNormalizado = string.IsNullOrWhiteSpace(tipo)
                ? "IngresoManual"
                : (EsMovimientoIngreso(tipo) ? tipo.Trim() : $"Ingreso_{tipo.Trim()}");

            if (tipoNormalizado.Length > 50)
                tipoNormalizado = tipoNormalizado[..50];

            string conceptoNormalizado = concepto.Trim().Length > 250
                ? concepto.Trim()[..250]
                : concepto.Trim();

            MovimientoCaja? movimientoCreado = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(token)
                        ?? throw new BusinessRuleException("No hay una sesión de caja abierta válida para registrar el ingreso.");

                    if (sesionCajaId > 0 && caja.Id != sesionCajaId)
                        throw new BusinessRuleException($"La sesión de caja indicada (#{sesionCajaId}) no coincide con la caja abierta actual (#{caja.Id}).");

                    var (efectivo, digital, ingresosPrevios, egresosPrevios) = CalcularTotalesColecciones(caja);

                    movimientoCreado = new MovimientoCaja
                    {
                        SesionCajaId = caja.Id,
                        Tipo = tipoNormalizado,
                        Monto = monto,
                        Concepto = conceptoNormalizado,
                        Fecha = DateTime.UtcNow,
                        CreatedByUserId = usuarioEjecutorId
                    };

                    await _unitOfWork.SesionesCaja.RegistrarMovimientoCajaAsync(movimientoCreado, token);

                    caja.TotalVentasEfectivo = efectivo;
                    caja.TotalVentasDigital = digital;
                    caja.TotalIngresosManuales = ingresosPrevios + monto;
                    caja.TotalEgresosManuales = egresosPrevios;
                    caja.MontoFinalEsperado = caja.MontoInicial + caja.TotalVentasEfectivo + caja.TotalIngresosManuales - caja.TotalEgresosManuales;
                    caja.UpdatedByUserId = usuarioEjecutorId;
                    _unitOfWork.SesionesCaja.ActualizarSesionCaja(caja);

                    await _unitOfWork.SaveChangesAsync(token);
                }, ct: ct);

                return ApiResponse<MovimientoCajaResponseDto>.Ok(
                    MapMovimientoToDto(movimientoCreado!),
                    "Ingreso manual registrado correctamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<MovimientoCajaResponseDto>.Fail(ex.Message);
            }
        }

        public async Task<ApiResponse<MovimientoCajaResponseDto>> RegistrarEgresoCajaAsync(
            int sesionCajaId,
            decimal monto,
            string concepto,
            string tipo = "EgresoMenor",
            int? usuarioEjecutorId = null,
            CancellationToken ct = default)
        {
            if (monto <= 0m)
                return ApiResponse<MovimientoCajaResponseDto>.Fail("El monto del egreso debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(concepto))
                return ApiResponse<MovimientoCajaResponseDto>.Fail("Debe especificar el concepto del egreso de caja.");

            string tipoNormalizado = string.IsNullOrWhiteSpace(tipo)
                ? "EgresoMenor"
                : (EsMovimientoIngreso(tipo) ? $"Egreso_{tipo.Trim()}" : tipo.Trim());

            if (tipoNormalizado.Length > 50)
                tipoNormalizado = tipoNormalizado[..50];

            string conceptoNormalizado = concepto.Trim().Length > 250
                ? concepto.Trim()[..250]
                : concepto.Trim();

            MovimientoCaja? movimientoCreado = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(token)
                        ?? throw new BusinessRuleException("No hay una sesión de caja abierta válida para registrar el egreso.");

                    if (sesionCajaId > 0 && caja.Id != sesionCajaId)
                        throw new BusinessRuleException($"La sesión de caja indicada (#{sesionCajaId}) no coincide con la caja abierta actual (#{caja.Id}).");

                    var (efectivo, digital, ingresosPrevios, egresosPrevios) = CalcularTotalesColecciones(caja);

                    movimientoCreado = new MovimientoCaja
                    {
                        SesionCajaId = caja.Id,
                        Tipo = tipoNormalizado,
                        Monto = monto,
                        Concepto = conceptoNormalizado,
                        Fecha = DateTime.UtcNow,
                        CreatedByUserId = usuarioEjecutorId
                    };

                    await _unitOfWork.SesionesCaja.RegistrarMovimientoCajaAsync(movimientoCreado, token);

                    caja.TotalVentasEfectivo = efectivo;
                    caja.TotalVentasDigital = digital;
                    caja.TotalIngresosManuales = ingresosPrevios;
                    caja.TotalEgresosManuales = egresosPrevios + monto;
                    caja.MontoFinalEsperado = caja.MontoInicial + caja.TotalVentasEfectivo + caja.TotalIngresosManuales - caja.TotalEgresosManuales;
                    caja.UpdatedByUserId = usuarioEjecutorId;
                    _unitOfWork.SesionesCaja.ActualizarSesionCaja(caja);

                    await _unitOfWork.SaveChangesAsync(token);
                }, ct: ct);

                return ApiResponse<MovimientoCajaResponseDto>.Ok(
                    MapMovimientoToDto(movimientoCreado!),
                    "Egreso de caja registrado correctamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<MovimientoCajaResponseDto>.Fail(ex.Message);
            }
        }

        public async Task<ApiResponse<SesionCajaResponseDto>> CerrarCajaAsync(
            CierreCajaDto dto,
            int? usuarioEjecutorId = null,
            CancellationToken ct = default)
        {
            if (dto.MontoFinalReal < 0m)
                return ApiResponse<SesionCajaResponseDto>.Fail("El dinero físico contado en caja no puede ser negativo.");

            SesionCaja? cajaCerrada = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var caja = await _unitOfWork.SesionesCaja.GetCajaAbiertaActualAsync(token)
                        ?? throw new BusinessRuleException("No existe ninguna sesión de caja abierta para cerrar.");

                    int usuarioCierreId = usuarioEjecutorId ?? (dto.UsuarioId > 0 ? dto.UsuarioId : caja.UsuarioAperturaId);

                    var (ventasEfectivo, ventasDigital, ingresosManuales, egresosManuales) = CalcularTotalesColecciones(caja);
                    decimal esperado = caja.MontoInicial + ventasEfectivo + ingresosManuales - egresosManuales;

                    caja.UsuarioCierreId = usuarioCierreId;
                    caja.FechaCierre = DateTime.UtcNow;
                    caja.TotalVentasEfectivo = ventasEfectivo;
                    caja.TotalVentasDigital = ventasDigital;
                    caja.TotalIngresosManuales = ingresosManuales;
                    caja.TotalEgresosManuales = egresosManuales;
                    caja.MontoFinalEsperado = esperado;
                    caja.MontoFinalReal = dto.MontoFinalReal;
                    caja.Diferencia = dto.MontoFinalReal - esperado;
                    caja.ObservacionesCierre = string.IsNullOrWhiteSpace(dto.ObservacionesCierre)
                        ? null
                        : (dto.ObservacionesCierre.Trim().Length > 500 ? dto.ObservacionesCierre.Trim()[..500] : dto.ObservacionesCierre.Trim());
                    caja.EstadoAbierta = false;
                    caja.UpdatedByUserId = usuarioCierreId;

                    _unitOfWork.SesionesCaja.ActualizarSesionCaja(caja);
                    await _unitOfWork.SaveChangesAsync(token);

                    cajaCerrada = caja;
                }, ct: ct);

                var usuariosDict = await CargarUsuariosSesionAsync(new[] { cajaCerrada! }, ct);
                var responseDto = MapToResponseDto(cajaCerrada!, usuariosDict, incluirMovimientos: true, recalcularDesdeColecciones: false);

                return ApiResponse<SesionCajaResponseDto>.Ok(responseDto, "Caja cerrada y arqueada exitosamente.");
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<SesionCajaResponseDto>.Fail(ex.Message);
            }
        }

        private async Task<Dictionary<int, Usuario>> CargarUsuariosSesionAsync(
            IEnumerable<SesionCaja> sesiones,
            CancellationToken ct)
        {
            var ids = new HashSet<int>();
            foreach (var s in sesiones)
            {
                if (s.UsuarioAperturaId > 0)
                    ids.Add(s.UsuarioAperturaId);
                if (s.UsuarioCierreId.HasValue && s.UsuarioCierreId.Value > 0)
                    ids.Add(s.UsuarioCierreId.Value);
            }

            if (ids.Count == 0)
                return new Dictionary<int, Usuario>();

            return await _unitOfWork.Usuarios.GetByIdsDictionaryAsync(ids, tracking: false, ct);
        }

        private static (decimal VentasEfectivo, decimal VentasDigital, decimal IngresosManuales, decimal EgresosManuales) CalcularTotalesColecciones(SesionCaja caja)
        {
            var pagosActivos = caja.Ventas
                .Where(v => !v.IsDeleted)
                .SelectMany(v => v.Pagos)
                .Where(p => !p.IsDeleted)
                .ToList();

            decimal ventasEfectivo = pagosActivos
                .Where(p => p.MedioPago != null && p.MedioPago.EsEfectivoFisico)
                .Sum(p => p.Importe);

            decimal ventasDigital = pagosActivos
                .Where(p => p.MedioPago == null || !p.MedioPago.EsEfectivoFisico)
                .Sum(p => p.Importe);

            var movimientosActivos = caja.MovimientosCaja
                .Where(m => !m.IsDeleted)
                .ToList();

            decimal ingresosManuales = movimientosActivos
                .Where(m => EsMovimientoIngreso(m.Tipo))
                .Sum(m => m.Monto);

            decimal egresosManuales = movimientosActivos
                .Where(m => !EsMovimientoIngreso(m.Tipo))
                .Sum(m => m.Monto);

            return (ventasEfectivo, ventasDigital, ingresosManuales, egresosManuales);
        }

        private static bool EsMovimientoIngreso(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return false;

            return tipo.Trim().StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase);
        }

        private static SesionCajaResponseDto MapToResponseDto(
            SesionCaja caja,
            Dictionary<int, Usuario> usuariosDict,
            bool incluirMovimientos,
            bool recalcularDesdeColecciones)
        {
            decimal ventasEfectivo = caja.TotalVentasEfectivo;
            decimal ventasDigital = caja.TotalVentasDigital;
            decimal ingresosManuales = caja.TotalIngresosManuales;
            decimal egresosManuales = caja.TotalEgresosManuales;
            decimal montoFinalEsperado;

            if (recalcularDesdeColecciones)
            {
                (ventasEfectivo, ventasDigital, ingresosManuales, egresosManuales) = CalcularTotalesColecciones(caja);
                montoFinalEsperado = caja.MontoInicial + ventasEfectivo + ingresosManuales - egresosManuales;
            }
            else
            {
                montoFinalEsperado = caja.MontoFinalEsperado
                    ?? (caja.MontoInicial + ventasEfectivo + ingresosManuales - egresosManuales);
            }

            string? aperturaNombre = usuariosDict.TryGetValue(caja.UsuarioAperturaId, out var uApertura)
                ? uApertura.Username
                : null;

            string? cierreNombre = caja.UsuarioCierreId.HasValue && usuariosDict.TryGetValue(caja.UsuarioCierreId.Value, out var uCierre)
                ? uCierre.Username
                : null;

            var movimientosDto = incluirMovimientos
                ? caja.MovimientosCaja
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.Fecha)
                    .ThenByDescending(m => m.Id)
                    .Select(MapMovimientoToDto)
                    .ToList()
                : new List<MovimientoCajaResponseDto>();

            return new SesionCajaResponseDto
            {
                Id = caja.Id,
                UsuarioAperturaId = caja.UsuarioAperturaId,
                UsuarioAperturaNombre = aperturaNombre,
                FechaApertura = caja.FechaApertura,
                MontoInicial = caja.MontoInicial,
                UsuarioCierreId = caja.UsuarioCierreId,
                UsuarioCierreNombre = cierreNombre,
                FechaCierre = caja.FechaCierre,
                TotalVentasEfectivo = ventasEfectivo,
                TotalVentasDigital = ventasDigital,
                TotalIngresosManuales = ingresosManuales,
                TotalEgresosManuales = egresosManuales,
                MontoFinalEsperado = montoFinalEsperado,
                MontoFinalReal = caja.MontoFinalReal,
                Diferencia = caja.Diferencia,
                ObservacionesCierre = caja.ObservacionesCierre,
                EstadoAbierta = caja.EstadoAbierta,
                Movimientos = movimientosDto
            };
        }

        private static MovimientoCajaResponseDto MapMovimientoToDto(MovimientoCaja m)
        {
            return new MovimientoCajaResponseDto
            {
                Id = m.Id,
                SesionCajaId = m.SesionCajaId,
                Tipo = m.Tipo,
                Monto = m.Monto,
                Concepto = m.Concepto,
                Fecha = m.Fecha
            };
        }
    }
}
