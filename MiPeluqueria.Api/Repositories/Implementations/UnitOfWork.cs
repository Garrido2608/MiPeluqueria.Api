using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private int _transactionDepth;

        public IClienteRepository Clientes { get; }
        public IFichaTecnicaRepository FichasTecnicas { get; }
        public IPeluqueroRepository Peluqueros { get; }
        public IServicioRepository Servicios { get; }
        public IRecetaServicioRepository Recetas { get; }
        public IBloqueoHorarioRepository BloqueosHorarios { get; }
        public ITurnoRepository Turnos { get; }
        public IProductoRepository Productos { get; }
        public IProveedorRepository Proveedores { get; }
        public IOrdenCompraRepository OrdenesCompra { get; }
        public ISesionCajaRepository SesionesCaja { get; }
        public IVentaRepository Ventas { get; }
        public IComisionConfigRepository ComisionesConfig { get; }
        public ILiquidacionRepository Liquidaciones { get; }
        public IUsuarioRepository Usuarios { get; }
        public ICatalogoRepository Catalogos { get; }
        public IConfiguracionSalonRepository ConfiguracionSalon { get; }
        public IDashboardRepository Dashboard { get; }
        public ISaaSRepository SaaS { get; }

        public UnitOfWork(
            ApplicationDbContext context,
            IClienteRepository clientes,
            IFichaTecnicaRepository fichasTecnicas,
            IPeluqueroRepository peluqueros,
            IServicioRepository servicios,
            IRecetaServicioRepository recetas,
            IBloqueoHorarioRepository bloqueoHorarios,
            ITurnoRepository turnos,
            IProductoRepository productos,
            IProveedorRepository proveedores,
            IOrdenCompraRepository ordenesCompra,
            ISesionCajaRepository sesionesCaja,
            IVentaRepository ventas,
            IComisionConfigRepository comisionesConfig,
            ILiquidacionRepository liquidaciones,
            IUsuarioRepository usuarios,
            ICatalogoRepository catalogos,
            IConfiguracionSalonRepository configuracionSalon,
            IDashboardRepository dashboard,
            ISaaSRepository saas)
        {
            _context = context;
            Clientes = clientes;
            FichasTecnicas = fichasTecnicas;
            Peluqueros = peluqueros;
            Servicios = servicios;
            Recetas = recetas;
            BloqueosHorarios = bloqueoHorarios;
            Turnos = turnos;
            Productos = productos;
            Proveedores = proveedores;
            OrdenesCompra = ordenesCompra;
            SesionesCaja = sesionesCaja;
            Ventas = ventas;
            ComisionesConfig = comisionesConfig;
            Liquidaciones = liquidaciones;
            Usuarios = usuarios;
            Catalogos = catalogos;
            ConfiguracionSalon = configuracionSalon;
            Dashboard = dashboard;
            SaaS = saas;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            if (_transactionDepth > 0 || _currentTransaction != null || _context.Database.CurrentTransaction != null)
            {
                _transactionDepth++;
                try
                {
                    await operation(ct);
                }
                finally
                {
                    _transactionDepth--;
                }
                return;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async token =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(isolationLevel, token);
                _currentTransaction = transaction;
                _transactionDepth = 1;

                try
                {
                    await operation(token);

                    if (_context.ChangeTracker.HasChanges())
                        await _context.SaveChangesAsync(token);

                    await transaction.CommitAsync(token);
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                    catch
                    {
                        // Si la conexión física cayó durante un fallo transitorio, ignorar error secundario en Rollback
                    }

                    _context.ChangeTracker.Clear();
                    throw;
                }
                finally
                {
                    _transactionDepth = 0;
                    _currentTransaction = null;
                }
            }, ct);
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
