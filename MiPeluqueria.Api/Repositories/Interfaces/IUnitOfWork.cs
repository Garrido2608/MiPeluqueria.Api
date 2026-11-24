using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IClienteRepository Clientes { get; }
        IFichaTecnicaRepository FichasTecnicas { get; }
        IPeluqueroRepository Peluqueros { get; }
        IServicioRepository Servicios { get; }
        IRecetaServicioRepository Recetas { get; }
        IBloqueoHorarioRepository BloqueosHorarios { get; }
        ITurnoRepository Turnos { get; }
        IProductoRepository Productos { get; }
        IProveedorRepository Proveedores { get; }
        IOrdenCompraRepository OrdenesCompra { get; }
        ISesionCajaRepository SesionesCaja { get; }
        IVentaRepository Ventas { get; }
        IComisionConfigRepository ComisionesConfig { get; }
        ILiquidacionRepository Liquidaciones { get; }
        IUsuarioRepository Usuarios { get; }
        ICatalogoRepository Catalogos { get; }
        IConfiguracionSalonRepository ConfiguracionSalon { get; }
        IDashboardRepository Dashboard { get; }
        ISaaSRepository SaaS { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken ct = default);
    }
}
