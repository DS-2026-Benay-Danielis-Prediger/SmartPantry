using System;
using Volo.Abp.Domain.Entities.Auditing;

public class Advertencia : FullAuditedAggregateRoot<Guid>
{
    public Guid ItemDespensaId { get; set; }
    public string Mensaje { get; set; }
    public string TipoAdvertencia { get; set; } // Ej: "ProximoAVencer", "Vencido"
    public bool Activa { get; set; }
}