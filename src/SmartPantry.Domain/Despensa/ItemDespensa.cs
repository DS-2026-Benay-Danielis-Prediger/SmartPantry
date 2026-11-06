using System;
using Volo.Abp.Domain.Entities.Auditing;

public class ItemDespensa : FullAuditedAggregateRoot<Guid>
{
    public Guid PropietarioId { get; set; } // Identificador del propietario (sin usar ICurrentUser)
    public Guid ProductoId { get; set; }   // Referencia al producto
    public string Estado { get; set; }       // Ej: Activo, Consumido, etc.
    public DateTime? FechaVencimiento { get; set; } // Opcional
}