# Fuentes editables de los diagramas (Mermaid)

Pega cada bloque en https://mermaid.live o en un editor compatible (VS Code, Draw.io → Organizar → Insertar → Mermaid).
Los SVG finales de las diapositivas están en esta misma carpeta.

## 1. Arquitectura MVC implementada en el MVP local

```mermaid
flowchart LR
  subgraph Navegador["Navegador (HTML · CSS · JS)"]
    V1["Vista Razor<br/>Transferencias/Nueva.cshtml"]
    V2["Diálogo «Revisa tu envío»<br/>clave de idempotencia oculta"]
    V3["Vista de constancia<br/>Historial/Constancia.cshtml"]
  end
  subgraph Controladores
    C1["TransferenciasController"]
    C2["AccesoController · CuentasController<br/>HistorialController · ContinuidadController"]
  end
  subgraph Servicios["Servicios (casos de uso)"]
    S1["ServicioTransferencias<br/>fondos · clave única · débito + abono"]
    S2["ServicioConciliacion · ServicioCuentas<br/>ServicioAcceso · ServicioAuditoria"]
    A1["Adaptadores BCP / Yape (locales)"]
  end
  subgraph Datos["Modelo y persistencia"]
    D1["AppDbContext (EF Core)"]
    D2[("SQLite<br/>bcp_academico.db")]
  end
  V1 --> V2 -- "POST /banca/enviar/confirmar" --> C1 --> S1 --> D1 --> D2
  S1 --> A1
  C2 --> S2 --> D1
  D2 -. "redirección" .-> V3
```

## 2. Arquitectura empresarial propuesta (To-Be del PESI)

```mermaid
flowchart LR
  subgraph Canales["Canales digitales"]
    App["App Banca Móvil"]
    Yape["Yape"]
    OF["Open Finance (APIs)"]
  end
  subgraph Azure["Microsoft Azure — Propuesta To-Be del PESI"]
    APIM["Azure API Management<br/>Strangler Fig · OAuth 2.0 · rate limiting"]
    AKS["Microservicios en AKS<br/>pagos idempotentes · conciliación"]
    LEC[("Modelo de lectura CQRS<br/>Azure SQL / Cosmos DB")]
    OBS["Seguridad · observabilidad · DevSecOps"]
    DR["DRP: RTO ≤ 15 min · RPO 0 (meta)"]
  end
  subgraph Central["Sistema central on-premise"]
    CORE["Core Retail (mainframe)<br/>CICS / MQ"]
    DB2[("DB2")]
  end
  App & Yape & OF --> APIM
  APIM --> AKS
  APIM -- lecturas --> LEC
  AKS -- escrituras --> CORE --> DB2
  DB2 -. CDC .-> LEC
  AKS --- OBS
  AKS --- DR
```
