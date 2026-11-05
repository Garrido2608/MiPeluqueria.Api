# 07 - LIBRO MAYOR DE ESTADO EN VIVO, BITÁCORA DE CAMBIOS Y MEMORIA PERSISTENTE (CAPA 7 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Propósito Crítico:** Memoria persistente en disco para el Agente de IA. Elimina la dependencia del historial de chat y previene alucinaciones por desfase entre especificaciones y código real.
**Regla de Uso:** El Agente DEBE leer este archivo ANTES de iniciar cualquier tarea y DEBE actualizarlo ANTES de dar por finalizada cualquier tarea.

---

## 1. ESTADO ACTUAL DEL PROYECTO (PUNTERO DE EJECUCIÓN)

* **Última Tarea Completada:** `NINGUNA (Estado Inicial Pre-Ejecución)`
* **Tarea Actual en Curso:** `TAREA 1.1 - Jerarquía Base Multi-Tenant y Excepciones de Dominio`
* **Estado de Compilación Backend (`dotnet build`):** `Pendiente de verificación inicial`
* **Estado de Migración SQL Server:** `Migración inicial previa (Pendiente de reemplazo/actualización en GATE 1)`
* **Estado de Compilación Frontend (`npm run build`):** `En espera hasta superar HARD GATE 3`

---

## 2. REGISTRO DE DESVIACIONES, RENOMBRES Y DECISIONES APROBADAS EN VIVO (OVERRIDE LOG)

> **DIRECTIVA PARA EL AGENTE:** Toda modificación de nombres de propiedades, firmas de métodos, tipos de datos o reglas de negocio que se decida durante la programación DEBE registrarse en esta tabla y sincronizarse en el archivo `01` a `05` correspondiente. **Lo registrado en esta tabla tiene prioridad sobre cualquier texto anterior.**

| Fecha / Tarea | Archivo `.md` Original Afectado | Especificación Original | Nuevo Valor / Cambio Implementado | Motivo Técnico / Autorización |
| :--- | :--- | :--- | :--- | :--- |
| *Inicial* | `03_DATA_MODEL.md` | `ITentantEntity.cs` | `ITenantEntity.cs` + `AuditableTenantEntity.cs` | Corrección de typo e inclusión de herencia universal (`TenantId`, `SucursalId`)[cite: 30, 35]. |

---

## 3. INVENTARIO DE PUENTES DE COMPATIBILIDAD TEMPORAL (`[NotMapped]` / Obsoletos)

> **DIRECTIVA PARA EL AGENTE:** Si en una tarea temprana se deja una propiedad o método puente para no romper la compilación de capas superiores, anotalo acá y tácharlo (`~~así~~`) cuando lo elimines en la tarea correspondiente.

* `[PENDIENTE DE CREAR EN TAREA 1.2 -> ELIMINAR EN TAREA 4.1]`: Propiedades `[NotMapped] Fecha` y `[NotMapped] DetalleFormulaTintura` en `Models/Clientes/FichaTecnica.cs` para sostener la compilación hasta refactorizar `FichaTecnicaService.cs`.

---

## 4. DICCIONARIO DE SÍMBOLOS Y FIRMAS CRÍTICAS CONSOLIDADAS EN CÓDIGO

*(El agente debe ir completando esta sección al cerrar cada Etapa con las firmas exactas que quedaron compiladas en C# para no adivinarlas en etapas posteriores)*

* **Clase Base Operativa (`Models/Common/AuditableTenantEntity.cs`):** *(Pendiente Tarea 1.1)*
* **Firma Transaccional (`Repositories/Interfaces/IUnitOfWork.cs`):** *(Pendiente Tarea 3.1)*
* **Firma Batch Anti N+1 (`Repositories/Interfaces/IGenericRepository.cs`):** *(Pendiente Tarea 3.1)*

---

## 5. HISTORIAL DE AUDITORÍA PASO A PASO (TASK EXECUTION LOG)

### Plantilla de Registro por Tarea (El Agente debe agregar un bloque por cada tarea terminada):
```text
### [TAREA X.X] - Nombre de la Tarea
- Estado: COMPLETADA / EN REVISIÓN
- Archivos Creados: [lista]
- Archivos Modificados: [lista]
- Archivos Eliminados: [lista]
- Cambios o Decisiones Especiales: [detalle de cualquier ajuste fino]
- Sincronización de MD (Spec-Sync): [indicar si hubo que actualizar algún archivo 01-05]
- Resultado dotnet build: 0 Errores / X Advertencias