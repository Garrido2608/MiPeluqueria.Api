using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public IClienteRepository Clientes { get; }
        public IPeluqueroRepository Peluqueros { get; }
        public IProductoRepository Productos { get; }
        public IProveedorRepository Proveedores { get; }
        public IServicioRepository Servicios { get; }
        public ITurnoRepository Turnos { get; }
        public ISesionCajaRepository SesionesCaja { get; }
        public IVentaRepository Ventas { get; }

        public IUsuarioRepository Usuarios { get; }
        public IFichaTecnicaRepository FichasTecnicas { get; }
        public IBloqueoHorarioRepository BloqueosHorarios { get; }
        public IComisionConfigRepository ComisionesConfig { get; }
        public ILiquidacionRepository Liquidaciones { get; }
        public ICatalogoRepository Catalogos { get; }
        public IDashboardRepository Dashboard { get; }
        public IConfiguracionSalonRepository ConfiguracionSalon { get; }

        public UnitOfWork(
            ApplicationDbContext context,
            IClienteRepository clientes,
            IPeluqueroRepository peluqueros,
            IProductoRepository productos,
            IProveedorRepository proveedores,
            IServicioRepository servicios,
            ITurnoRepository turnos,
            ISesionCajaRepository sesionesCaja,
            IVentaRepository ventas,
            IUsuarioRepository usuarios,
            IFichaTecnicaRepository fichasTecnicas,
            IBloqueoHorarioRepository bloqueoHorarios,
            IComisionConfigRepository comisionesConfig,
            ILiquidacionRepository liquidaciones,
            ICatalogoRepository catalogos,
            IDashboardRepository dashboard,
            IConfiguracionSalonRepository configuracionSalon)
        {
            _context = context;
            Clientes = clientes;
            Peluqueros = peluqueros;
            Productos = productos;
            Proveedores = proveedores;
            Servicios = servicios;
            Turnos = turnos;
            SesionesCaja = sesionesCaja;
            Ventas = ventas;
            Usuarios = usuarios;
            FichasTecnicas = fichasTecnicas;
            BloqueosHorarios = bloqueoHorarios;
            ComisionesConfig = comisionesConfig;
            Liquidaciones = liquidaciones;
            Catalogos = catalogos;
            Dashboard = dashboard;
            ConfiguracionSalon = configuracionSalon;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
                return;

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_currentTransaction != null)
                    await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentTransaction != null)
                    await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}