using MiPeluqueria.Api.Repositories.Implementations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IClienteRepository Clientes { get; }
        IPeluqueroRepository Peluqueros { get; }
        IProductoRepository Productos { get; }
        IProveedorRepository Proveedores { get; }
        IServicioRepository Servicios { get; }
        ITurnoRepository Turnos { get; }
        ISesionCajaRepository SesionesCaja { get; }
        IVentaRepository Ventas { get; }

        IUsuarioRepository Usuarios { get; }
        IFichaTecnicaRepository FichasTecnicas { get; }
        IBloqueoHorarioRepository BloqueosHorarios { get; }
        IComisionConfigRepository ComisionesConfig { get; }
        ILiquidacionRepository Liquidaciones { get; }
        ICatalogoRepository Catalogos { get; }
        IDashboardRepository Dashboard { get; }
        IConfiguracionSalonRepository ConfiguracionSalon { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}