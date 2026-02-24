# automatizacion-alertas

Servicio de notificaciones automáticas a Microsoft Teams mediante **Adaptive Cards**, desarrollado en **.NET 10 Worker Service** con **Quartz.NET** para la programación de tareas.

## ¿Qué hace?

Envía notificaciones programadas al canal de Teams configurado a las siguientes horas:

| Hora  | Mensaje              |
|-------|----------------------|
| 8:00  | Buenos días - 8:00 AM |
| 12:00 | Mediodía - 12:00 PM  |
| 15:00 | Buenas tardes - 3:00 PM |
| 17:00 | Final del día - 5:00 PM |

Cada notificación se envía como un **Adaptive Card** con la fecha, hora y estado del servicio.

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8 o superior, con la carga de trabajo **Desarrollo de ASP.NET y web**) — opcional
- Un **Incoming Webhook** configurado en Microsoft Teams

## Abrir en Visual Studio

1. Clona el repositorio:
   ```bash
   git clone https://github.com/pfernandez-veconinter/automatizacion-alertas.git
   ```
2. Abre el archivo **`AutomatizacionAlertas.sln`** con Visual Studio 2022.
3. Visual Studio cargará automáticamente el proyecto `TeamsNotificationService`.
4. Configura el webhook (ver sección [Configuración del servicio](#configuración-del-servicio)).
5. Presiona **F5** para ejecutar en modo depuración.

## Configuración del Webhook de Teams

1. En Teams, ve al canal donde quieres recibir las notificaciones.
2. Haz clic en `···` → **Connectors** → **Incoming Webhook** → **Configure**.
3. Dale un nombre (p. ej. `AlertasAutomatizacion`) y copia la URL generada.

## Configuración del servicio

Edita `TeamsNotificationService/appsettings.json` (o usa variables de entorno / User Secrets):

```json
{
  "Teams": {
    "WebhookUrl": "https://outlook.office.com/webhook/TU_URL_AQUI"
  },
  "Schedule": {
    "TimeZone": "America/Caracas"
  }
}
```

> ⚠️ **Nunca** incluyas la URL del webhook en control de versiones. Usa **User Secrets** o variables de entorno en producción.

### Usando User Secrets (desarrollo local)

```bash
cd TeamsNotificationService
dotnet user-secrets set "Teams:WebhookUrl" "https://outlook.office.com/webhook/TU_URL_AQUI"
```

### Usando variables de entorno

```bash
export Teams__WebhookUrl="https://outlook.office.com/webhook/TU_URL_AQUI"
```

## Ejecución

```bash
cd TeamsNotificationService
dotnet run
```

## Publicar y ejecutar como servicio

### Windows (servicio de Windows)

```bash
dotnet publish -c Release -o ./publish
# Instalar como servicio de Windows con sc.exe o NSSM
```

### Linux (systemd)

```bash
dotnet publish -c Release -o /opt/alertas
# Crear /etc/systemd/system/alertas.service con el ejecutable
systemctl enable alertas && systemctl start alertas
```

## Estructura del proyecto

```
TeamsNotificationService/
├── Jobs/
│   ├── AdaptiveCardFactory.cs     # Construye el payload de la Adaptive Card
│   └── TeamsNotificationJob.cs    # Job de Quartz que se ejecuta en cada horario
├── Models/
│   └── AdaptiveCardPayload.cs     # Modelos del payload JSON para Teams
├── Services/
│   └── TeamsWebhookService.cs     # Envía el POST HTTP al webhook de Teams
├── Program.cs                     # Configuración del host, Quartz y DI
├── appsettings.json               # Configuración base
└── appsettings.Development.json   # Configuración para desarrollo
```
