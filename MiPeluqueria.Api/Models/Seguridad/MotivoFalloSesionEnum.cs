namespace MiPeluqueria.Api.Models.Seguridad
{
    public enum MotivoFalloSesionEnum
    {
        Ninguno = 0, // Login Exitoso
        CredencialesIncorrectas = 1,
        CuentaBloqueada = 2, // Por exceso de intentos fallidos (Fuerza Bruta)
        CuentaInactiva = 3, // Dado de baja por el administrador
        EmailNoVerificado = 4,
        FaltaAceptarTerminos = 5,
        BotDetectado = 6, // Falló el reCAPTCHA
        CodigoOTPInvalido = 7, // Le erró al pin temporal del mail
        IPBloqueada = 8 // Bloqueo de red por seguridad
    }
}