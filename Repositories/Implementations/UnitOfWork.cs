using Microsoft.EntityFrameworkCore.Storage;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public IClienteRepository Clientes { get; }
        public IPeluqueroRepository Peluqueros { get; }
        public IServicioRepository Servicios { get; }
        public ITurnoRepository Turnos { get; }
        public IProductoRepository Productos { get; }
        public IProveedorRepository Proveedores { get; }

        public UnitOfWork(
            ApplicationDbContext context,
            IClienteRepository clienteRepository,
            IPeluqueroRepository peluqueroRepository,
            IServicioRepository servicioRepository,
            ITurnoRepository turnoRepository,
            IProductoRepository productoRepository,
            IProveedorRepository proveedorRepository)
        {
            _context = context;
            Clientes = clienteRepository;
            Peluqueros = peluqueroRepository;
            Servicios = servicioRepository;
            Turnos = turnoRepository;
            Productos = productoRepository;
            Proveedores = proveedorRepository;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}