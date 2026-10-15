namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IClienteRepository Clientes { get; }
        IPeluqueroRepository Peluqueros { get; }
        IServicioRepository Servicios { get; }
        ITurnoRepository Turnos { get; }
        IProductoRepository Productos { get; }
        IProveedorRepository Proveedores { get; }

        Task<int> SaveChangesAsync();
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}